using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupAction : MonoBehaviour
{
    public GameObject pickupRayGameObject;
    private PlayerPickupRay pickupRay;
    private Character character;

    void Awake()
    {
        if (pickupRayGameObject != null)
            pickupRay = pickupRayGameObject.GetComponent<PlayerPickupRay>();
        character = GetComponentInParent<Character>();
        if (pickupRay == null || character == null)
        {
            Debug.LogError("PlayerPickupAction requires a pickup ray and a parent Character.", this);
            enabled = false;
        }
    }

    public void OnInteract(InputValue inputValue)
    {
        if (!isActiveAndEnabled || inputValue == null || !inputValue.isPressed || pickupRay == null || character == null) return;
        EdibleItem item = pickupRay.RefreshTarget();
        HungerMeter meter = character.GetHungerMeter();
        if (item == null || !item.CanBeEatenBy(meter) || !meter.TryEat(item)) return;

        pickupRay.ClearTarget();
        GameSounds.Play("Eat");
        Destroy(item.gameObject);
    }

    public PlayerPickupRay GetPlayerPickupRay() => pickupRay;
}
