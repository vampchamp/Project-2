using UnityEngine;

public class DryZone : MonoBehaviour
{
    [SerializeField] private float foamClearTime = 2f;

    private HandwashingManager manager;
    private int lastProgressFrame = -1;

    private void Start()
    {
        manager = HandwashingManager.Instance;
        if (manager != null)
            manager.OnStepChanged += HandleStepChanged;
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.OnStepChanged -= HandleStepChanged;
    }

    private void HandleStepChanged(WashStep step)
    {
        if (step != WashStep.Complete)
            return;

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (manager == null) return;
        if (!other.CompareTag("Hand")) return;
        if (manager.CurrentStep != WashStep.Dry) return;

        if (lastProgressFrame != Time.frameCount)
        {
            manager.AccumulateProgress(WashStep.Dry, Time.deltaTime);
            lastProgressFrame = Time.frameCount;
        }

        HandFoam foam = other.GetComponentInChildren<HandFoam>();
        if (foam != null)
            foam.WashFoam(Time.deltaTime / foamClearTime);
    }
}
