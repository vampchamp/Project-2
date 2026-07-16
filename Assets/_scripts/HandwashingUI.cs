using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class HandwashingUI : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private bool followCamera = true;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.05f, 1.2f);

    [Header("Texts")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text timerText;

    [Header("Progress")]
    [SerializeField] private Image progressFill;

    [Header("Per-Hand Progress (used on rubbing steps)")]
    [SerializeField] private Image leftHandFill;
    [SerializeField] private Image rightHandFill;
    [SerializeField] private TMP_Text leftHandLabel;
    [SerializeField] private TMP_Text rightHandLabel;

    [Header("WHO Steps")]
    [SerializeField] private TMP_Text wetHands;
    [SerializeField] private TMP_Text applySoap;
    [SerializeField] private TMP_Text palmToPalm;
    [SerializeField] private TMP_Text backOfHands;
    [SerializeField] private TMP_Text fingersInterlaced;
    [SerializeField] private TMP_Text backsOfFingers;
    [SerializeField] private TMP_Text thumbs;
    [SerializeField] private TMP_Text fingertips;
    [SerializeField] private TMP_Text rinse;
    [SerializeField] private TMP_Text dry;

    private Dictionary<WashStep, TMP_Text> stepTexts;
    private HandwashingManager manager;
    private int lastDisplayedSecond = -1;

    private void Start()
    {
        if (followCamera && followTarget == null && Camera.main != null)
            followTarget = Camera.main.transform;

        if (leftHandLabel != null) leftHandLabel.text = "Left";
        if (rightHandLabel != null) rightHandLabel.text = "Right";

        stepTexts = new Dictionary<WashStep, TMP_Text>
        {
            { WashStep.WetHands, wetHands },
            { WashStep.ApplySoap, applySoap },
            { WashStep.PalmToPalm, palmToPalm },
            { WashStep.BackOfHands, backOfHands },
            { WashStep.FingersInterlaced, fingersInterlaced },
            { WashStep.BacksOfFingers, backsOfFingers },
            { WashStep.Thumbs, thumbs },
            { WashStep.Fingertips, fingertips },
            { WashStep.Rinse, rinse },
            { WashStep.Dry, dry }
        };

        manager = HandwashingManager.Instance;
        if (manager == null)
        {
            enabled = false;
            return;
        }

        manager.OnStepChanged += UpdateCurrentStep;
        manager.OnProgressChanged += UpdateProgress;
        manager.OnHandProgressChanged += UpdateHandProgress;

        UpdateCurrentStep(manager.CurrentStep);
        UpdateProgress(manager.CurrentStepProgress);
        UpdateHandProgress(Handedness.Left, manager.LeftHandProgress);
        UpdateHandProgress(Handedness.Right, manager.RightHandProgress);
    }

    private void OnDestroy()
    {
        if (manager == null)
            return;

        manager.OnStepChanged -= UpdateCurrentStep;
        manager.OnProgressChanged -= UpdateProgress;
        manager.OnHandProgressChanged -= UpdateHandProgress;
    }

    private void LateUpdate()
    {
        if (followCamera && followTarget != null)
        {
            transform.position = followTarget.TransformPoint(offset);
            transform.rotation = followTarget.rotation;
        }

        UpdateTimer();
    }

    private void UpdateCurrentStep(WashStep current)
    {
        instructionText.text = manager.GetCurrentInstruction();

        bool perSide = WashStepCatalog.GetRubMode(current) == RubMode.PerSide;

        if (leftHandFill != null) leftHandFill.gameObject.SetActive(perSide);
        if (rightHandFill != null) rightHandFill.gameObject.SetActive(perSide);
        if (leftHandLabel != null) leftHandLabel.gameObject.SetActive(perSide);
        if (rightHandLabel != null) rightHandLabel.gameObject.SetActive(perSide);

        foreach (KeyValuePair<WashStep, TMP_Text> pair in stepTexts)
        {
            if (pair.Value == null)
                continue;

            string label = WashStepCatalog.GetDisplayName(pair.Key);

            if (manager.IsStepCompleted(pair.Key))
            {
                pair.Value.text = "✓ " + label;
                pair.Value.color = Color.green;
            }
            else if (pair.Key == current)
            {
                pair.Value.text = "► " + label;
                pair.Value.color = Color.yellow;
            }
            else
            {
                pair.Value.text = "○ " + label;
                pair.Value.color = Color.white;
            }
        }
    }

    private void UpdateProgress(float progress)
    {
        if (progressFill != null)
            progressFill.fillAmount = progress;
    }

    private void UpdateHandProgress(Handedness hand, float value)
    {
        Image fill = hand == Handedness.Left ? leftHandFill : rightHandFill;
        if (fill != null)
            fill.fillAmount = value;
    }

    private void UpdateTimer()
    {
        int total = Mathf.FloorToInt(manager.TotalElapsedTime);
        if (total == lastDisplayedSecond)
            return;

        lastDisplayedSecond = total;
        timerText.text = $"{total / 60:00}:{total % 60:00}";
    }
}
