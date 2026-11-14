using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupAction : MonoBehaviour
{   [SerializeField]
    private GameObject pickupRayGameObject;
    private PlayerPickupRay pickupRay;

    public void Start()
    {
        if (pickupRayGameObject != null)
        {
            pickupRay = pickupRayGameObject.GetComponent<PlayerPickupRay>();
        }
    }

    public void OnInteract(InputValue inputValue)
    {
        print("Interact");
        print(pickupRay.name + " can interact");
        if (pickupRay.canInteract)
        {
            // Eat edible entity
            Destroy(pickupRay.lastEdibleGameObject);
            print("Destroy" + pickupRay.lastEdibleGameObject.name);
        }
    }

    public PlayerPickupRay GetPlayerPickupRay()
    {
        return pickupRay;
    }
}
