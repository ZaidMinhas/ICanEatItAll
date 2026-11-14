using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupRay : MonoBehaviour
{
    public bool canPickup = true;
    public float maxPickupDistance = 2.0f;
    
    public bool canInteract = false;
    public GameObject lastEdibleGameObject = null;
    private EdibleItem lastEdibleEntity = null;


    void Update()
    {
        if (canPickup)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            
            if (Physics.Raycast(ray, out hit, maxPickupDistance)) {
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

    // public void OnInteract(InputAction.CallbackContext context)
    // {
    //     print("Interact");
    //     if (canInteract)
    //     {
    //         // Eat edible entity
    //         Destroy(lastEdibleGameObject);
    //         print("Destroy" + lastEdibleGameObject.name);
    //     }
    // }
}
