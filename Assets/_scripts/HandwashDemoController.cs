using UnityEngine;

// Procedural ghost-hands demo. Subscribes to HandwashingManager.OnStepChanged
// and plays a looping demonstration of the current step using two translucent
// hand models. No animation clips needed - all motion is generated in code.
public class HandwashDemoController : MonoBehaviour
{
    [Header("Ghost hands")]
    [SerializeField] private Transform leftGhost;
    [SerializeField] private Transform rightGhost;

    [Header("Anchors")]
    [SerializeField] private Transform sinkAnchor;   // above the basin
    [SerializeField] private Transform soapAnchor;   // at the pump
    [SerializeField] private Transform towelAnchor;  // at the towel

    [Header("Calibration (tune once so palms face correctly)")]
    [SerializeField] private Vector3 leftBaseEuler = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 rightBaseEuler = new Vector3(0f, 0f, 0f);

    [Header("Motion")]
    [SerializeField] private float rubSpeed = 5f;        // oscillation speed
    [SerializeField] private float swapPeriod = 3f;      // seconds per side (bilateral steps)

    private HandwashingManager.WashStep step;
    private bool visible;

    private void Start()
    {
        var m = HandwashingManager.Instance;
        if (m != null)
        {
            m.OnStepChanged += OnStep;
            OnStep(m.CurrentStep);
        }
    }

    private void OnDestroy()
    {
        if (HandwashingManager.Instance != null)
            HandwashingManager.Instance.OnStepChanged -= OnStep;
    }

    private void OnStep(HandwashingManager.WashStep s)
    {
        step = s;
        visible = s != HandwashingManager.WashStep.Complete;
        if (leftGhost != null) leftGhost.gameObject.SetActive(visible);
        if (rightGhost != null) rightGhost.gameObject.SetActive(visible);
    }

    private void Update()
    {
        if (!visible || leftGhost == null || rightGhost == null) return;

        float t = Time.time;
        Transform anchor = sinkAnchor;
        // local-space poses (position + extra rotation on top of base)
        Vector3 lp = Vector3.zero, rp = Vector3.zero;
        Quaternion lr = Quaternion.identity, rr = Quaternion.identity;

        switch (step)
        {
            case HandwashingManager.WashStep.WetHands:
            case HandwashingManager.WashStep.Rinse:
            {
                // Palms up side by side, gently rolling under the water
                float roll = Mathf.Sin(t * rubSpeed * 0.6f) * 20f;
                lp = new Vector3(-0.07f, Mathf.Sin(t * 2f) * 0.015f, 0f);
                rp = new Vector3(0.07f, Mathf.Sin(t * 2f + 1f) * 0.015f, 0f);
                lr = Quaternion.Euler(0f, 0f, 180f + roll);
                rr = Quaternion.Euler(0f, 0f, 180f - roll);
                break;
            }
            case HandwashingManager.WashStep.ApplySoap:
            {
                anchor = soapAnchor;
                // Right palm up under the pump, bobbing to 'press'
                rp = new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 2.2f)) * -0.03f, 0f);
                rr = Quaternion.Euler(0f, 0f, 180f);
                lp = new Vector3(-0.18f, 0f, 0f);
                lr = Quaternion.Euler(0f, 0f, 180f);
                break;
            }
            case HandwashingManager.WashStep.PalmToPalm:
            {
                // Palms facing, sliding against each other
                float slide = Mathf.Sin(t * rubSpeed) * 0.045f;
                lp = new Vector3(-0.015f, 0f, slide);
                rp = new Vector3(0.015f, 0f, -slide);
                lr = Quaternion.Euler(0f, 0f, -90f);
                rr = Quaternion.Euler(0f, 0f, 90f);
                break;
            }
            case HandwashingManager.WashStep.BackOfHands:
            {
                // One palm scrubs the BACK of the other; sides swap
                float s01 = Blend(t);
                Vector2 orbit = Orbit(t, 0.03f);
                // A: left below palm-down, right on top scrubbing
                Vector3 lpA = Vector3.zero;                       Quaternion lrA = Quaternion.identity;
                Vector3 rpA = new Vector3(orbit.x, 0.05f, orbit.y); Quaternion rrA = Quaternion.identity;
                // B: mirrored
                Vector3 rpB = Vector3.zero;                       Quaternion rrB = Quaternion.identity;
                Vector3 lpB = new Vector3(orbit.x, 0.05f, orbit.y); Quaternion lrB = Quaternion.identity;
                lp = Vector3.Lerp(lpA, lpB, s01); rp = Vector3.Lerp(rpA, rpB, s01);
                lr = Quaternion.Slerp(lrA, lrB, s01); rr = Quaternion.Slerp(rrA, rrB, s01);
                break;
            }
            case HandwashingManager.WashStep.FingersInterlaced:
            {
                // Palms together, fingers crossed, rocking
                float rock = Mathf.Sin(t * rubSpeed) * 12f;
                lp = new Vector3(-0.01f, 0f, 0.02f);
                rp = new Vector3(0.01f, 0f, -0.02f);
                lr = Quaternion.Euler(rock, 25f, -90f);
                rr = Quaternion.Euler(-rock, -25f, 90f);
                break;
            }
            case HandwashingManager.WashStep.BacksOfFingers:
            {
                // Backs of fingers rubbing in the opposite palm
                float slide = Mathf.Sin(t * rubSpeed) * 0.03f;
                lp = new Vector3(0f, 0f, 0f);
                lr = Quaternion.Euler(0f, 0f, 180f); // palm up
                rp = new Vector3(0f, 0.045f, slide);
                rr = Quaternion.Euler(0f, 0f, 180f); // back of fingers down
                break;
            }
            case HandwashingManager.WashStep.Thumbs:
            {
                // One hand wraps and orbits the other's thumb; sides swap
                float s01 = Blend(t);
                Vector2 orbit = Orbit(t, 0.02f);
                Vector3 lpA = Vector3.zero; Quaternion lrA = Quaternion.identity;
                Vector3 rpA = new Vector3(-0.075f + orbit.x, 0.02f + orbit.y, 0f);
                Quaternion rrA = Quaternion.Euler(0f, 0f, 60f);
                Vector3 rpB = Vector3.zero; Quaternion rrB = Quaternion.identity;
                Vector3 lpB = new Vector3(0.075f + orbit.x, 0.02f + orbit.y, 0f);
                Quaternion lrB = Quaternion.Euler(0f, 0f, -60f);
                lp = Vector3.Lerp(lpA, lpB, s01); rp = Vector3.Lerp(rpA, rpB, s01);
                lr = Quaternion.Slerp(lrA, lrB, s01); rr = Quaternion.Slerp(rrA, rrB, s01);
                break;
            }
            case HandwashingManager.WashStep.Fingertips:
            {
                // Fingertips scrub circles in the opposite palm; sides swap
                float s01 = Blend(t);
                Vector2 orbit = Orbit(t, 0.018f);
                Vector3 lpA = Vector3.zero; Quaternion lrA = Quaternion.Euler(0f, 0f, 180f);
                Vector3 rpA = new Vector3(orbit.x, 0.06f, 0.03f + orbit.y);
                Quaternion rrA = Quaternion.Euler(-50f, 0f, 0f);
                Vector3 rpB = Vector3.zero; Quaternion rrB = Quaternion.Euler(0f, 0f, 180f);
                Vector3 lpB = new Vector3(orbit.x, 0.06f, 0.03f + orbit.y);
                Quaternion lrB = Quaternion.Euler(-50f, 0f, 0f);
                lp = Vector3.Lerp(lpA, lpB, s01); rp = Vector3.Lerp(rpA, rpB, s01);
                lr = Quaternion.Slerp(lrA, lrB, s01); rr = Quaternion.Slerp(rrA, rrB, s01);
                break;
            }
            case HandwashingManager.WashStep.Dry:
            {
                anchor = towelAnchor;
                float slide = Mathf.Sin(t * rubSpeed * 0.8f) * 0.03f;
                lp = new Vector3(-0.02f, 0f, slide);
                rp = new Vector3(0.02f, 0f, -slide);
                lr = Quaternion.Euler(0f, 0f, -80f);
                rr = Quaternion.Euler(0f, 0f, 80f);
                break;
            }
        }

        if (anchor == null) anchor = sinkAnchor;
        if (anchor == null) return;

        leftGhost.position = anchor.TransformPoint(lp);
        rightGhost.position = anchor.TransformPoint(rp);
        leftGhost.rotation = anchor.rotation * lr * Quaternion.Euler(leftBaseEuler);
        rightGhost.rotation = anchor.rotation * rr * Quaternion.Euler(rightBaseEuler);
    }

    // Smooth 0..1..0 blend used to swap which hand is being scrubbed
    private float Blend(float t)
    {
        float p = Mathf.PingPong(t / swapPeriod, 1f);
        return Mathf.SmoothStep(0f, 1f, p);
    }

    private Vector2 Orbit(float t, float r)
    {
        return new Vector2(Mathf.Cos(t * rubSpeed) * r, Mathf.Sin(t * rubSpeed) * r);
    }
}
