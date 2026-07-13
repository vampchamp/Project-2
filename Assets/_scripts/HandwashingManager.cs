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

    public float LeftHandProgress { get; private set; }
    public float RightHandProgress { get; private set; }

    [Header("Absolute Workspace Tuning (World/Tracking Space)")]
    [SerializeField] private float maxDistance = 0.30f;
    [SerializeField] private float targetScrubDistance = 0.35f;

    [SerializeField] private bool useAbsoluteProximityOnly = true;

    public event Action<WashStep> OnStepChanged;
    public event Action<float> OnProgressChanged;
    public event Action<WashStep> OnStepCompleted;
    public event Action<Handedness, float> OnHandProgressChanged;

    private readonly HashSet<WashStep> completedSteps = new();

    private static readonly HashSet<WashStep> colliderDrivenSteps = new();
    public static void RegisterColliderStep(WashStep step) => colliderDrivenSteps.Add(step);

    private MonoBehaviour xrHandManagerInstance;
    private bool searchedForComponents = false;

    private Vector3 lastLeftPalmPos;
    private Vector3 lastRightPalmPos;
    private bool standardPositionsInitialized = false;

    public static readonly Dictionary<WashStep, string> StepInstructions = new()
    {
        { WashStep.WetHands, "Wet hands with water." },
        { WashStep.ApplySoap, "Press the soap pump and apply enough soap to cover all hand surfaces." },
        { WashStep.PalmToPalm, "Rub hands palm to palm." },
        { WashStep.BackOfHands, "Rub right palm over left dorsum, then left palm over right dorsum." },
        { WashStep.FingersInterlaced, "Rub palm to palm with fingers interlaced." },
        { WashStep.BacksOfFingers, "Rub backs of fingers to opposing palms with fingers interlocked." },
        { WashStep.Thumbs, "Rotationally rub each thumb clasped in the opposite palm." },
        { WashStep.Fingertips, "Rotationally rub fingertips backwards and forwards in the opposite palm." },
        { WashStep.Rinse, "Rinse hands with water." },
        { WashStep.Dry, "Dry hands thoroughly, then use the towel to turn off the faucet." }
    };

    private readonly Dictionary<WashStep, float> stepDurations = new()
    {
        { WashStep.WetHands, 2f },
        { WashStep.ApplySoap, 1.5f },
        { WashStep.PalmToPalm, 4f },
        { WashStep.BackOfHands, 4f },
        { WashStep.FingersInterlaced, 4f },
        { WashStep.BacksOfFingers, 4f },
        { WashStep.Thumbs, 4f },
        { WashStep.Fingertips, 4f },
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
        LeftHandProgress = 0f;
        RightHandProgress = 0f;
        OnStepChanged?.Invoke(CurrentStep);
    }

    private void Update()
    {
        if (CurrentStep != WashStep.Complete)
            TotalElapsedTime += Time.deltaTime;

        if (IsRubbingStep(CurrentStep) &&
            CurrentStep != WashStep.PalmToPalm &&
            !colliderDrivenSteps.Contains(CurrentStep))
        {
            ProcessVRHandTrackingInput();
        }
    }

    private void ProcessVRHandTrackingInput()
    {
        if (!searchedForComponents)
        {
            searchedForComponents = true;
            foreach (var mono in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mono.GetType().Name == "XRHandManager")
                {
                    xrHandManagerInstance = mono;
                    break;
                }
            }
        }

        if (xrHandManagerInstance == null) return;

        try
        {
            var subsystemProp = xrHandManagerInstance.GetType().GetProperty("subsystem");
            if (subsystemProp == null) return;

            var subsystem = subsystemProp.GetValue(xrHandManagerInstance);
            if (subsystem == null) return;

            var leftHand = subsystem.GetType().GetProperty("leftHand")?.GetValue(subsystem);
            var rightHand = subsystem.GetType().GetProperty("rightHand")?.GetValue(subsystem);
            if (leftHand == null || rightHand == null) return;

            bool leftTracked = (bool)(leftHand.GetType().GetProperty("isTracked")?.GetValue(leftHand) ?? false);
            bool rightTracked = (bool)(rightHand.GetType().GetProperty("isTracked")?.GetValue(rightHand) ?? false);

            if (!leftTracked && !rightTracked) return;

            var getJointMethod = leftHand.GetType().GetMethod("GetJoint");
            if (getJointMethod == null) return;

            var leftPalmJoint = getJointMethod.Invoke(leftHand, new object[] { 2 });
            var rightPalmJoint = getJointMethod.Invoke(rightHand, new object[] { 2 });
            if (leftPalmJoint == null || rightPalmJoint == null) return;

            var tryGetPoseMethod = leftPalmJoint.GetType().GetMethod("TryGetPose");
            if (tryGetPoseMethod == null) return;

            object[] leftArgs = new object[] { default(Pose) };
            object[] rightArgs = new object[] { default(Pose) };

            bool leftValid = (bool)tryGetPoseMethod.Invoke(leftPalmJoint, leftArgs);
            bool rightValid = (bool)tryGetPoseMethod.Invoke(rightPalmJoint, rightArgs);

            Pose leftPose = leftValid ? (Pose)leftArgs[0] : default;
            Pose rightPose = rightValid ? (Pose)rightArgs[0] : default;

            if (!standardPositionsInitialized)
            {
                lastLeftPalmPos = leftPose.position;
                lastRightPalmPos = rightPose.position;
                standardPositionsInitialized = true;
                return;
            }

            float leftFrameMovement = leftTracked && leftValid ? Vector3.Distance(leftPose.position, lastLeftPalmPos) : 0f;
            float rightFrameMovement = rightTracked && rightValid ? Vector3.Distance(rightPose.position, lastRightPalmPos) : 0f;

            if (leftFrameMovement > 0.12f) leftFrameMovement = 0f;
            if (rightFrameMovement > 0.12f) rightFrameMovement = 0f;

            if (leftTracked && leftValid) lastLeftPalmPos = leftPose.position;
            if (rightTracked && rightValid) lastRightPalmPos = rightPose.position;

            if (leftTracked && rightTracked && leftValid && rightValid)
            {
                float palmDistance = Vector3.Distance(leftPose.position, rightPose.position);

                if (palmDistance <= maxDistance)
                {
                    float frameScrubDelta = leftFrameMovement + rightFrameMovement;
                    if (frameScrubDelta > 0.0003f)
                    {
                        AccumulateUnifiedScrubProgress(frameScrubDelta);
                    }
                }
            }
            else if (useAbsoluteProximityOnly)
            {
                float singleHandDelta = Mathf.Max(leftFrameMovement, rightFrameMovement);
                if (singleHandDelta > 0.0003f)
                {
                    AccumulateUnifiedScrubProgress(singleHandDelta * 1.5f);
                }
            }
        }
        catch
        {
        }
    }

    private void AccumulateUnifiedScrubProgress(float deltaAmount)
    {
        float progressAddition = (deltaAmount / targetScrubDistance);
        CurrentStepProgress = Mathf.Clamp01(CurrentStepProgress + progressAddition);

        LeftHandProgress = CurrentStepProgress;
        RightHandProgress = CurrentStepProgress;

        OnProgressChanged?.Invoke(CurrentStepProgress);
        OnHandProgressChanged?.Invoke(Handedness.Left, LeftHandProgress);
        OnHandProgressChanged?.Invoke(Handedness.Right, RightHandProgress);

        if (CurrentStepProgress >= 1f)
            CompleteCurrentStep();
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

    public void AccumulateHandProgress(WashStep step, Handedness hand, float progressAmount)
    {
        if (step != CurrentStep) return;
        float normalizedDelta = progressAmount / stepDurations[step];
        AccumulateUnifiedScrubProgress(normalizedDelta * targetScrubDistance);
    }

    public float GetStepDuration(WashStep step)
        => stepDurations.TryGetValue(step, out float d) ? d : 0f;

    public void SetProgress(WashStep step, float normalized)
    {
        if (step != CurrentStep) return;
        CurrentStepProgress = Mathf.Clamp01(normalized);
        OnProgressChanged?.Invoke(CurrentStepProgress);
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
            CurrentStep = WashStep.Complete;
        else
            CurrentStep = (WashStep)next;

        CurrentStepProgress = 0f;
        standardPositionsInitialized = false;
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
        standardPositionsInitialized = false;
        ResetHandProgress();

        OnProgressChanged?.Invoke(0f);
        OnStepChanged?.Invoke(CurrentStep);
    }
}
