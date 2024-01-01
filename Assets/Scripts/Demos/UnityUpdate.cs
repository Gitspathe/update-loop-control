using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class UnityUpdate : MonoBehaviour
{
    public static int Counter;
    
    private void Update()
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
