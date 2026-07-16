using UnityEngine;

public class FoamZone : MonoBehaviour
{
    [SerializeField] private float rinseFoamClearTime = 2.5f;

    private int lastProgressFrame = -1;

    private void OnTriggerStay(Collider other)
    {
        HandwashingManager manager = HandwashingManager.Instance;
        if (manager == null) return;

        WashStep step = manager.CurrentStep;
        if (step != WashStep.WetHands && step != WashStep.Rinse) return;

        HandFoam handFoam = other.GetComponentInChildren<HandFoam>();
        if (handFoam == null) handFoam = other.GetComponentInParent<HandFoam>();

        HandSoap handSoap = other.GetComponentInChildren<HandSoap>();
        if (handSoap == null) handSoap = other.GetComponentInParent<HandSoap>();

        if (handFoam == null && handSoap == null) return;

        bool firstThisFrame = lastProgressFrame != Time.frameCount;
        lastProgressFrame = Time.frameCount;

        if (step == WashStep.WetHands)
        {
            if (firstThisFrame)
                manager.AccumulateProgress(WashStep.WetHands, Time.deltaTime);

            if (handSoap != null) handSoap.SetWet();
        }
        else
        {
            if (firstThisFrame)
                manager.AccumulateProgress(WashStep.Rinse, Time.deltaTime);

            if (handFoam != null) handFoam.WashFoam(Time.deltaTime / rinseFoamClearTime);
            if (handSoap != null) handSoap.WashOffSoap();
        }
    }
}
