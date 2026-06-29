using UnityEngine;

public class RubTracker : MonoBehaviour
{
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

        if (manager.CurrentStep != HandwashingManager.WashStep.RubPalms)
            return;

        bool moving = IsMoving();

        if (!handsAreTouching || !moving)
            return;

        manager.AccumulateProgress(
            HandwashingManager.WashStep.RubPalms,
            Time.deltaTime);

        float amount = Time.deltaTime / latherTime;

        if (myFoam != null)
            myFoam.BuildFoam(amount);
        
    }

    private bool IsMoving()
    {
        float speed =
            Vector3.Distance(transform.position, previousPosition)
            / Time.deltaTime;

        previousPosition = transform.position;

        return speed > movementThreshold;
    }
}