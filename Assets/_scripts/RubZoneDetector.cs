using System.Collections.Generic;
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

    [Header("Strictness")]
    [SerializeField] private bool requireBothSides = false;

    [SerializeField] private bool requireEachSide = false;

    [Header("Tuning")]
    [SerializeField] private float minSlideSpeed = 0.05f;

    [SerializeField] private float contactGrace = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private static readonly Dictionary<HandwashingManager.WashStep, HashSet<RubZoneDetector>> drivers = new();
    private static readonly Dictionary<HandwashingManager.WashStep, HashSet<RubZoneDetector>> rubbingNowSet = new();
    private static readonly Dictionary<HandwashingManager.WashStep, Dictionary<RubZoneDetector, float>> sideProgress = new();
    private static readonly Dictionary<HandwashingManager.WashStep, int> lastAccumFrame = new();

    private RubZoneDetector other;
    private float contactTimer;
    private float mySeconds;
    private Vector3 lastPos, otherLastPos;

    private bool IsValidPartner(RubZoneDetector p)
        => p != null && p.hand != hand && p.zone == requiredOtherZone;

    private void OnEnable()
    {
        HandwashingManager.RegisterColliderStep(step);
        if (drivesProgress)
            GetSet(drivers, step).Add(this);
    }

    private void OnDisable()
    {
        if (drivers.TryGetValue(step, out var d)) d.Remove(this);
        if (rubbingNowSet.TryGetValue(step, out var r)) r.Remove(this);
        if (sideProgress.TryGetValue(step, out var s)) s.Remove(this);
    }

    private void OnTriggerEnter(Collider c) => TryLatch(c);
    private void OnTriggerStay(Collider c) => TryLatch(c);

    private void TryLatch(Collider c)
    {
        var p = c.GetComponentInParent<RubZoneDetector>();

        if (debugLog && drivesProgress)
            Debug.Log($"[{name}] overlap '{c.name}' partner={(p != null ? p.name : "null")} valid={(p != null && IsValidPartner(p))}", this);

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

        bool rubbing = false;
        if (drivesProgress && other != null && contactTimer > 0f &&
            mgr != null && mgr.CurrentStep == step && Time.deltaTime > 0f)
        {
            Vector3 myDelta = transform.position - lastPos;
            Vector3 otherDelta = other.transform.position - otherLastPos;
            float relSpeed = (myDelta - otherDelta).magnitude / Time.deltaTime;
            rubbing = relSpeed >= minSlideSpeed;

            if (debugLog)
                Debug.Log($"[{name}] step={step} relSpeed={relSpeed:F3} min={minSlideSpeed} rubbing={rubbing}", this);
        }

        if (drivesProgress && mgr != null)
        {
            if (requireEachSide)
                DriveEachSide(mgr, rubbing);
            else if (requireBothSides)
                DriveBothSides(mgr, rubbing);
            else if (rubbing)
                mgr.AccumulateProgress(step, Time.deltaTime);
        }

        lastPos = transform.position;
        if (other != null) otherLastPos = other.transform.position;
    }

    private void DriveBothSides(HandwashingManager mgr, bool rubbing)
    {
        SetRubbing(step, this, rubbing);
        if (rubbing && AllDriversRubbing(step) && ClaimFrame(step))
            mgr.AccumulateProgress(step, Time.deltaTime);
    }

    private void DriveEachSide(HandwashingManager mgr, bool rubbing)
    {
        if (mgr.CurrentStep != step)
        {
            mySeconds = 0f;
            GetSideMap(step)[this] = 0f;
            return;
        }

        if (rubbing)
            mySeconds += Time.deltaTime;

        int count = GetSet(drivers, step).Count;
        float dur = mgr.GetStepDuration(step);
        float perSideTarget = count > 0 && dur > 0f ? dur / count : dur;
        GetSideMap(step)[this] = perSideTarget > 0f ? Mathf.Clamp01(mySeconds / perSideTarget) : 0f;

        if (!ClaimFrame(step))
            return;

        var map = GetSideMap(step);
        float sum = 0f, min = 1f;
        foreach (float v in map.Values)
        {
            sum += v;
            if (v < min) min = v;
        }
        mgr.SetProgress(step, map.Count > 0 ? sum / map.Count : 0f);

        if (count > 0 && map.Count >= count && min >= 1f)
            mgr.CompleteCurrentStep();
    }

    private static Dictionary<RubZoneDetector, float> GetSideMap(HandwashingManager.WashStep s)
    {
        if (!sideProgress.TryGetValue(s, out var map))
        {
            map = new Dictionary<RubZoneDetector, float>();
            sideProgress[s] = map;
        }
        return map;
    }

    private static HashSet<RubZoneDetector> GetSet(
        Dictionary<HandwashingManager.WashStep, HashSet<RubZoneDetector>> map,
        HandwashingManager.WashStep s)
    {
        if (!map.TryGetValue(s, out var set))
        {
            set = new HashSet<RubZoneDetector>();
            map[s] = set;
        }
        return set;
    }

    private static void SetRubbing(HandwashingManager.WashStep s, RubZoneDetector d, bool isRubbing)
    {
        var set = GetSet(rubbingNowSet, s);
        if (isRubbing) set.Add(d);
        else set.Remove(d);
    }

    private static bool AllDriversRubbing(HandwashingManager.WashStep s)
    {
        var d = GetSet(drivers, s);
        if (d.Count == 0) return false;
        return GetSet(rubbingNowSet, s).Count >= d.Count;
    }

    private static bool ClaimFrame(HandwashingManager.WashStep s)
    {
        int f = Time.frameCount;
        if (lastAccumFrame.TryGetValue(s, out int last) && last == f)
            return false;
        lastAccumFrame[s] = f;
        return true;
    }
}
