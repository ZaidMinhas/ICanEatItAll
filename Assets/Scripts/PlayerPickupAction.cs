using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupAction : MonoBehaviour
{   [SerializeField]
    private const float GROWTH_CONST = 0.25f;
    public GameObject pickupRayGameObject;
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
        if (pickupRay.GetCanInteract())
        {
            // Eat edible entity
            Destroy(pickupRay.GetlastEdibleGameObject());
            print("Destroy" + pickupRay.GetlastEdibleGameObject().name);

            GetComponentInParent<Character>().ChangeSize(pickupRay.GetLastEdibleEntity().nourishment * GROWTH_CONST);
            pickupRay.pickupDistanceModifier += pickupRay.GetLastEdibleEntity().nourishment * GROWTH_CONST;
        }
    }

    public PlayerPickupRay GetPlayerPickupRay()
    {
        return pickupRay;
    }
}
