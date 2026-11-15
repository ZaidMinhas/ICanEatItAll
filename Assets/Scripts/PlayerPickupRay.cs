using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupRay : MonoBehaviour
{   
    [SerializeField]
    public float pickupDistanceModifier = 1.0f;
    private const float DISTANCE_CONST = 2.0f;
    private bool canPickup = true;
    private bool canInteract = false;
    private GameObject lastEdibleGameObject = null;
    private EdibleItem lastEdibleEntity = null;


    void Update()
    {
        if (canPickup)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            
            if (Physics.Raycast(ray, out hit, pickupDistanceModifier * DISTANCE_CONST)) {
                EdibleItem edibleEntity = hit.collider.gameObject.GetComponent<EdibleItem>();
                // Edible entity sighted
                if (edibleEntity != null)
                {
                    // Debug.Log("Hit " + edibleEntity.name + " at distance: " + hit.distance);
                    if (edibleEntity != lastEdibleEntity || lastEdibleEntity.myOutline.OutlineMode == Outline.Mode.OutlineHidden)
                    {
                        lastEdibleEntity = edibleEntity;
                        lastEdibleGameObject = hit.collider.gameObject;
                        edibleEntity.ToggleOutline(true);

                        canInteract = true;
                    }
                }
                // No edible entity
                // else if (lastEdibleEntity.myOutline.OutlineMode == Outline.Mode.OutlineVisible)
                // {
                //     lastEdibleEntity.ToggleOutline(false);
                //     canInteract = false;
                // }
            }
            // No edible entity
            else if (lastEdibleEntity != null)
            {
                if (lastEdibleEntity.myOutline.OutlineMode == Outline.Mode.OutlineVisible)
                {
                    lastEdibleEntity.ToggleOutline(false);
                    canInteract = false;
                }
            }
        }
    }

    public EdibleItem GetLastEdibleEntity()
    {
        return lastEdibleEntity;
    }

    public bool GetCanInteract()
    {
        return canInteract;
    }

    public GameObject GetlastEdibleGameObject()
    {
        return lastEdibleGameObject;
    }

    public bool GetCanPickup()
    {
        return canPickup;
    }
}
