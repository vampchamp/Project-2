using UnityEngine;

// Ghost-hands demo. Assigned clips are played first so custom authored
// animations can drive the hands. Missing clips fall back to procedural poses.
public class HandwashDemoController : MonoBehaviour
{
    [System.Serializable]
    private class StepAnimation
    {
        public HandwashingManager.WashStep step;
        public AnimationClip clip;
    }

    [Header("Ghost hands")]
    [SerializeField] private Transform leftGhost;
    [SerializeField] private Transform rightGhost;
    [SerializeField] private Animator ghostAnimator;

    [Header("Animation Clips")]
    [SerializeField] private bool preferAnimationClips = true;
    [SerializeField] private bool proceduralFallbackWhenMissing = true;
    [SerializeField] private float animationPlaybackSpeed = 1f;
    [SerializeField] private StepAnimation[] stepAnimations =
    {
        new StepAnimation { step = HandwashingManager.WashStep.WetHands },
        new StepAnimation { step = HandwashingManager.WashStep.ApplySoap },
        new StepAnimation { step = HandwashingManager.WashStep.PalmToPalm },
        new StepAnimation { step = HandwashingManager.WashStep.BackOfHands },
        new StepAnimation { step = HandwashingManager.WashStep.FingersInterlaced },
        new StepAnimation { step = HandwashingManager.WashStep.BacksOfFingers },
        new StepAnimation { step = HandwashingManager.WashStep.Thumbs },
        new StepAnimation { step = HandwashingManager.WashStep.Fingertips },
        new StepAnimation { step = HandwashingManager.WashStep.Rinse },
        new StepAnimation { step = HandwashingManager.WashStep.Dry },
    };

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
    private bool usingClip;
    private AnimationClip activeClip;
    private float clipTime;

    private void Start()
    {
        EnsureAnimator();
        DisableRecordingControllerAtRuntime();

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

        StopClip();
    }

    private void OnDisable()
    {
        StopClip();
    }

    private void OnStep(HandwashingManager.WashStep s)
    {
        step = s;
        visible = s != HandwashingManager.WashStep.Complete;
        if (leftGhost != null) leftGhost.gameObject.SetActive(visible);
        if (rightGhost != null) rightGhost.gameObject.SetActive(visible);

        StopClip();
        activeClip = visible && preferAnimationClips ? FindClip(s) : null;
        usingClip = activeClip != null;

        if (usingClip)
        {
            clipTime = 0f;
            SampleActiveClip();
        }
        else if (!proceduralFallbackWhenMissing)
        {
            if (leftGhost != null) leftGhost.gameObject.SetActive(false);
            if (rightGhost != null) rightGhost.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!visible || leftGhost == null || rightGhost == null) return;
        if (usingClip)
        {
            AdvanceActiveClip();
            return;
        }
        if (!proceduralFallbackWhenMissing) return;

        float t = Time.time;
        Transform anchor = sinkAnchor;
        float scrub = Mathf.Sin(t * rubSpeed);
        float scrub01 = (scrub + 1f) * 0.5f;
        // local-space poses (position + extra rotation on top of base)
        Vector3 lp = Vector3.zero, rp = Vector3.zero;
        Quaternion lr = Quaternion.identity, rr = Quaternion.identity;

        switch (step)
        {
            case HandwashingManager.WashStep.WetHands:
            case HandwashingManager.WashStep.Rinse:
            {
                // Steps 0 and 8: palms together under the running water.
                float roll = Mathf.Sin(t * rubSpeed * 0.6f) * 16f;
                lp = new Vector3(-0.055f, Mathf.Sin(t * 2f) * 0.012f, 0f);
                rp = new Vector3(0.055f, Mathf.Sin(t * 2f + 1f) * 0.012f, 0f);
                lr = Quaternion.Euler(0f, 0f, 165f + roll);
                rr = Quaternion.Euler(0f, 0f, -165f - roll);
                break;
            }
            case HandwashingManager.WashStep.ApplySoap:
            {
                anchor = soapAnchor;
                // Step 1: left hand presses the soap head, right palm waits under the nozzle.
                float press = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(t * 1.4f, 1f));
                lp = new Vector3(0.0f, -0.17f - press * 0.035f, -0.018f);
                rp = new Vector3(0.0f, -0.315f, -0.065f);
                lr = Quaternion.Euler(90f, 0f, 0f);
                rr = Quaternion.Euler(0f, 0f, 180f);
                break;
            }
            case HandwashingManager.WashStep.PalmToPalm:
            {
                // Step 2: palm to palm, broad sliding strokes.
                float slide = scrub * 0.045f;
                lp = new Vector3(-0.015f, 0f, slide);
                rp = new Vector3(0.015f, 0f, -slide);
                lr = Quaternion.Euler(0f, -18f, -90f);
                rr = Quaternion.Euler(0f, 18f, 90f);
                break;
            }
            case HandwashingManager.WashStep.BackOfHands:
            {
                // Step 3: right palm over left dorsum, then left palm over right dorsum.
                float s01 = Blend(t);
                Vector2 orbit = Orbit(t, 0.03f);
                Vector3 lpA = new Vector3(-0.02f, -0.01f, 0f);
                Quaternion lrA = Quaternion.Euler(0f, 0f, 0f);
                Vector3 rpA = new Vector3(-0.02f + orbit.x, 0.055f, orbit.y);
                Quaternion rrA = Quaternion.Euler(0f, 0f, 180f);
                Vector3 rpB = new Vector3(0.02f, -0.01f, 0f);
                Quaternion rrB = Quaternion.Euler(0f, 0f, 0f);
                Vector3 lpB = new Vector3(0.02f + orbit.x, 0.055f, orbit.y);
                Quaternion lrB = Quaternion.Euler(0f, 0f, 180f);
                lp = Vector3.Lerp(lpA, lpB, s01); rp = Vector3.Lerp(rpA, rpB, s01);
                lr = Quaternion.Slerp(lrA, lrB, s01); rr = Quaternion.Slerp(rrA, rrB, s01);
                break;
            }
            case HandwashingManager.WashStep.FingersInterlaced:
            {
                // Step 4: palm to palm with fingers interlaced.
                float rock = scrub * 14f;
                lp = new Vector3(-0.01f, 0f, 0.02f);
                rp = new Vector3(0.01f, 0f, -0.02f);
                lr = Quaternion.Euler(rock, 35f, -90f);
                rr = Quaternion.Euler(-rock, -35f, 90f);
                break;
            }
            case HandwashingManager.WashStep.BacksOfFingers:
            {
                // Step 5: backs of fingers to opposing palms.
                float slide = scrub * 0.035f;
                lp = new Vector3(-0.015f, 0f, 0f);
                rp = new Vector3(0.015f, 0.05f, slide);
                lr = Quaternion.Euler(0f, 0f, 180f);
                rr = Quaternion.Euler(0f, 0f, 0f);
                break;
            }
            case HandwashingManager.WashStep.Thumbs:
            {
                // Step 6: rotational rubbing of each thumb.
                float s01 = Blend(t);
                Vector2 orbit = Orbit(t, 0.02f);
                Vector3 lpA = new Vector3(-0.045f, 0f, 0f); Quaternion lrA = Quaternion.identity;
                Vector3 rpA = new Vector3(-0.075f + orbit.x, 0.02f + orbit.y, 0f);
                Quaternion rrA = Quaternion.Euler(0f, 0f, 60f);
                Vector3 rpB = new Vector3(0.045f, 0f, 0f); Quaternion rrB = Quaternion.identity;
                Vector3 lpB = new Vector3(0.075f + orbit.x, 0.02f + orbit.y, 0f);
                Quaternion lrB = Quaternion.Euler(0f, 0f, -60f);
                lp = Vector3.Lerp(lpA, lpB, s01); rp = Vector3.Lerp(rpA, rpB, s01);
                lr = Quaternion.Slerp(lrA, lrB, s01); rr = Quaternion.Slerp(rrA, rrB, s01);
                break;
            }
            case HandwashingManager.WashStep.Fingertips:
            {
                // Step 7: fingertips rotate in the opposite palm.
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
                // Steps 9 and 10: dry hands with towel, then use towel to turn off faucet.
                if (scrub01 < 0.55f)
                {
                    float slide = scrub * 0.03f;
                    lp = new Vector3(-0.025f, 0f, slide);
                    rp = new Vector3(0.025f, 0f, -slide);
                    lr = Quaternion.Euler(0f, 0f, -80f);
                    rr = Quaternion.Euler(0f, 0f, 80f);
                }
                else
                {
                    lp = new Vector3(-0.02f, 0.025f, 0.015f);
                    rp = new Vector3(0.02f, 0.025f, -0.015f);
                    lr = Quaternion.Euler(0f, 0f, -35f + scrub * 18f);
                    rr = Quaternion.Euler(0f, 0f, 35f - scrub * 18f);
                }
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

    private void EnsureAnimator()
    {
        if (ghostAnimator == null)
            ghostAnimator = GetComponent<Animator>();

        if (ghostAnimator == null)
            ghostAnimator = gameObject.AddComponent<Animator>();

        ghostAnimator.applyRootMotion = false;
        ghostAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void DisableRecordingControllerAtRuntime()
    {
        if (ghostAnimator == null)
            return;

        ghostAnimator.runtimeAnimatorController = null;
    }

    private AnimationClip FindClip(HandwashingManager.WashStep washStep)
    {
        if (stepAnimations == null)
            return null;

        foreach (StepAnimation stepAnimation in stepAnimations)
        {
            if (stepAnimation != null && stepAnimation.step == washStep && IsUsableClip(stepAnimation.clip))
                return stepAnimation.clip;
        }

        return null;
    }

    private bool IsUsableClip(AnimationClip clip)
    {
        return clip != null && !clip.empty && clip.length > 0f;
    }

    private void AdvanceActiveClip()
    {
        if (activeClip == null || activeClip.length <= 0f)
            return;

        clipTime += Time.deltaTime * animationPlaybackSpeed;
        if (animationPlaybackSpeed >= 0f)
            clipTime = Mathf.Repeat(clipTime, activeClip.length);
        else
            clipTime = activeClip.length - Mathf.Repeat(-clipTime, activeClip.length);

        SampleActiveClip();
    }

    private void SampleActiveClip()
    {
        if (activeClip == null)
            return;

        activeClip.SampleAnimation(gameObject, clipTime);
    }

    private void StopClip()
    {
        usingClip = false;
        activeClip = null;
        clipTime = 0f;
    }
}
