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

    public static HandwashingManager Instance { get; private set; }

    [Header("State")]
    public WashStep CurrentStep { get; private set; } = WashStep.WetHands;
    public float CurrentStepProgress { get; private set; } = 0f; // Normalized 0.0 to 1.0

    [Header("Target Durations (Seconds)")]
    [SerializeField] private float wetHandsDuration = 2f;
    [SerializeField] private float rubPalmsDuration = 5f; // Shortened for gameplay flow
    [SerializeField] private float rinseDuration = 3f;

    // Events for UI/Audio/VFX to hook into
    public event Action<WashStep> OnStepChanged;
    public event Action<float> OnProgressChanged; // Sends 0.0 - 1.0 float for progress bars/fills
    public event Action OnStepCompleted;          // Perfect for triggering a "ding!" sound

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
        ResetProgress();
        OnStepChanged?.Invoke(CurrentStep);
        // DIAGNOSTIC LOG
        Debug.LogWarning($"[HANDWASH] Game Started! Initial Step is: {CurrentStep}");
    }


    public void AccumulateProgress(WashStep trackingStep, float deltaTime)
    {
        if (trackingStep != CurrentStep || CurrentStep == WashStep.Complete) return;

        float duration = GetTargetDuration(CurrentStep);
        if (duration <= 0f) return;

        CurrentStepProgress += deltaTime / duration;
        CurrentStepProgress = Mathf.Clamp01(CurrentStepProgress);
        
        OnProgressChanged?.Invoke(CurrentStepProgress);

        if (CurrentStepProgress >= 1f)
        {
            OnStepCompleted?.Invoke();
            AdvanceStep();
        }
    }

    public void AdvanceStep()
    {
        if (CurrentStep == WashStep.Complete) return;

        // Total number of elements in our enum
        int totalSteps = Enum.GetNames(typeof(WashStep)).Length;
        int nextStepIndex = (int)CurrentStep + 1;

        if (nextStepIndex < totalSteps)
        {
            CurrentStep = (WashStep)nextStepIndex;
            ResetProgress();
            OnStepChanged?.Invoke(CurrentStep);
            Debug.Log($"Step Advanced! New Step: {CurrentStep}");
        }
    }

    // Direct manual bypass if needed for soap dispensing which is a 1-shot action
    public void CompleteSoapStep()
    {
        if (CurrentStep == WashStep.ApplySoap)
        {
            CurrentStepProgress = 1f;
            OnProgressChanged?.Invoke(1f);
            OnStepCompleted?.Invoke();
            AdvanceStep();
        }
    }

    private void ResetProgress()
    {
        CurrentStepProgress = 0f;
        OnProgressChanged?.Invoke(0f);
    }

    private float GetTargetDuration(WashStep step)
    {
        return step switch
        {
            WashStep.WetHands => wetHandsDuration,
            WashStep.RubPalms => rubPalmsDuration,
            WashStep.Rinse => rinseDuration,
            _ => 0f // Soap and Complete don't use time accumulation
        };
    }
}