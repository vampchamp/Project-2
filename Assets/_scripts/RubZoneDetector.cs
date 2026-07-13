using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RubZoneDetector : MonoBehaviour
{
    public enum HandZone { Palm, Dorsum, Fingers, FingerBacks, Thumb, Fingertips }

    [Header("Identity")]
    public Handedness hand;
    public HandZone zone;

    [Header("Contact Rule")]
    [SerializeField] private HandwashingManager.WashStep step;

    [SerializeField] private HandZone requiredOtherZone = HandZone.Palm;

    [SerializeField] private bool drivesProgress = false;

    [Header("Tuning")]
    [SerializeField] private float minSlideSpeed = 0.05f;

    [SerializeField] private float contactGrace = 0.2f;

    private RubZoneDetector other;
    private float contactTimer;
    private Vector3 lastPos, otherLastPos;

    private bool IsValidPartner(RubZoneDetector p)
        => p != null && p.hand != hand && p.zone == requiredOtherZone;

    private void OnEnable()
    {
        HandwashingManager.RegisterColliderStep(step);
    }

    private void OnTriggerEnter(Collider c) => TryLatch(c);
    private void OnTriggerStay(Collider c) => TryLatch(c);

    private void TryLatch(Collider c)
    {
        var p = c.GetComponentInParent<RubZoneDetector>();
        if (IsValidPartner(p))
        {
            other = p;
            contactTimer = contactGrace;
        }
    }

    private void Update()
    {
        if (contactTimer > 0f)
            contactTimer -= Time.deltaTime;

        var mgr = HandwashingManager.Instance;

        if (drivesProgress && other != null && contactTimer > 0f &&
            mgr != null && mgr.CurrentStep == step && Time.deltaTime > 0f)
        {
            Vector3 myDelta = transform.position - lastPos;
            Vector3 otherDelta = other.transform.position - otherLastPos;
            float relSpeed = (myDelta - otherDelta).magnitude / Time.deltaTime;

            if (relSpeed >= minSlideSpeed)
                mgr.AccumulateProgress(step, Time.deltaTime);
        }

        lastPos = transform.position;
        if (other != null) otherLastPos = other.transform.position;
    }
}
