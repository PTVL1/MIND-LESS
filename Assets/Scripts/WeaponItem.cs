using UnityEngine;

public class WeaponItem : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private string weaponName = "Test Weapon";
    [SerializeField] private Sprite icon;
    [SerializeField] private Transform grabPoint;

    [Header("Pickup Preview")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Material blackOutlineMaterial;
    [SerializeField] private Material greenOutlineMaterial;
    [SerializeField] private Light shinyLight;

    private Material[][] baseMaterialsByRenderer;

    public string WeaponName => weaponName;
    public Sprite Icon => icon;
    public Transform GrabPoint => grabPoint;

    public bool TryGetAmmoWeapon(out IAmmoWeapon ammoWeapon)
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IAmmoWeapon foundAmmoWeapon)
            {
                ammoWeapon = foundAmmoWeapon;
                return true;
            }
        }

        ammoWeapon = null;
        return false;
    }

    private void Awake()
    {
        CacheBaseMaterials();
    }

    public void SetWorldState(bool inWorld)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = inWorld;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !inWorld;
            rb.useGravity = inWorld;
        }

        if (!inWorld)
        {
            SetPickupPreview(false, false);
        }
    }

    public void SetPickupPreview(bool isSelected, bool isNearby)
    {
        if (!isNearby)
        {
            ApplyOutlineMaterial(null);
            SetShinyLight(false);
            return;
        }

        Material targetOutline = isSelected ? greenOutlineMaterial : blackOutlineMaterial;
        ApplyOutlineMaterial(targetOutline);
        SetShinyLight(isSelected);
    }

    private void CacheBaseMaterials()
    {
        if (highlightRenderers == null)
        {
            return;
        }

        baseMaterialsByRenderer = new Material[highlightRenderers.Length][];

        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            Renderer rendererRef = highlightRenderers[i];
            if (rendererRef == null)
            {
                continue;
            }

            Material[] mats = rendererRef.sharedMaterials;
            Material[] copy = new Material[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                copy[j] = mats[j];
            }

            baseMaterialsByRenderer[i] = copy;
        }
    }

    private void ApplyOutlineMaterial(Material outlineMaterial)
    {
        if (highlightRenderers == null)
        {
            return;
        }

        if (baseMaterialsByRenderer == null || baseMaterialsByRenderer.Length != highlightRenderers.Length)
        {
            CacheBaseMaterials();
        }

        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            Renderer rendererRef = highlightRenderers[i];
            if (rendererRef == null)
            {
                continue;
            }

            Material[] baseMats = baseMaterialsByRenderer[i];
            if (baseMats == null)
            {
                continue;
            }

            if (outlineMaterial == null)
            {
                rendererRef.sharedMaterials = baseMats;
                continue;
            }

            Material[] combined = BuildOutlinedMaterials(baseMats, outlineMaterial);
            rendererRef.sharedMaterials = combined;
        }
    }

    private Material[] BuildOutlinedMaterials(Material[] baseMats, Material outlineMaterial)
    {
        int targetSize = Mathf.Max(2, baseMats.Length);
        Material[] result = new Material[targetSize];

        for (int i = 0; i < targetSize; i++)
        {
            if (i < baseMats.Length)
            {
                result[i] = baseMats[i];
            }
        }

        result[1] = outlineMaterial;
        return result;
    }

    private void SetShinyLight(bool state)
    {
        if (shinyLight == null)
        {
            return;
        }

        shinyLight.enabled = state;
    }
}
