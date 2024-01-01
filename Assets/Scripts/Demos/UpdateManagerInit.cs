using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
public class UpdateManagerInit : MonoBehaviour
{
    public UpdateManagerInit()
    {
        UpdateManager.OnInitialized = Register;
    }

    private void Register()
    {
        UpdateManager.Instance.AddLoopEntries(
            new UpdateLoopEntry("Default",            GameUpdateOrder.Default),
            new UpdateLoopEntry("AI Preprocessing",   GameUpdateOrder.AIPreprocessing),
            new UpdateLoopEntry("AI Processing",      GameUpdateOrder.AIProcessing),
            new UpdateLoopEntry("AI Post Processing", GameUpdateOrder.AIPostProcessing));
    }
}

public static class GameUpdateOrder
{
    public static int AIPreprocessing  => -110;
    public static int AIProcessing     => -100;
    public static int Default          => 0;
    public static int AIPostProcessing => 100;
}
