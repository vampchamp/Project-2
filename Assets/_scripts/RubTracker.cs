using UnityEngine;

public class VRHandRubTracker : MonoBehaviour
{
    [SerializeField] private float movementThreshold = 0.1f; 
    [SerializeField] private float latherTime = 2.0f;       
    
    private Rigidbody rb;
    private bool handsAreTouching = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand") && other.transform.root != transform.root)
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
        if (rb != null)
        {
            return rb.linearVelocity.magnitude > movementThreshold;
        }
        return true; 
    }
}