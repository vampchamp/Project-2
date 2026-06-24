using UnityEngine;

public class FoamZone : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        HandFoam handFoam = other.GetComponentInChildren<HandFoam>();
        if (handFoam == null) return;

        HandwashingManager manager = HandwashingManager.Instance;
        if (manager == null) return;

        // STEP 0: Wetting Hands
        if (manager.CurrentStep == HandwashingManager.WashStep.WetHands)
        {
            manager.AccumulateProgress(HandwashingManager.WashStep.WetHands, Time.deltaTime);
        }
        // STEP 3: Rinsing Soap & Foam Away
        else if (manager.CurrentStep == HandwashingManager.WashStep.Rinse)
        {
            manager.AccumulateProgress(HandwashingManager.WashStep.Rinse, Time.deltaTime);
            
            // Simultaneously wash away the visual foam spheres
            // (Using 2.5f as the wash duration)
            handFoam.WashFoam(Time.deltaTime / 2.5f);
        }
    }
}