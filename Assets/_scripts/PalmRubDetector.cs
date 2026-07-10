using UnityEngine;

// Put this on each palm object (L_Palm and R_Palm). Set 'hand' to match.
// Detects when the two palms' trigger colliders overlap AND are sliding relative
// to each other (rubbing), and feeds that as progress for the PalmToPalm WHO step.
//
// A kinematic Rigidbody is required so the palm collider raises its own trigger
// events (a child collider under the wrist's Rigidbody would report to the wrist
// instead). Kinematic so it follows the tracked/animated bone without physics.
[RequireComponent(typeof(Rigidbody))]
public class PalmRubDetector : MonoBehaviour
{
    public Handedness hand;

    [Tooltip("Minimum RELATIVE sliding speed between the two palms to count as rubbing (m/s).")]
    [SerializeField] private float minSlideSpeed = 0.05f;

    [SerializeField] private HandwashingManager.WashStep step =
        HandwashingManager.WashStep.PalmToPalm;

    [Tooltip("Keep contact 'alive' briefly after separation so tracking flicker doesn't reset it.")]
    [SerializeField] private float contactGrace = 0.2f;

    private PalmRubDetector other;   // the opposite palm, latched on first contact
    private float contactTimer;      // > 0 while considered in contact
    private Vector3 lastPos, otherLastPos;

    private void OnTriggerEnter(Collider c)
    {
        var p = c.GetComponentInParent<PalmRubDetector>();
        if (p != null && p.hand != hand)
        {
            other = p;
            contactTimer = contactGrace;
        }
    }

    private void OnTriggerStay(Collider c)
    {
        var p = c.GetComponentInParent<PalmRubDetector>();
        if (p != null && p.hand != hand)
        {
            other = p;
            contactTimer = contactGrace;   // refresh grace while overlapping
        }
    }

    private void Update()
    {
        if (contactTimer > 0f)
            contactTimer -= Time.deltaTime;

        var mgr = HandwashingManager.Instance;

        // Only the LEFT palm drives progress, so a single rub isn't counted twice.
        if (hand == Handedness.Left && other != null && contactTimer > 0f &&
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
