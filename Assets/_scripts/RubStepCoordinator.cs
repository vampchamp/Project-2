using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class RubStepCoordinator : MonoBehaviour
{
    private static RubStepCoordinator instance;
    private static readonly List<RubZoneDetector> Registered = new();

    private readonly List<RubZoneDetector> drivers = new();
    private readonly Dictionary<RubZoneDetector, float> sideSeconds = new();
    private WashStep trackedStep = WashStep.Complete;

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

    private void DrivePerSide(HandwashingManager manager, WashStep step, float duration)
    {
        float perSideTarget = duration / drivers.Count;
        float sum = 0f;
        float lowest = 1f;

        foreach (RubZoneDetector driver in drivers)
        {
            sideSeconds.TryGetValue(driver, out float seconds);

            if (driver.IsRubbing)
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
