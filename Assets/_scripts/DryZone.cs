using UnityEngine;

public class DryZone : MonoBehaviour
{
    [SerializeField] private float foamClearTime = 2f;

    private int lastProgressFrame = -1;

    private void Start()
    {
        var manager = HandwashingManager.Instance;
        if (manager != null)
            manager.OnStepChanged += HandleStepChanged;
    }

    private void OnDestroy()
    {
        var manager = HandwashingManager.Instance;
        if (manager != null)
            manager.OnStepChanged -= HandleStepChanged;
    }

    private void HandleStepChanged(HandwashingManager.WashStep step)
    {
        if (step == HandwashingManager.WashStep.Complete)
        {
            foreach (Collider col in GetComponentsInChildren<Collider>())
                col.enabled = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Hand")) return;

        HandwashingManager manager = HandwashingManager.Instance;
        if (manager == null) return;

        if (manager.CurrentStep != HandwashingManager.WashStep.Dry) return;

        if (lastProgressFrame != Time.frameCount)
        {
            manager.AccumulateProgress(HandwashingManager.WashStep.Dry, Time.deltaTime);
            lastProgressFrame = Time.frameCount;
        }
        
        HandFoam foam = other.GetComponentInChildren<HandFoam>();
        if (foam != null)
            foam.WashFoam(Time.deltaTime / foamClearTime);
        
    }
}
