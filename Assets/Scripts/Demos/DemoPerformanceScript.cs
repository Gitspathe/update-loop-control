using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DemoPerformanceScript : MonoBehaviour
{
    [SerializeField] private int instances = 5_000;
    [SerializeField] private bool unityUpdate;
    [SerializeField] private bool complexUpdate;
    [SerializeField] private TextMeshProUGUI infoText1;
    [SerializeField] private TextMeshProUGUI infoText2;

    private GameObject parent;

    public static bool ComplexUpdate;

    private void Awake()
    {
        ComplexUpdate = complexUpdate;
        Setup();
    }

    private void LateUpdate()
    {
        UnityUpdate.Counter   = 0;
        ManagerUpdate.Counter = 0;

        if (Input.GetKeyDown(KeyCode.Q)) {
            unityUpdate = !unityUpdate;
            Setup();
        }
        if (Input.GetKeyDown(KeyCode.W)) {
            complexUpdate = !complexUpdate;
            Setup();
        }
        
        ComplexUpdate = complexUpdate;
        infoText1.text = $"Press Q to set manager update {(unityUpdate    ? "ON" : "OFF")}";        
        infoText2.text = $"Press W to set complex update {(!complexUpdate ? "ON" : "OFF")}";
    }

    private void Setup()
    {
        if (parent != null) {
            Destroy(parent);
        }
        
        parent = new GameObject("Component Holder");
        parent.hideFlags = HideFlags.HideAndDontSave;
        for (int i = 0; i < instances; i++) {
            GameObject go = new GameObject(i.ToString());
            go.transform.parent = parent.transform;
            go.AddComponent(unityUpdate ? typeof(UnityUpdate) : typeof(ManagerUpdate));
        } 
    }
}
