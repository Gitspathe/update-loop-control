using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
public abstract class GameComponent : MonoBehaviour, IGameComponent
{
    public virtual int UpdateOrder => 0;
    public bool IsValidForUpdating { get; set; }

    protected virtual void OnEnable()
    {
        UpdateManager.Register(this);
    }

    protected virtual void OnDisable()
    {
        UpdateManager.Unregister(this);
    }

    protected virtual void OnDestroy()
    {
        UpdateManager.Unregister(this);
    }
}
