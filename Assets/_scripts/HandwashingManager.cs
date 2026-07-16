using System;
using System.Collections.Generic;
using UnityEngine;

public class HandwashingManager : MonoBehaviour
{
    public static HandwashingManager Instance { get; private set; }

    public WashStep CurrentStep { get; private set; } = WashStep.WetHands;
    public float CurrentStepProgress { get; private set; }
    public float TotalElapsedTime { get; private set; }

    public float LeftHandProgress { get; private set; }
    public float RightHandProgress { get; private set; }

    public event Action<WashStep> OnStepChanged;
    public event Action<float> OnProgressChanged;
    public event Action<WashStep> OnStepCompleted;
    public event Action<Handedness, float> OnHandProgressChanged;

    private readonly HashSet<WashStep> completedSteps = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        OnStepChanged?.Invoke(CurrentStep);
    }

    private void Update()
    {
        if (CurrentStep != WashStep.Complete)
            TotalElapsedTime += Time.deltaTime;
    }

    public void AccumulateProgress(WashStep step, float deltaTime)
    {
        float duration = WashStepCatalog.GetDuration(step);
        if (duration <= 0f)
            return;

        SetProgress(step, CurrentStepProgress + deltaTime / duration);
    }

    public void SetProgress(WashStep step, float normalized)
    {
        if (step != CurrentStep)
            return;

        CurrentStepProgress = Mathf.Clamp01(normalized);
        OnProgressChanged?.Invoke(CurrentStepProgress);

        if (CurrentStepProgress >= 1f)
            CompleteCurrentStep();
    }

    public void SetHandProgress(WashStep step, Handedness hand, float normalized)
    {
        if (step != CurrentStep)
            return;

        float value = Mathf.Clamp01(normalized);

        if (hand == Handedness.Left)
        {
            if (Mathf.Approximately(LeftHandProgress, value)) return;
            LeftHandProgress = value;
        }
        else
        {
            if (Mathf.Approximately(RightHandProgress, value)) return;
            RightHandProgress = value;
        }

        OnHandProgressChanged?.Invoke(hand, value);
    }

    public void CompleteCurrentStep()
    {
        if (CurrentStep == WashStep.Complete)
            return;

        completedSteps.Add(CurrentStep);
        OnStepCompleted?.Invoke(CurrentStep);
        AdvanceStep();
    }

    public void AdvanceStep()
    {
        if (CurrentStep == WashStep.Complete)
            return;

        int next = (int)CurrentStep + 1;
        CurrentStep = next >= (int)WashStep.Complete ? WashStep.Complete : (WashStep)next;

        CurrentStepProgress = 0f;
        ResetHandProgress();

        OnProgressChanged?.Invoke(0f);
        OnStepChanged?.Invoke(CurrentStep);
    }

    public bool IsStepCompleted(WashStep step) => completedSteps.Contains(step);

    public string GetCurrentInstruction()
        => CurrentStep == WashStep.Complete
            ? "Hand washing complete!"
            : WashStepCatalog.GetInstruction(CurrentStep);

    public void ResetSimulation()
    {
        completedSteps.Clear();

        CurrentStep = WashStep.WetHands;
        CurrentStepProgress = 0f;
        TotalElapsedTime = 0f;
        ResetHandProgress();

        OnProgressChanged?.Invoke(0f);
        OnStepChanged?.Invoke(CurrentStep);
    }

    private void ResetHandProgress()
    {
        LeftHandProgress = 0f;
        RightHandProgress = 0f;
        OnHandProgressChanged?.Invoke(Handedness.Left, 0f);
        OnHandProgressChanged?.Invoke(Handedness.Right, 0f);
    }
}
