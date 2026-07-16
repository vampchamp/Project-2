using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ResetTarget : MonoBehaviour
{
    [SerializeField] private GameObject visual;
    [SerializeField] private float armDelay = 1f;

    private HandwashingManager manager;
    private Collider touchCollider;
    private float armedTime;

    private void Awake()
    {
        touchCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        manager = HandwashingManager.Instance;
        if (manager == null)
        {
            enabled = false;
            return;
        }

        manager.OnStepChanged += HandleStepChanged;
        HandleStepChanged(manager.CurrentStep);
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.OnStepChanged -= HandleStepChanged;
    }

    private void HandleStepChanged(WashStep step) => SetShown(step == WashStep.Complete);

    private void SetShown(bool shown)
    {
        if (visual != null)
            visual.SetActive(shown);

        touchCollider.enabled = shown;
        armedTime = Time.time + armDelay;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (manager == null || manager.CurrentStep != WashStep.Complete) return;
        if (Time.time < armedTime) return;
        if (!other.CompareTag("Hand")) return;

        manager.ResetSimulation();
    }
}
