using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class PlayerHand : MonoBehaviour
{
    [SerializeField] private bool rightHand;
    private Animator animator;
    private InputAction grabAction;

    void Awake()
    {
        animator = GetComponent<Animator>();
        grabAction = new InputAction("Grab", InputActionType.Button,
            rightHand ? "<Mouse>/rightButton" : "<Mouse>/leftButton");
        grabAction.performed += OnGrab;
    }

    void OnEnable() => grabAction.Enable();
    void OnDisable()
    {
        grabAction.Disable();
        animator.ResetTrigger("_Grab");
    }

    void OnDestroy() => grabAction.Dispose();

    void OnGrab(InputAction.CallbackContext context)
    {
        if (animator.isActiveAndEnabled)
            animator.SetTrigger("_Grab");
    }
}
