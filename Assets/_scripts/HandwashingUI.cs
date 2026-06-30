using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;


public class HandwashingUI : MonoBehaviour
{
    [Header("Follow")] [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.05f, 1.2f);

    [Header("Texts")] [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text timerText;

    [Header("Progress")] [SerializeField] private Image progressFill;

    [Header("WHO Steps")] [SerializeField] private TMP_Text wetHands;
    [SerializeField] private TMP_Text applySoap;
    [SerializeField] private TMP_Text palmToPalm;
    [SerializeField] private TMP_Text backOfHands;
    [SerializeField] private TMP_Text fingersInterlaced;
    [SerializeField] private TMP_Text backsOfFingers;
    [SerializeField] private TMP_Text thumbs;
    [SerializeField] private TMP_Text fingertips;
    [SerializeField] private TMP_Text rinse;
    [SerializeField] private TMP_Text dry;

    private Dictionary<HandwashingManager.WashStep, TMP_Text> stepTexts;

    private void Start()
    {
        if (followTarget == null)
            followTarget = Camera.main.transform;

        stepTexts = new Dictionary<HandwashingManager.WashStep, TMP_Text>()
        {
            { HandwashingManager.WashStep.WetHands, wetHands },
            { HandwashingManager.WashStep.ApplySoap, applySoap },
            { HandwashingManager.WashStep.PalmToPalm, palmToPalm },
            { HandwashingManager.WashStep.BackOfHands, backOfHands },
            { HandwashingManager.WashStep.FingersInterlaced, fingersInterlaced },
            { HandwashingManager.WashStep.BacksOfFingers, backsOfFingers },
            { HandwashingManager.WashStep.Thumbs, thumbs },
            { HandwashingManager.WashStep.Fingertips, fingertips },
            { HandwashingManager.WashStep.Rinse, rinse },
            { HandwashingManager.WashStep.Dry, dry }
        };

        HandwashingManager manager = HandwashingManager.Instance;

        manager.OnStepChanged += UpdateCurrentStep;
        manager.OnProgressChanged += UpdateProgress;

        UpdateCurrentStep(manager.CurrentStep);
        UpdateProgress(manager.CurrentStepProgress);
    }

    private void LateUpdate()
    {
        if (followTarget == null)
            return;

        transform.position =
            followTarget.TransformPoint(offset);

        transform.rotation =
            followTarget.rotation;

        UpdateTimer();
    }

    private void UpdateCurrentStep(HandwashingManager.WashStep current)
    {
        instructionText.text =
            HandwashingManager.Instance.GetCurrentInstruction();

        foreach (var pair in stepTexts)
        {
            if (HandwashingManager.Instance.IsStepCompleted(pair.Key))
            {
                pair.Value.text = "✓ " + Format(pair.Key);
                pair.Value.color = Color.green;
            }
            else if (pair.Key == current)
            {
                pair.Value.text = "► " + Format(pair.Key);
                pair.Value.color = Color.yellow;
            }
            else
            {
                pair.Value.text = "○ " + Format(pair.Key);
                pair.Value.color = Color.white;
            }
        }
    }

    private void UpdateProgress(float progress)
    {
        progressFill.fillAmount = progress;
    }

    private void UpdateTimer()
    {
        var manager = HandwashingManager.Instance;

        int m = Mathf.FloorToInt(manager.TotalElapsedTime / 60f);
        int s = Mathf.FloorToInt(manager.TotalElapsedTime % 60f);

        timerText.text = $"{m:00}:{s:00}";
    }

    private string Format(HandwashingManager.WashStep step)
    {
        switch (step)
        {
            case HandwashingManager.WashStep.WetHands:
                return "Wet Hands";

            case HandwashingManager.WashStep.ApplySoap:
                return "Apply Soap";

            case HandwashingManager.WashStep.PalmToPalm:
                return "Palm to Palm";

            case HandwashingManager.WashStep.BackOfHands:
                return "Back of Hands";

            case HandwashingManager.WashStep.FingersInterlaced:
                return "Fingers Interlaced";

            case HandwashingManager.WashStep.BacksOfFingers:
                return "Backs of Fingers";

            case HandwashingManager.WashStep.Thumbs:
                return "Thumb Rotation";

            case HandwashingManager.WashStep.Fingertips:
                return "Fingertips";

            case HandwashingManager.WashStep.Rinse:
                return "Rinse";

            case HandwashingManager.WashStep.Dry:
                return "Dry";

            default:
                return step.ToString();
        }
    }

    private void OnDestroy()
    {
        if (HandwashingManager.Instance == null)
            return;

        HandwashingManager.Instance.OnStepChanged -= UpdateCurrentStep;
        HandwashingManager.Instance.OnProgressChanged -= UpdateProgress;
    }
}