using UnityEngine;
using System;

public class HandwashingManager : MonoBehaviour
{
    public enum WashStep
    {
        WetHands,
        ApplySoap,
        RubPalms,
        Rinse,
        Complete
    }

    public WashStep CurrentStep { get; private set; }

    public static HandwashingManager Instance { get; private set; }

    public event Action<WashStep> OnStepChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SetStep(WashStep.WetHands);
    }

    public void SetStep(WashStep step)
    {
        CurrentStep = step;
        OnStepChanged?.Invoke(step);

        Debug.Log($"Current Step: {step}");
    }

    public void AdvanceStep()
    {
        if (CurrentStep == WashStep.Complete)
            return;

        SetStep(CurrentStep + 1);
    }
}