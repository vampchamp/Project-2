using UnityEngine;

public class SoapZone : MonoBehaviour
{
    public HandSoap handInZone;

    private void OnTriggerEnter(Collider other)
    {
        HandSoap hand =
            other.GetComponentInChildren<HandSoap>();
        if (hand != null)
        {
            handInZone = hand;

            Debug.Log("Hand entered zone: " + hand.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HandSoap hand =
            other.GetComponentInChildren<HandSoap>();
        
        if (hand == handInZone)
        {
            handInZone = null;
        }
    }
}