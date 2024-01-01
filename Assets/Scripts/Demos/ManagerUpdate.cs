using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerUpdate : GameComponent, IUpdate
{
    public static int Counter;
    
    public void GameUpdate()
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
