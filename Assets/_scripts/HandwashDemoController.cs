using UnityEngine;

public class HandwashDemoController : MonoBehaviour
{
    [SerializeField] private recorder recorder;

    private void Start()
    {
        var manager = HandwashingManager.Instance;

        if (manager != null)
        {
            manager.OnStepChanged += OnStep;
            OnStep(manager.CurrentStep);
        }
    }

    private void OnDestroy()
    {
        if (HandwashingManager.Instance != null)
            HandwashingManager.Instance.OnStepChanged -= OnStep;
    }

    private void OnStep(HandwashingManager.WashStep step)
    {
        if (step == HandwashingManager.WashStep.Complete)
        {
            recorder.StopPlayback();
            return;
        }

        recorder.StartPlayback(step + ".demo");
    }
    
}