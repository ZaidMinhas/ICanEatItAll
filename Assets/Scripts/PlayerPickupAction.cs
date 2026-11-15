using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupAction : MonoBehaviour
{
    [SerializeField]
    private const float GROWTH_CONST = 0.25f;
    public GameObject pickupRayGameObject;
    private PlayerPickupRay pickupRay;
    private Character character;

    public void Start()
    {
        if (pickupRayGameObject != null)
        {
            pickupRay = pickupRayGameObject.GetComponent<PlayerPickupRay>();
        }

        character = GetComponentInParent<Character>();
    }

    public void OnInteract(InputValue inputValue)
    {
        // print("Interact");
        // print(pickupRay.name + " can interact");
        if (pickupRay.GetCanInteract())
        {
            // print("Destroy" + pickupRay.GetlastEdibleGameObject().name);
            // Change size
            character.GetHungerMeter().Eat(pickupRay.GetLastEdibleEntity());
            // Eat edible entity
            Destroy(pickupRay.GetlastEdibleGameObject());
        }
    }

    public PlayerPickupRay GetPlayerPickupRay()
    {
        return pickupRay;
    }
}
