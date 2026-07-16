using System.Collections.Generic;
using UnityEngine;

public class FoamBuilder : MonoBehaviour
{
    [SerializeField] private float latherRate = 1.5f;

    private HandwashingManager manager;
    private HandFoam leftFoam;
    private HandFoam rightFoam;
    private readonly List<HandSoap> soaps = new();
    private float lastProgress;

    private void Start()
    {
        foreach (HandSoap h in FindObjectsByType<HandSoap>(FindObjectsSortMode.None))
        {
            soaps.Add(h);

            HandFoam foam = h.GetComponentInChildren<HandFoam>();
            if (foam == null) foam = h.GetComponentInParent<HandFoam>();

            if (h.hand == Handedness.Left && leftFoam == null) leftFoam = foam;
            else if (h.hand == Handedness.Right && rightFoam == null) rightFoam = foam;
        }

        manager = HandwashingManager.Instance;
        if (manager == null)
            return;

        manager.OnProgressChanged += OnProgress;
        manager.OnStepChanged += OnStep;
        manager.OnSimulationReset += OnSimulationReset;
    }

    private void OnDestroy()
    {
        if (manager == null)
            return;

        manager.OnProgressChanged -= OnProgress;
        manager.OnStepChanged -= OnStep;
        manager.OnSimulationReset -= OnSimulationReset;
    }

    private void OnSimulationReset()
    {
        lastProgress = 0f;

        if (leftFoam != null) leftFoam.ResetFoam();
        if (rightFoam != null) rightFoam.ResetFoam();

        foreach (HandSoap soap in soaps)
        {
            if (soap != null)
                soap.ResetHand();
        }
    }

    private void OnStep(WashStep step)
    {
        lastProgress = 0f;
    }

    private void OnProgress(float progress)
    {
        float delta = progress - lastProgress;
        lastProgress = progress;

        if (delta <= 0f || !WashStepCatalog.IsRubbingStep(manager.CurrentStep))
            return;

        float amount = delta * latherRate;
        if (leftFoam != null) leftFoam.BuildFoam(amount);
        if (rightFoam != null) rightFoam.BuildFoam(amount);
    }
}
