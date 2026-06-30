using UnityEngine;

public class RubTracker : MonoBehaviour
{
    [Header("WHO Step")]
    [SerializeField] private HandwashingManager.WashStep trackedStep;

    [Header("Settings")]
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField] private float latherTime = 2f;

    private bool handsAreTouching;
    private Vector3 previousPosition;

    private HandFoam myFoam;

    private void Start()
    {
        previousPosition = transform.position;

        myFoam = GetComponentInParent<HandFoam>();

        if (myFoam == null)
            myFoam = GetComponentInChildren<HandFoam>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Hand"))
            return;

        handsAreTouching = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Hand"))
            return;

        handsAreTouching = false;
    }

    private void Update()
    {
        HandwashingManager manager = HandwashingManager.Instance;

        if (manager == null)
            return;

        if (manager.CurrentStep != trackedStep)
            return;

        if (!handsAreTouching)
            return;

        if (!IsMoving())
            return;

        manager.AccumulateProgress(trackedStep, Time.deltaTime);

        if (myFoam != null)
        {
            myFoam.BuildFoam(Time.deltaTime / latherTime);
        }
    }

    private bool IsMoving()
    {
        float speed =
            Vector3.Distance(transform.position, previousPosition) /
            Time.deltaTime;

        previousPosition = transform.position;

        return speed > movementThreshold;
    }
}