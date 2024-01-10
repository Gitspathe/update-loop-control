using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    private List<UpdateLoopEntry> loop              = new List<UpdateLoopEntry>();
    private List<UpdateLoopEntry> entriesToRemove   = new List<UpdateLoopEntry>();
    private Dictionary<int, UpdateLoopEntry> lookup = new Dictionary<int, UpdateLoopEntry>();

    [SerializeField] private bool removeUnusedEntries;
    [SerializeField] private bool showDebugMessages;
    
    public static Action OnInitialized   { get; set; }
    public static bool IsRunning         { get; private set; }
    public static bool IsQuitting        { get; private set; }
    public static UpdateManager Instance { get; private set; }

    public void Awake()
    {
        if(Instance != null) {
            Debug.Log($"UpdateManager singleton already exists. Destroying new instance.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        Instance = this;
        IsQuitting = false;
        OnInitialized?.Invoke();
    }

    private void OnDestroy()
    {
        OnInitialized = null;
    }

    private void FixedUpdate()
    {
        IsRunning = true;
        foreach(UpdateLoopEntry entry in loop) {
            entry.ExecuteFixedUpdate();
        }
    }

    private void Update()
    {
        IsRunning = true;
        foreach(UpdateLoopEntry entry in loop) {
            entry.ExecuteEarlyUpdate();
        }
        foreach(UpdateLoopEntry entry in loop) {
            entry.ExecuteUpdate();
        }
    }

    private void LateUpdate()
    {
        foreach(UpdateLoopEntry entry in loop) {
            entry.ExecuteLateUpdate();
        }
        foreach(UpdateLoopEntry entry in loop) {
            entry.CleanUp();
        }
        if(removeUnusedEntries) {
            PruneUnused();
        }
        IsRunning = false;
    }

    private void PruneUnused()
    {
        foreach(UpdateLoopEntry entry in loop) {
            if(entry.Permanent || !entry.IsEmpty)
                continue;
            
            entriesToRemove.Add(entry);
            if(showDebugMessages) {
                Debug.Log($"Pruning unused update loop entry with order {entry.Order}, Name: {entry.Name}");
            }
        }
        foreach(UpdateLoopEntry i in entriesToRemove) {
            loop.Remove(i);
        }
        entriesToRemove.Clear();
    }

    public static void AddLoopEntry(UpdateLoopEntry entry)
    {
        if(!EnsureInitialization())
            return;
        
        if(Instance.lookup.ContainsKey(entry.Order)) {
            Debug.LogError($"Tried to add update loop entry '{entry.Name}', but an entry at order {entry.Order} already exists.");
            return;
        }
        Instance.loop.Add(entry);
        Instance.lookup.Add(entry.Order, entry);
        Sort();
        if(Instance.showDebugMessages) {
            Debug.Log($"Registered update loop entry with order {entry.Order}, Name: {entry.Name}");
        }
    }

    public static void AddLoopEntries(params UpdateLoopEntry[] entries)
    {
        foreach(UpdateLoopEntry entry in entries) {
            AddLoopEntry(entry);
        }
    }

    public static void Register(ILoopUpdateable updateable)
    {
        if(!EnsureInitialization())
            return;

        if(!Instance.lookup.TryGetValue(updateable.UpdateOrder, out UpdateLoopEntry loopEntry)) {
            loopEntry = new UpdateLoopEntry($"Unnamed ({updateable.UpdateOrder})", updateable.UpdateOrder, false);
            AddLoopEntry(loopEntry);
        }
        loopEntry.Register(updateable);
    }

    public static void Unregister(ILoopUpdateable updateable)
    {
        if(!EnsureInitialization())
            return;
        
        if(!Instance.lookup.TryGetValue(updateable.UpdateOrder, out UpdateLoopEntry loopEntry)) {
            Debug.LogError($"Failed to unregister updateable: no update loop entry at position {updateable.UpdateOrder}.");
            return;
        }
        loopEntry.Unregister(updateable);
    }

    private static void Sort()
    {
        Instance.loop.Sort(new UpdateLoopEntry.Comparer());
    }
    
    private static bool EnsureInitialization()
    {
        if(IsQuitting || !Application.isPlaying)
            return false;
        if(Instance != null)
            return true;
        
        Debug.LogError("UpdateManager is not initialized!");
        return false;
    }

    private void OnApplicationQuit()
    {
        IsQuitting = true;
    }
}

public interface ILoopUpdateable
{
    int UpdateOrder         { get; }
    bool IsValidForUpdating { get; set; }
}

public interface ILoopFixedUpdate : ILoopUpdateable
{
    void LoopFixedUpdate();
}

public interface ILoopEarlyUpdate : ILoopUpdateable
{
    void LoopEarlyUpdate();
}

public interface ILoopUpdate : ILoopUpdateable
{
    void LoopUpdate();
}

public interface ILoopLateUpdate : ILoopUpdateable
{
    void LoopLateUpdate();
}

public class UpdateLoopEntry
{
    public string Name    { get; }
    public int Order      { get; }
    public bool Permanent { get; }

    private int count;
    private HashSet<ILoopFixedUpdate> fixedUpdates = new HashSet<ILoopFixedUpdate>();
    private HashSet<ILoopEarlyUpdate> earlyUpdates = new HashSet<ILoopEarlyUpdate>();
    private HashSet<ILoopUpdate> updates           = new HashSet<ILoopUpdate>();
    private HashSet<ILoopLateUpdate> lateUpdates   = new HashSet<ILoopLateUpdate>();
    private HashSet<ILoopUpdateable> toAdd         = new HashSet<ILoopUpdateable>();
    private HashSet<ILoopUpdateable> toRemove      = new HashSet<ILoopUpdateable>();

    public bool IsEmpty => count == 0;
    
    public UpdateLoopEntry(string name, int order, bool permanent = true)
    {
        Name = name;
        Order = order;
        Permanent = permanent;
    }

    public void ExecuteFixedUpdate()
    {
        foreach(ILoopFixedUpdate update in fixedUpdates) {
            if(!update.IsValidForUpdating)
                continue;

            try {
                update.LoopFixedUpdate();
            } catch(Exception e) {
                Debug.LogError(e);
            }
        }
    }

    public void ExecuteEarlyUpdate()
    {
        foreach(ILoopEarlyUpdate update in earlyUpdates) {
            if(!update.IsValidForUpdating)
                continue;

            try {
                update.LoopEarlyUpdate();
            } catch(Exception e) {
                Debug.LogError(e);
            }
        }
    }
    
    public void ExecuteUpdate()
    {
        foreach(ILoopUpdate update in updates) {
            if(!update.IsValidForUpdating)
                continue;
            
            try {
                update.LoopUpdate();
            } catch(Exception e) {
                Debug.LogError(e);
            }
        }
    }
    
    public void ExecuteLateUpdate()
    {
        foreach(ILoopLateUpdate update in lateUpdates) {
            if(!update.IsValidForUpdating)
                continue;
            
            try {
                update.LoopLateUpdate();
            } catch(Exception e) {
                Debug.LogError(e);
            }
        }
    }

    public void Register(ILoopUpdateable updateable)
    {
        count++;
        updateable.IsValidForUpdating = true;
        if(UpdateManager.IsRunning) {
            toAdd.Add(updateable);
        } else {
            RegisterInternal(updateable);
        }
    }

    public void Unregister(ILoopUpdateable updateable)
    {
        count--;
        updateable.IsValidForUpdating = false;
        if(UpdateManager.IsRunning) {
            toAdd.Remove(updateable);
            toRemove.Add(updateable);
        } else {
            UnregisterInternal(updateable);
        }
    }

    public void CleanUp()
    {
        foreach(ILoopUpdateable updateable in toRemove) {
            UnregisterInternal(updateable);
        }
        foreach(ILoopUpdateable updateable in toAdd) {
            RegisterInternal(updateable);
        }
        toAdd.Clear();
        toRemove.Clear();
    }

    private void UnregisterInternal(ILoopUpdateable updateable)
    {
        if(updateable is ILoopFixedUpdate fixedUpdate) {
            fixedUpdates.Remove(fixedUpdate);
        }
        if(updateable is ILoopEarlyUpdate earlyUpdate) {
            earlyUpdates.Remove(earlyUpdate);
        }
        if(updateable is ILoopUpdate update) {
            updates.Remove(update);
        }
        if(updateable is ILoopLateUpdate lateUpdate) {
            lateUpdates.Remove(lateUpdate);
        }
    }

    private void RegisterInternal(ILoopUpdateable updateable)
    {
        if(updateable is ILoopFixedUpdate fixedUpdate) {
            fixedUpdates.Add(fixedUpdate);
        }
        if(updateable is ILoopEarlyUpdate earlyUpdate) {
            earlyUpdates.Add(earlyUpdate);
        }
        if(updateable is ILoopUpdate update) {
            updates.Add(update);
        }
        if(updateable is ILoopLateUpdate lateUpdate) {
            lateUpdates.Add(lateUpdate);
        }
    }
    
    public override int GetHashCode()
    {
        return Order;
    }
    
    public struct Comparer : IComparer<UpdateLoopEntry>
    {
        public int Compare(UpdateLoopEntry x, UpdateLoopEntry y)
        {
            return x.Order.CompareTo(y.Order);
        }
    }
}
