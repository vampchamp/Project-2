using UnityEngine;

// Drives the final WHO step (Dry). Place this on a trigger volume at the
// towel / air-dryer. While a hand tagged 'Hand' stays inside and the current
// step is Dry, progress accumulates and any residual foam is cleared.
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
