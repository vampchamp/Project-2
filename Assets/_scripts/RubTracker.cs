using UnityEngine;

public class VRHandRubTracker : MonoBehaviour
{
    [SerializeField] private float movementThreshold = 0.1f; 
    [SerializeField] private float latherTime = 2.0f;       
    
    private Rigidbody rb;
    private bool handsAreTouching = false;
    Vector3 previousPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        previousPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            handsAreTouching = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hand") && other.transform.root != transform.root)
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

            HandFoam handFoam = GetComponentInChildren<HandFoam>();
            if (handFoam != null)
            {
                handFoam.BuildFoam(Time.deltaTime / latherTime);
            }
        }
    }

    private bool IsMoving()
    {
        float speed = Vector3.Distance(transform.position, previousPosition) / Time.deltaTime;

        previousPosition = transform.position;

        return speed > movementThreshold;
    }
}