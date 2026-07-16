using UnityEngine;

public class HandwashDemoController : MonoBehaviour
{
    [SerializeField] private HandDemoRecorder recorder;

    private HandwashingManager manager;

    private void Start()
    {
        manager = HandwashingManager.Instance;
        if (manager == null)
            return;

        manager.OnStepChanged += OnStep;
        OnStep(manager.CurrentStep);
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.OnStepChanged -= OnStep;
    }

    private void OnStep(WashStep step)
    {
        if (recorder == null)
            return;

        if (step == WashStep.Complete)
        {
            recorder.StopPlayback();
            return;
        }

        recorder.StartPlayback(step + ".demo");
    }
}
