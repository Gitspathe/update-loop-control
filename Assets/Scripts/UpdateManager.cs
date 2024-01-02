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
    
    public static Action OnInitialized { get; set; }
    public static bool IsRunning       { get; private set; }

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
            Debug.LogError($"Tried to add update entry '{entry.Name}', but an entry at order {entry.Order} already exists.");
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

    public static void Register(IGameComponent component)
    {
        if(!EnsureInitialization())
            return;
        
        if(!Instance.loop.TryGetValue(component.UpdateOrder, out UpdateLoopEntry loopEntry)) {
            loopEntry = new UpdateLoopEntry($"Unnamed ({component.UpdateOrder})", component.UpdateOrder, false);
            AddLoopEntry(loopEntry);
        }
        loopEntry.Register(component);
    }

    public static void Unregister(IGameComponent component)
    {
        if(!EnsureInitialization())
            return;
        
        if(!Instance.loop.TryGetValue(component.UpdateOrder, out UpdateLoopEntry loopEntry)) {
            Debug.LogError($"Failed to unregister component: no update loop at position {component.UpdateOrder} found.");
            return;
        }
        loopEntry.Unregister(component);
    }
    
    private static bool EnsureInitialization()
    {
        if(Instance != null)
            return true;
        
        Debug.LogError("UpdateManager is not initialized!");
        return false;
    }
}

public interface IGameComponent
{
    int UpdateOrder         { get; }
    bool IsValidForUpdating { get; set; }
}

public interface IFixedUpdate : IGameComponent
{
    void GameFixedUpdate();
}

public interface IEarlyUpdate : IGameComponent
{
    void GameEarlyUpdate();
}

public interface IUpdate : IGameComponent
{
    void GameUpdate();
}

public interface ILateUpdate : IGameComponent
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
    private HashSet<IGameComponent> toAdd      = new HashSet<IGameComponent>();
    private HashSet<IGameComponent> toRemove   = new HashSet<IGameComponent>();

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

    public void Register(IGameComponent gameComponent)
    {
        count++;
        gameComponent.IsValidForUpdating = true;
        if(UpdateManager.IsRunning) {
            toAdd.Add(gameComponent);
        } else {
            RegisterInternal(gameComponent);
        }
    }

    public void Unregister(IGameComponent gameComponent)
    {
        count--;
        gameComponent.IsValidForUpdating = false;
        if(UpdateManager.IsRunning) {
            toRemove.Add(gameComponent);
        } else {
            UnregisterInternal(gameComponent);
        }
    }

    public void CleanUp()
    {
        foreach(IGameComponent component in toAdd) {
            RegisterInternal(component);
        }
        foreach(IGameComponent component in toRemove) {
            UnregisterInternal(component);
        }
        toAdd.Clear();
        toRemove.Clear();
    }

    private void UnregisterInternal(IGameComponent component)
    {
        if(component is IFixedUpdate fixedUpdate) {
            fixedUpdates.Remove(fixedUpdate);
        }
        if(component is IEarlyUpdate earlyUpdate) {
            earlyUpdates.Remove(earlyUpdate);
        }
        if(component is IUpdate update) {
            updates.Remove(update);
        }
        if(component is ILateUpdate lateUpdate) {
            lateUpdates.Remove(lateUpdate);
        }
    }

    private void RegisterInternal(IGameComponent component)
    {
        if(component is IFixedUpdate fixedUpdate) {
            fixedUpdates.Add(fixedUpdate);
        }
        if(component is IEarlyUpdate earlyUpdate) {
            earlyUpdates.Add(earlyUpdate);
        }
        if(component is IUpdate update) {
            updates.Add(update);
        }
        if(component is ILateUpdate lateUpdate) {
            lateUpdates.Add(lateUpdate);
        }
    }
    
    public override int GetHashCode()
    {
        return Order;
    }
}
