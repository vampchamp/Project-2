using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class RubStepCoordinator : MonoBehaviour
{
    private static RubStepCoordinator instance;
    private static readonly List<RubZoneDetector> Registered = new();

    [Tooltip("On per-side steps only one hand may earn progress at a time. This is how long the active side stays latched after it stops rubbing, before the other side can take over.")]
    [SerializeField] private float sideSwitchGrace = 0.4f;

    [SerializeField] private bool debugLog;

    private readonly List<RubZoneDetector> drivers = new();
    private readonly Dictionary<RubZoneDetector, float> sideSeconds = new();
    private WashStep trackedStep = WashStep.Complete;

    private RubZoneDetector activeDriver;
    private float activeHold;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Registered.Clear();
        instance = null;
    }

    public static void Register(RubZoneDetector detector)
    {
        if (!Registered.Contains(detector))
            Registered.Add(detector);

        EnsureInstance();
    }

    public static void Unregister(RubZoneDetector detector)
    {
        Registered.Remove(detector);

        if (instance != null)
            instance.sideSeconds.Remove(detector);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        instance = FindAnyObjectByType<RubStepCoordinator>();

        if (instance == null)
            instance = new GameObject(nameof(RubStepCoordinator)).AddComponent<RubStepCoordinator>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void LateUpdate()
    {
        HandwashingManager manager = HandwashingManager.Instance;
        if (manager == null)
            return;

        WashStep step = manager.CurrentStep;

        if (trackedStep != step)
        {
            trackedStep = step;
            sideSeconds.Clear();
            activeDriver = null;
            activeHold = 0f;
        }

        RubMode mode = WashStepCatalog.GetRubMode(step);
        if (mode == RubMode.None)
            return;

        float duration = WashStepCatalog.GetDuration(step);
        if (duration <= 0f)
            return;

        CollectDrivers(step);
        if (drivers.Count == 0)
            return;

        if (mode == RubMode.Shared)
            DriveShared(manager, step);
        else
            DrivePerSide(manager, step, duration);
    }

    private void CollectDrivers(WashStep step)
    {
        drivers.Clear();

        foreach (RubZoneDetector detector in Registered)
        {
            if (detector != null && detector.isActiveAndEnabled && detector.DrivesProgress && detector.Step == step)
                drivers.Add(detector);
        }
    }

    private void DriveShared(HandwashingManager manager, WashStep step)
    {
        foreach (RubZoneDetector driver in drivers)
        {
            if (!driver.IsRubbing)
                return;
        }

        manager.AccumulateProgress(step, Time.deltaTime);

        float progress = manager.CurrentStepProgress;
        manager.SetHandProgress(step, Handedness.Left, progress);
        manager.SetHandProgress(step, Handedness.Right, progress);
    }

    private RubZoneDetector ResolveActiveDriver()
    {
        RubZoneDetector closest = null;
        float closestDistance = float.PositiveInfinity;

        foreach (RubZoneDetector driver in drivers)
        {
            if (driver.IsRubbing && driver.ContactDistance < closestDistance)
            {
                closestDistance = driver.ContactDistance;
                closest = driver;
            }
        }

        if (closest == null)
        {
            activeHold -= Time.deltaTime;
            if (activeHold <= 0f)
                activeDriver = null;
        }
        else if (activeDriver == null || activeHold <= 0f || closest == activeDriver)
        {
            activeDriver = closest;
            activeHold = sideSwitchGrace;
        }
        else
        {
            activeHold -= Time.deltaTime;
        }

        if (debugLog)
        {
            foreach (RubZoneDetector driver in drivers)
                Debug.Log($"[Rub] {trackedStep} {driver.Hand} rubbing={driver.IsRubbing} dist={driver.ContactDistance:F3} active={(driver == activeDriver)}");
        }

        return activeDriver;
    }

    private void DrivePerSide(HandwashingManager manager, WashStep step, float duration)
    {
        float perSideTarget = duration / drivers.Count;
        float sum = 0f;
        float lowest = 1f;

        RubZoneDetector active = ResolveActiveDriver();

        foreach (RubZoneDetector driver in drivers)
        {
            sideSeconds.TryGetValue(driver, out float seconds);

            if (driver.IsRubbing && driver == active)
            {
                seconds = Mathf.Min(seconds + Time.deltaTime, perSideTarget);
                sideSeconds[driver] = seconds;
            }

            float normalized = perSideTarget > 0f ? Mathf.Clamp01(seconds / perSideTarget) : 0f;
            manager.SetHandProgress(step, driver.Hand, normalized);

            sum += normalized;
            if (normalized < lowest)
                lowest = normalized;
        }

        if (lowest >= 1f)
        {
            manager.SetProgress(step, 1f);
            return;
        }

        manager.SetProgress(step, sum / drivers.Count);
    }
}
