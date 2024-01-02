using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UpdateTaskBase : IGameComponent
{
    protected Action Action;
        
    public int UpdateOrder         { get; protected set; }
    public bool IsValidForUpdating { get; set; }

    protected UpdateTaskBase(Action action, int updateOrder)
    {
        Action = action;
        UpdateOrder = updateOrder;
    }
        
    public void Register()
    {
        UpdateManager.Register(this);
    }

    public void Unregister()
    {
        UpdateManager.Unregister(this);
    }
}

public class FixedUpdateTask : UpdateTaskBase, IFixedUpdate
{
    public FixedUpdateTask(Action onFixedUpdate, int updateOrder = 0) : base(onFixedUpdate, updateOrder) { }
    public void GameFixedUpdate() => Action.Invoke();
}
    
public class EarlyUpdateTask : UpdateTaskBase, IEarlyUpdate
{
    public EarlyUpdateTask(Action onEarlyUpdate, int updateOrder = 0) : base(onEarlyUpdate, updateOrder) { }
    public void GameEarlyUpdate() => Action.Invoke();
}
    
public class UpdateTask : UpdateTaskBase, IUpdate
{
    public UpdateTask(Action onUpdate, int updateOrder = 0) : base(onUpdate, updateOrder) { }
    public void GameUpdate() => Action.Invoke();
}
    
public class LateUpdateTask : UpdateTaskBase, ILateUpdate
{
    public LateUpdateTask(Action onLateUpdate, int updateOrder = 0) : base(onLateUpdate, updateOrder) { }
    public void GameLateUpdate() => Action.Invoke();
}
