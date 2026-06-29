using UnityEngine;

public class VRHandRubTracker : MonoBehaviour
{
    [SerializeField] private float movementThreshold = 0.1f; 
    [SerializeField] private float latherTime = 2.0f;       
    
    private Rigidbody rb;
    private HandFoam otherHandFoam;
    private bool handsAreTouching = false;
    Vector3 previousPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        previousPosition = transform.position;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Hand"))
            return;

        handsAreTouching = true;

        otherHandFoam = other.GetComponentInChildren<HandFoam>();
        Debug.Log(otherHandFoam);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hand") )
        {
            handsAreTouching = false;
        }
    }

    void Update()
    {
        HandwashingManager manager = HandwashingManager.Instance;
        if (manager == null || manager.CurrentStep != HandwashingManager.WashStep.RubPalms) return;

        if (handsAreTouching && IsMoving())
        {
            manager.AccumulateProgress(HandwashingManager.WashStep.RubPalms, Time.deltaTime);

            HandFoam myFoam = GetComponentInChildren<HandFoam>();
            if (myFoam != null)
                myFoam.BuildFoam(Time.deltaTime / latherTime);

            if (otherHandFoam != null)
                otherHandFoam.BuildFoam(Time.deltaTime / latherTime);
        }
    }

    private bool IsMoving()
    {
        float speed = Vector3.Distance(transform.position, previousPosition) / Time.deltaTime;

        previousPosition = transform.position;

        return speed > movementThreshold;
    }
}