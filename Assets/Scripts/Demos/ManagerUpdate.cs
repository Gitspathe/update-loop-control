using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerUpdate : LoopComponent, ILoopUpdate
{
    public static int Counter;
    
    public void LoopUpdate()
    {
        if (DemoPerformanceScript.ComplexUpdate) {
            for (int i = 0; i < 10; i++) {
                Counter += Random.Range(0, 5);
            }
        } else {
            Counter++;
        }
    }
}
