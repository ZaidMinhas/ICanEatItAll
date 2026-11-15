using UnityEngine;

public class PlayerPickupRay : MonoBehaviour
{
    public HungerMeter hungerMeter;
    [SerializeField] private Camera viewCamera;
    private bool canPickup = true;
    private EdibleItem lastEdibleEntity;

    public float DEFAULT_RAY_DISTANCE = 4;
    public float MIN_RAY_DISTANCE = 2;

    void Awake()
    {
        if (hungerMeter == null) hungerMeter = GetComponentInParent<HungerMeter>();
        if (hungerMeter == null)
        {
            Debug.LogError("PlayerPickupRay requires a HungerMeter on its character.", this);
            enabled = false;
        }
    }

    void Update() => RefreshTarget();
    void OnDisable() => ClearTarget();

    // Also called when E is pressed, so input never consumes a stale target.
    public EdibleItem RefreshTarget()
    {
        EdibleItem target = null;
        Camera camera = viewCamera != null ? viewCamera : Camera.main;
        if (isActiveAndEnabled && canPickup && hungerMeter != null && camera != null && camera.isActiveAndEnabled)
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            float pickupScale = MIN_RAY_DISTANCE + (DEFAULT_RAY_DISTANCE - MIN_RAY_DISTANCE) * hungerMeter.Level;
            if (Physics.Raycast(ray, out RaycastHit hit, pickupScale))
            {
                var edible = hit.collider.GetComponentInParent<EdibleItem>();
                if (edible != null && edible.CanBeEatenBy(hungerMeter)) target = edible;
            }
        }
        SetTarget(target);
        return lastEdibleEntity;
    }

    void SetTarget(EdibleItem target)
    {
        if (lastEdibleEntity != target && lastEdibleEntity != null)
            lastEdibleEntity.ToggleOutline(false);
        lastEdibleEntity = target;
        if (lastEdibleEntity != null) lastEdibleEntity.ToggleOutline(true);
    }

    public void ClearTarget() => SetTarget(null);
    public EdibleItem GetLastEdibleEntity() => lastEdibleEntity;
    public bool GetCanInteract() => isActiveAndEnabled && lastEdibleEntity != null && lastEdibleEntity.CanBeEatenBy(hungerMeter);
    public GameObject GetlastEdibleGameObject() => lastEdibleEntity != null ? lastEdibleEntity.gameObject : null;
    public bool GetCanPickup() => canPickup;
}
