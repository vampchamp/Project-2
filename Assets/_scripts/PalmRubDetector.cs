using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PalmRubDetector : MonoBehaviour
{
    public Handedness hand;

    [SerializeField] private float minSlideSpeed = 0.05f;

    [SerializeField] private HandwashingManager.WashStep step =
        HandwashingManager.WashStep.PalmToPalm;

    [SerializeField] private float contactGrace = 0.2f;

    private PalmRubDetector other;
    private float contactTimer;
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
            contactTimer = contactGrace;
        }
    }

    private void Update()
    {
        if (contactTimer > 0f)
            contactTimer -= Time.deltaTime;

        var mgr = HandwashingManager.Instance;

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
