using UnityEngine;

public class RubTracker : MonoBehaviour
{
    [Header("WHO Step")]
    [SerializeField] private HandwashingManager.WashStep trackedStep;

    [Header("Settings")]
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField] private float proximityThreshold = 0.3f;
    [SerializeField] private float latherTime = 2f;
    [SerializeField] private bool requireHandsTogether = true;

    private HandSoap leftHand;
    private HandSoap rightHand;
    private HandFoam leftFoam;
    private HandFoam rightFoam;
    private Vector3 leftLast;
    private Vector3 rightLast;

    private void Start()
    {
        var hands = UnityEngine.Object.FindObjectsByType<HandSoap>(FindObjectsSortMode.None);
        foreach (var h in hands)
        {
            if (h.hand == Handedness.Left && leftHand == null) leftHand = h;
            else if (h.hand == Handedness.Right && rightHand == null) rightHand = h;
        }
        if (leftHand != null)
        {
            leftFoam = leftHand.GetComponentInChildren<HandFoam>();
            if (leftFoam == null) leftFoam = leftHand.GetComponentInParent<HandFoam>();
            leftLast = leftHand.transform.position;
        }
        if (rightHand != null)
        {
            rightFoam = rightHand.GetComponentInChildren<HandFoam>();
            if (rightFoam == null) rightFoam = rightHand.GetComponentInParent<HandFoam>();
            rightLast = rightHand.transform.position;
        }
        if (leftHand == null || rightHand == null)
            Debug.LogWarning("RubTracker (" + trackedStep + "): missing a HandSoap. Left=" + (leftHand != null) + " Right=" + (rightHand != null));
    }

    private void Update()
    {
        HandwashingManager manager = HandwashingManager.Instance;
        if (manager == null || manager.CurrentStep != trackedStep) return;
        if (leftHand == null || rightHand == null) return;

        Vector3 lp = leftHand.transform.position;
        Vector3 rp = rightHand.transform.position;

        float leftSpeed = Vector3.Distance(lp, leftLast) / Time.deltaTime;
        float rightSpeed = Vector3.Distance(rp, rightLast) / Time.deltaTime;
        leftLast = lp;
        rightLast = rp;

        if (requireHandsTogether && Vector3.Distance(lp, rp) > proximityThreshold)
            return;

        if (leftSpeed > movementThreshold)
        {
            manager.AccumulateHandProgress(trackedStep, Handedness.Left, Time.deltaTime);
            if (leftFoam != null) leftFoam.BuildFoam(Time.deltaTime / latherTime);
        }
        if (rightSpeed > movementThreshold)
        {
            manager.AccumulateHandProgress(trackedStep, Handedness.Right, Time.deltaTime);
            if (rightFoam != null) rightFoam.BuildFoam(Time.deltaTime / latherTime);
        }
    }
}
