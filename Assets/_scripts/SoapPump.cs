using UnityEngine;

public class SoapPump : MonoBehaviour
{
    [Header("Pump Settings")]
    [SerializeField] private float pressDistance = 0.03f;
    [SerializeField] private float pressSpeed = 10f;

    [SerializeField] private SoapZone soapZone;

    private Vector3 startLocalPosition;

    private bool isPressed;
    private bool soapDispensed;

    private void Start()
    {
        startLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        Vector3 targetPosition = startLocalPosition;

        if (isPressed)
        {
            targetPosition = startLocalPosition + Vector3.down * pressDistance;

            if (!soapDispensed &&
                soapZone != null &&
                soapZone.handInZone != null)
            {
                soapZone.handInZone.AddSoap();

                soapDispensed = true;

                HandwashingManager manager = HandwashingManager.Instance;

                if (manager != null &&
                    manager.CurrentStep == HandwashingManager.WashStep.ApplySoap)
                {
                    manager.CompleteCurrentStep();
                }
            }
        }

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * pressSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == soapZone.gameObject)
            return;

        if (other.CompareTag("Hand"))
        {
            isPressed = true;
            soapDispensed = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            isPressed = false;
        }
    }
}