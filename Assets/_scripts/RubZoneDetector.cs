using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RubZoneDetector : MonoBehaviour
{
    public enum HandZone { Palm, Dorsum, Fingers, FingerBacks, Thumb, Fingertips }

    [Header("Identity")]
    [SerializeField] private Handedness hand;
    [SerializeField] private HandZone zone;

    [Header("Contact Rule")]
    [SerializeField] private WashStep step;
    [SerializeField] private HandZone requiredOtherZone = HandZone.Palm;
    [SerializeField] private bool drivesProgress;

    [Header("Tuning")]
    [SerializeField] private float minSlideSpeed = 0.05f;
    [SerializeField] private float contactGrace = 0.2f;

    public Handedness Hand => hand;
    public HandZone Zone => zone;
    public WashStep Step => step;
    public bool DrivesProgress => drivesProgress;

    public bool IsRubbing { get; private set; }

    private RubZoneDetector partner;
    private float contactTimer;
    private Vector3 lastPos;
    private Vector3 partnerLastPos;

    private void OnEnable()
    {
        lastPos = transform.position;
        RubStepCoordinator.Register(this);
    }

    private void OnDisable()
    {
        RubStepCoordinator.Unregister(this);
        IsRubbing = false;
        partner = null;
        contactTimer = 0f;
    }

    private void OnTriggerEnter(Collider c) => TryLatch(c);

    private void OnTriggerStay(Collider c) => TryLatch(c);

    private void TryLatch(Collider c)
    {
        RubZoneDetector match = FindPartner(c);
        if (match == null)
            return;

        if (partner != match)
        {
            partner = match;
            partnerLastPos = match.transform.position;
        }

        contactTimer = contactGrace;
    }

    private RubZoneDetector FindPartner(Collider c)
    {
        foreach (RubZoneDetector candidate in c.GetComponentsInParent<RubZoneDetector>())
        {
            if (candidate.hand != hand && candidate.zone == requiredOtherZone)
                return candidate;
        }
        return null;
    }

    private void Update()
    {
        if (contactTimer > 0f)
            contactTimer -= Time.deltaTime;

        IsRubbing = false;

        if (partner != null && contactTimer > 0f && Time.deltaTime > 0f)
        {
            Vector3 myDelta = transform.position - lastPos;
            Vector3 partnerDelta = partner.transform.position - partnerLastPos;
            float relativeSpeed = (myDelta - partnerDelta).magnitude / Time.deltaTime;
            IsRubbing = relativeSpeed >= minSlideSpeed;
        }

        lastPos = transform.position;
        if (partner != null)
            partnerLastPos = partner.transform.position;
    }
}
