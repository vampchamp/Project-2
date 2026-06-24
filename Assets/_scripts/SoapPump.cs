using UnityEngine;

public class SoapPump : MonoBehaviour
{
    [Header("Pump Settings")]
    public float pressDistance = 0.03f;
    public float pressSpeed = 10f;

    private Vector3 startLocalPosition;
    private bool isPressed;
    private bool soapDispensed;
    

    public SoapZone soapZone;

    private void Start()
    {
        startLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        Vector3 targetPosition = startLocalPosition;

        if (isPressed)
        {
            targetPosition =
                startLocalPosition + Vector3.down * pressDistance;

            if (!soapDispensed &&
                soapZone.handInZone != null)
            {
                soapZone.handInZone.AddSoap();
                soapDispensed = true;
            }
        }

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * pressSpeed
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "SoapZone")
            return;

        if (other.name == "TriggerZone")
            return;

        isPressed = true;
        soapDispensed = false;
        if (HandwashingManager.Instance != null)
        {
            HandwashingManager.Instance.CompleteSoapStep();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        isPressed = false;
    }
    
}