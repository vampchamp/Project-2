using UnityEngine;

public class FoamZone : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        HandFoam handFoam = other.GetComponentInChildren<HandFoam>();
        if (handFoam == null) handFoam = other.GetComponentInParent<HandFoam>();

        HandSoap handSoap = other.GetComponentInChildren<HandSoap>();
        if (handSoap == null) handSoap = other.GetComponentInParent<HandSoap>();

        if (handFoam == null && handSoap == null) return;

        HandwashingManager manager = HandwashingManager.Instance;
        if (manager == null) return;

        if (manager.CurrentStep == HandwashingManager.WashStep.WetHands)
        {
            manager.AccumulateProgress(HandwashingManager.WashStep.WetHands, Time.deltaTime);
            if (handSoap != null) handSoap.SetWet();
        }
        else if (manager.CurrentStep == HandwashingManager.WashStep.Rinse)
        {
            manager.AccumulateProgress(HandwashingManager.WashStep.Rinse, Time.deltaTime);

            if (handFoam != null) handFoam.WashFoam(Time.deltaTime / 2.5f);
            if (handSoap != null) handSoap.WashOffSoap();
        }
    }
}
