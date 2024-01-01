using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FramerateCounter : MonoBehaviour
{
    [field: SerializeField] public bool ShowOnStart { get; private set; }
    [field: SerializeField] public GameObject MainPanel { get; private set; }
    [field: SerializeField] public TextMeshProUGUI AverageFPSText { get; private set; }
    [field: SerializeField] public int AverageFrames { get; private set; } = 60;
    
    private List<double> frameTimes = new List<double>();
    private bool showFPS;
    
    public static FramerateCounter Instance { get; private set; }

    private void Awake()
    {
        if(Instance != null) {
            Debug.LogError($"Attempted to initialize duplicate {nameof(FramerateCounter)} singleton.");
            Destroy(gameObject);
            return;
        }

        Hide();
        if(ShowOnStart) {
            Show();
        }
        DontDestroyOnLoad(gameObject);
        
        Instance = this;
    }

    public void Toggle()
    {
        if(showFPS) {
            Hide();
        } else {
            Show();
        }
    }

    public void Show()
    {
        MainPanel.SetActive(true);
        frameTimes.Clear();
        showFPS = true;
    }

    public void Hide()
    {
        MainPanel.SetActive(false);
        frameTimes.Clear();
        showFPS = false;
    }

    private void Update()
    {
        if(!showFPS)
            return;
        
        double time = Time.unscaledDeltaTime;
        if(frameTimes.Count >= AverageFrames) {
            frameTimes.RemoveAt(frameTimes.Count - 1);
        }
        frameTimes.Insert(0, time);

        double acc = 0;
        foreach(double t in frameTimes) {
            acc += t;
        }

        double avg = acc / frameTimes.Count;
        AverageFPSText.text = $"AVG FPS: {(int)(1.0 / avg)} | {avg * 1000.0f:0.000} ms";
    }
}
