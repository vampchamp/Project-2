using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimatorController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty gripAction;
    public InputActionProperty triggerAction;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        float gripValue =
            gripAction.action.ReadValue<float>();

        float triggerValue =
            triggerAction.action.ReadValue<float>();

        animator.SetFloat("Grip", gripValue);
        animator.SetFloat("Trigger", triggerValue);
    }
}