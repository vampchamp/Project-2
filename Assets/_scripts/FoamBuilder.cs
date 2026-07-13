using UnityEngine;

public class FoamBuilder : MonoBehaviour
{
    [SerializeField] private float latherRate = 1.5f;

    private HandFoam leftFoam;
    private HandFoam rightFoam;
    private float lastProgress;

    private void Start()
    {
        foreach (var h in FindObjectsByType<HandSoap>(FindObjectsSortMode.None))
        {
            HandFoam foam = h.GetComponentInChildren<HandFoam>();
            if (foam == null) foam = h.GetComponentInParent<HandFoam>();

            if (h.hand == Handedness.Left && leftFoam == null) leftFoam = foam;
            else if (h.hand == Handedness.Right && rightFoam == null) rightFoam = foam;
        }

        var mgr = HandwashingManager.Instance;
        if (mgr != null)
        {
            mgr.OnProgressChanged += OnProgress;
            mgr.OnStepChanged += OnStep;
        }
    }

    private void OnDestroy()
    {
        var mgr = HandwashingManager.Instance;
        if (mgr != null)
        {
            mgr.OnProgressChanged -= OnProgress;
            mgr.OnStepChanged -= OnStep;
        }
    }

    private void OnStep(HandwashingManager.WashStep step)
    {
        lastProgress = 0f;
    }

    private void OnProgress(float progress)
    {
        var mgr = HandwashingManager.Instance;
        if (mgr != null && mgr.IsRubbingStep(mgr.CurrentStep))
        {
            float delta = progress - lastProgress;
            if (delta > 0f)
            {
                float amount = delta * latherRate;
                if (leftFoam != null) leftFoam.BuildFoam(amount);
                if (rightFoam != null) rightFoam.BuildFoam(amount);
            }
        }

        lastProgress = progress;
    }
}
