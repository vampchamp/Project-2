using UnityEngine;

public class FoamZone : MonoBehaviour
{
    public float washTime = 1f;

    private void OnTriggerEnter(Collider other)
    {
        HandSoap handSoap =
            other.GetComponentInChildren<HandSoap>();

        HandFoam handFoam =
            other.GetComponentInChildren<HandFoam>();

        if (handFoam != null)
        {
            handFoam.AddFoam();
        }

        if (handSoap != null)
        {
            handSoap.WashOffSoap();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        HandFoam handFoam =
            other.GetComponentInChildren<HandFoam>();

        if (handFoam != null)
        {
            handFoam.WashFoam(Time.deltaTime / washTime);
        }
    }
}