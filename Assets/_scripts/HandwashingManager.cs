using System;
using System.Collections.Generic;
using UnityEngine;

public class HandwashingManager : MonoBehaviour
{
    public enum WashStep
    {
        WetHands,
        ApplySoap,
        PalmToPalm,
        BackOfHands,
        FingersInterlaced,
        BacksOfFingers,
        Thumbs,
        Fingertips,
        Rinse,
        Dry,
        Complete
    }

    public static HandwashingManager Instance { get; private set; }

    [Header("Current State")]
    public WashStep CurrentStep { get; private set; } = WashStep.WetHands;
    public float CurrentStepProgress { get; private set; }
    public float TotalElapsedTime { get; private set; }

    public event Action<WashStep> OnStepChanged;
    public event Action<float> OnProgressChanged;
    public event Action<WashStep> OnStepCompleted;

    private readonly HashSet<WashStep> completedSteps = new();

    public static readonly Dictionary<WashStep, string> StepInstructions = new()
    {
        { WashStep.WetHands, "Wet your hands thoroughly." },
        { WashStep.ApplySoap, "Apply enough soap." },
        { WashStep.PalmToPalm, "Rub palm to palm." },
        { WashStep.BackOfHands, "Rub the back of each hand." },
        { WashStep.FingersInterlaced, "Rub with fingers interlaced." },
        { WashStep.BacksOfFingers, "Rub backs of fingers." },
        { WashStep.Thumbs, "Rub each thumb rotationally." },
        { WashStep.Fingertips, "Rub fingertips in opposite palm." },
        { WashStep.Rinse, "Rinse all soap away." },
        { WashStep.Dry, "Dry your hands." }
    };

    private readonly Dictionary<WashStep, float> stepDurations = new()
    {
        { WashStep.WetHands, 2f },
        { WashStep.ApplySoap, 0f }, // instant
        { WashStep.PalmToPalm, 3f },
        { WashStep.BackOfHands, 3f },
        { WashStep.FingersInterlaced, 3f },
        { WashStep.BacksOfFingers, 3f },
        { WashStep.Thumbs, 3f },
        { WashStep.Fingertips, 3f },
        { WashStep.Rinse, 3f },
        { WashStep.Dry, 2f }
    };

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
        CurrentStepProgress = 0f;
        OnStepChanged?.Invoke(CurrentStep);
    }

    private void Update()
    {
        if (CurrentStep != WashStep.Complete)
            TotalElapsedTime += Time.deltaTime;
    }

    public void AccumulateProgress(WashStep step, float deltaTime)
    {
        if (step != CurrentStep)
            return;

        float duration = stepDurations[step];

        if (duration <= 0f)
            return;

        CurrentStepProgress += deltaTime / duration;
        CurrentStepProgress = Mathf.Clamp01(CurrentStepProgress);

        OnProgressChanged?.Invoke(CurrentStepProgress);

        if (CurrentStepProgress >= 1f)
            CompleteCurrentStep();
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

        if (next >= Enum.GetValues(typeof(WashStep)).Length - 1)
        {
            CurrentStep = WashStep.Complete;
        }
        else
        {
            CurrentStep = (WashStep)next;
        }

        CurrentStepProgress = 0f;

        OnProgressChanged?.Invoke(0f);
        OnStepChanged?.Invoke(CurrentStep);
    }

    public bool IsStepCompleted(WashStep step)
    {
        return completedSteps.Contains(step);
    }

    public string GetCurrentInstruction()
    {
        if (CurrentStep == WashStep.Complete)
            return "Hand washing complete!";

        return StepInstructions[CurrentStep];
    }

    public bool IsRubbingStep(WashStep step)
    {
        return step == WashStep.PalmToPalm ||
               step == WashStep.BackOfHands ||
               step == WashStep.FingersInterlaced ||
               step == WashStep.BacksOfFingers ||
               step == WashStep.Thumbs ||
               step == WashStep.Fingertips;
    }

    public void ResetSimulation()
    {
        completedSteps.Clear();

        CurrentStep = WashStep.WetHands;
        CurrentStepProgress = 0f;
        TotalElapsedTime = 0f;

        OnProgressChanged?.Invoke(0f);
        OnStepChanged?.Invoke(CurrentStep);
    }
}