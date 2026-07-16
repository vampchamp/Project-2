using UnityEngine;

public class DryZone : MonoBehaviour
{
    [SerializeField] private float foamClearTime = 2f;
    [SerializeField] private GameObject towelVisual;

    private HandwashingManager manager;
    private int lastProgressFrame = -1;

    private void Start()
    {
        manager = HandwashingManager.Instance;
        if (manager == null)
            return;

        manager.OnStepCompleted += HandleStepCompleted;
        manager.OnSimulationReset += HandleSimulationReset;
    }

    private void OnDestroy()
    {
        if (manager == null)
            return;

        manager.OnStepCompleted -= HandleStepCompleted;
        manager.OnSimulationReset -= HandleSimulationReset;
    }

    private void HandleStepCompleted(WashStep step)
    {
        if (step == WashStep.Dry)
            SetDiscarded(true);
    }

    private void HandleSimulationReset() => SetDiscarded(false);

    private void SetDiscarded(bool discarded)
    {
        if (towelVisual != null)
            towelVisual.SetActive(!discarded);

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            col.enabled = !discarded;
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
