using UnityEngine;

public class DryZone : MonoBehaviour
{
    [SerializeField] private float foamClearTime = 2f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Hand")) return;

        HandwashingManager manager = HandwashingManager.Instance;
        if (manager == null) return;

        if (manager.CurrentStep != HandwashingManager.WashStep.Dry) return;

        manager.AccumulateProgress(HandwashingManager.WashStep.Dry, Time.deltaTime);

        HandFoam foam = other.GetComponentInChildren<HandFoam>();
        if (foam != null)
            foam.WashFoam(Time.deltaTime / foamClearTime);
    }
}
