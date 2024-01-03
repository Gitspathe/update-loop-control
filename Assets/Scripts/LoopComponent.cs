using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
public abstract class LoopComponent : MonoBehaviour, ILoopUpdateable
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
}
