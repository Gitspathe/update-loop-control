using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    private SortedDictionary<int, UpdateLoopEntry> loop = new SortedDictionary<int, UpdateLoopEntry>();
    private List<int> entriesToRemove                   = new List<int>();

    [SerializeField] private bool removeUnusedEntries;
    [SerializeField] private bool showDebugMessages;

    private static bool isQuitting;
    
    public static Action OnInitialized   { get; set; }
    public static bool IsRunning         { get; private set; }
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
        isQuitting = false;
        OnInitialized?.Invoke();
    }

    private void OnDestroy()
    {
        OnInitialized = null;
    }

    private void FixedUpdate()
    {
        IsRunning = true;
        foreach(UpdateLoopEntry entry in loop.Values) {
            entry.ExecuteFixedUpdate();
        }
    }

    private void Update()
    {
        IsRunning = true;
        foreach(UpdateLoopEntry entry in loop.Values) {
            entry.ExecuteEarlyUpdate();
        }
        foreach(UpdateLoopEntry entry in loop.Values) {
            entry.ExecuteUpdate();
        }
    }

    private void LateUpdate()
    {
        foreach(UpdateLoopEntry entry in loop.Values) {
            entry.ExecuteLateUpdate();
        }
        foreach(UpdateLoopEntry entry in loop.Values) {
            entry.CleanUp();
        }
        if(removeUnusedEntries) {
            PruneUnused();
        }
        IsRunning = false;
    }

    private void PruneUnused()
    {
        foreach(UpdateLoopEntry entry in loop.Values) {
            if(entry.Permanent || !entry.IsEmpty)
                continue;
            
            entriesToRemove.Add(entry.Order);
            if(showDebugMessages) {
                Debug.Log($"Pruning unused update loop entry with order {entry.Order}, Name: {entry.Name}");
            }
        }
        foreach(int i in entriesToRemove) {
            loop.Remove(i);
        }
        entriesToRemove.Clear();
    }

    public static void AddLoopEntry(UpdateLoopEntry entry)
    {
        if(!EnsureInitialization())
            return;
        
        if(Instance.loop.ContainsKey(entry.Order)) {
            Debug.LogError($"Tried to add update loop entry '{entry.Name}', but an entry at order {entry.Order} already exists.");
            return;
        }
        Instance.loop.Add(entry.Order, entry);
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
        
        if(!Instance.loop.TryGetValue(updateable.UpdateOrder, out UpdateLoopEntry loopEntry)) {
            loopEntry = new UpdateLoopEntry($"Unnamed ({updateable.UpdateOrder})", updateable.UpdateOrder, false);
            AddLoopEntry(loopEntry);
        }
        loopEntry.Register(updateable);
    }

    public static void Unregister(ILoopUpdateable updateable)
    {
        if(!EnsureInitialization())
            return;
        
        if(!Instance.loop.TryGetValue(updateable.UpdateOrder, out UpdateLoopEntry loopEntry)) {
            Debug.LogError($"Failed to unregister updateable: no update loop entry at position {updateable.UpdateOrder}.");
            return;
        }
        loopEntry.Unregister(updateable);
    }
    
    private static bool EnsureInitialization()
    {
        if(isQuitting || !Application.isPlaying)
            return false;
        if(Instance != null)
            return true;
        
        Debug.LogError("UpdateManager is not initialized!");
        return false;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
}

public interface ILoopUpdateable
{
    int UpdateOrder         { get; }
    bool IsValidForUpdating { get; set; }
}

public interface IFixedUpdate : ILoopUpdateable
{
    void GameFixedUpdate();
}

public interface IEarlyUpdate : ILoopUpdateable
{
    void GameEarlyUpdate();
}

public interface IUpdate : ILoopUpdateable
{
    void GameUpdate();
}

public interface ILateUpdate : ILoopUpdateable
{
    void GameLateUpdate();
}

public class UpdateLoopEntry
{
    public string Name    { get; }
    public int Order      { get; }
    public bool Permanent { get; }

    private int count;
    private HashSet<IFixedUpdate> fixedUpdates = new HashSet<IFixedUpdate>();
    private HashSet<IEarlyUpdate> earlyUpdates = new HashSet<IEarlyUpdate>();
    private HashSet<IUpdate> updates           = new HashSet<IUpdate>();
    private HashSet<ILateUpdate> lateUpdates   = new HashSet<ILateUpdate>();
    private HashSet<ILoopUpdateable> toAdd     = new HashSet<ILoopUpdateable>();
    private HashSet<ILoopUpdateable> toRemove  = new HashSet<ILoopUpdateable>();

    public bool IsEmpty => count == 0;
    
    public UpdateLoopEntry(string name, int order, bool permanent = true)
    {
        Name = name;
        Order = order;
        Permanent = permanent;
    }

    public void ExecuteFixedUpdate()
    {
        foreach(IFixedUpdate update in fixedUpdates) {
            if(!update.IsValidForUpdating)
                continue;
            
            update.GameFixedUpdate();
        }
    }

    public void ExecuteEarlyUpdate()
    {
        foreach(IEarlyUpdate update in earlyUpdates) {
            if(!update.IsValidForUpdating)
                continue;
            
            update.GameEarlyUpdate();
        }
    }
    
    public void ExecuteUpdate()
    {
        foreach(IUpdate update in updates) {
            if(!update.IsValidForUpdating)
                continue;
            
            update.GameUpdate();
        }
    }
    
    public void ExecuteLateUpdate()
    {
        foreach(ILateUpdate update in lateUpdates) {
            if(!update.IsValidForUpdating)
                continue;
            
            update.GameLateUpdate();
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
            toRemove.Add(updateable);
        } else {
            UnregisterInternal(updateable);
        }
    }

    public void CleanUp()
    {
        foreach(ILoopUpdateable updateable in toAdd) {
            RegisterInternal(updateable);
        }
        foreach(ILoopUpdateable updateable in toRemove) {
            UnregisterInternal(updateable);
        }
        toAdd.Clear();
        toRemove.Clear();
    }

    private void UnregisterInternal(ILoopUpdateable updateable)
    {
        if(updateable is IFixedUpdate fixedUpdate) {
            fixedUpdates.Remove(fixedUpdate);
        }
        if(updateable is IEarlyUpdate earlyUpdate) {
            earlyUpdates.Remove(earlyUpdate);
        }
        if(updateable is IUpdate update) {
            updates.Remove(update);
        }
        if(updateable is ILateUpdate lateUpdate) {
            lateUpdates.Remove(lateUpdate);
        }
    }

    private void RegisterInternal(ILoopUpdateable updateable)
    {
        if(updateable is IFixedUpdate fixedUpdate) {
            fixedUpdates.Add(fixedUpdate);
        }
        if(updateable is IEarlyUpdate earlyUpdate) {
            earlyUpdates.Add(earlyUpdate);
        }
        if(updateable is IUpdate update) {
            updates.Add(update);
        }
        if(updateable is ILateUpdate lateUpdate) {
            lateUpdates.Add(lateUpdate);
        }
    }
    
    public override int GetHashCode()
    {
        return Order;
    }
}
