using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingSystem : MonoBehaviour
{
    public static bool IsBuildingModeActive { get; private set; }

    private enum StructureType
    {
        None,
        Campfire,
        Bed,
        WoodWall
    }

    [Header("References")]
    [SerializeField] private PlayerCurrencyWallet wallet;
    [SerializeField] private Camera placementCamera;
    [SerializeField] private GameObject buildingMenuRoot;
    [SerializeField] private Text progressText;

    [Header("Structure Prefabs")]
    [SerializeField] private GameObject campfirePrefab;
    [SerializeField] private GameObject bedPrefab;
    [SerializeField] private GameObject woodWallPrefab;

    [Header("Optional Preview Prefabs")]
    [SerializeField] private GameObject campfirePreviewPrefab;
    [SerializeField] private GameObject bedPreviewPrefab;
    [SerializeField] private GameObject woodWallPreviewPrefab;

    [Header("Placement")]
    [SerializeField] private KeyCode buildingModeKey = KeyCode.B;
    [SerializeField] private float gridCellSize = 1f;
    [SerializeField] private float maximumPlacementDistance = 50f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask placementBlockMask;
    [SerializeField] private float blockerCheckHeight = 1f;

    [Header("Preview")]
    [SerializeField] private Color validPreviewColor = new Color(0.1f, 1f, 0.2f, 0.45f);
    [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.1f, 0.1f, 0.45f);
    [SerializeField] private Texture2D buildingCursor;
    [SerializeField] private Vector2 cursorHotspot;

    [Header("Construction")]
    [SerializeField] private float buildingTime = 2.5f;

    private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private StructureType selectedStructure;
    private GameObject previewObject;
    private Renderer[] previewRenderers;
    private Material validPreviewMaterial;
    private Material invalidPreviewMaterial;
    private Vector2Int previewGridOrigin;
    private Vector3 previewWorldPosition;
    private bool previewIsValid;
    private bool isBuildingMode;
    private bool isConstructing;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    private void Awake()
    {
        if (wallet == null)
        {
            wallet = GetComponent<PlayerCurrencyWallet>();
        }

        if (placementCamera == null)
        {
            placementCamera = Camera.main;
        }

        validPreviewMaterial = CreatePreviewMaterial(validPreviewColor);
        invalidPreviewMaterial = CreatePreviewMaterial(invalidPreviewColor);
        isBuildingMode = false;
        IsBuildingModeActive = false;
        if (buildingMenuRoot != null)
        {
            buildingMenuRoot.SetActive(false);
        }

        if (progressText != null)
        {
            progressText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(buildingModeKey))
        {
            SetBuildingMode(!isBuildingMode);
        }

        if (!isBuildingMode || selectedStructure == StructureType.None || isConstructing)
        {
            return;
        }

        UpdatePreview();

        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) && previewIsValid && !pointerOverUi)
        {
            TryPlaceSelectedStructure();
        }

        if (Input.GetMouseButtonDown(1))
        {
            ClearSelection();
        }
    }

    public void SelectCampfire()
    {
        SelectStructure(StructureType.Campfire);
    }

    public void SelectBed()
    {
        SelectStructure(StructureType.Bed);
    }

    public void SelectWoodWall()
    {
        SelectStructure(StructureType.WoodWall);
    }

    public void ExitBuildingMode()
    {
        SetBuildingMode(false);
    }

    private void SetBuildingMode(bool enabled)
    {
        if (isBuildingMode == enabled)
        {
            return;
        }

        if (enabled)
        {
            previousCursorVisible = Cursor.visible;
            previousCursorLockState = Cursor.lockState;
        }

        isBuildingMode = enabled;
        IsBuildingModeActive = enabled;

        if (buildingMenuRoot != null)
        {
            buildingMenuRoot.SetActive(enabled);
        }

        Cursor.visible = enabled || previousCursorVisible;
        Cursor.lockState = enabled ? CursorLockMode.None : previousCursorLockState;
        Cursor.SetCursor(enabled ? buildingCursor : null, enabled ? cursorHotspot : Vector2.zero, CursorMode.Auto);

        if (!enabled)
        {
            ClearSelection();
        }
    }

    private void SelectStructure(StructureType structureType)
    {
        selectedStructure = structureType;
        DestroyPreview();

        GameObject prefab = GetSelectedPreviewPrefab();
        if (prefab == null)
        {
            selectedStructure = StructureType.None;
            return;
        }

        previewObject = Instantiate(prefab);
        previewObject.name = prefab.name + " Preview";

        foreach (Collider previewCollider in previewObject.GetComponentsInChildren<Collider>(true))
        {
            previewCollider.enabled = false;
        }

        foreach (MonoBehaviour behaviour in previewObject.GetComponentsInChildren<MonoBehaviour>(true))
        {
            behaviour.enabled = false;
        }

        previewRenderers = previewObject.GetComponentsInChildren<Renderer>(true);
        previewObject.SetActive(false);
    }

    private void UpdatePreview()
    {
        if (placementCamera == null)
        {
            placementCamera = Camera.main;
        }

        if (placementCamera == null || previewObject == null)
        {
            return;
        }

        Ray ray = placementCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maximumPlacementDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            previewObject.SetActive(false);
            previewIsValid = false;
            return;
        }

        Vector2Int footprint = GetFootprint();
        float safeGridSize = Mathf.Max(0.1f, gridCellSize);
        previewGridOrigin = new Vector2Int(
            Mathf.RoundToInt((hit.point.x / safeGridSize) - ((footprint.x - 1) * 0.5f)),
            Mathf.RoundToInt((hit.point.z / safeGridSize) - ((footprint.y - 1) * 0.5f)));

        previewWorldPosition = new Vector3(
            (previewGridOrigin.x + ((footprint.x - 1) * 0.5f)) * safeGridSize,
            hit.point.y,
            (previewGridOrigin.y + ((footprint.y - 1) * 0.5f)) * safeGridSize);

        previewObject.SetActive(true);
        previewObject.transform.SetPositionAndRotation(previewWorldPosition, Quaternion.identity);
        previewIsValid = HasResources() && AreCellsAvailable(previewGridOrigin, footprint) &&
            !HasPhysicalBlocker(previewWorldPosition, footprint, safeGridSize);
        ApplyPreviewMaterial(previewIsValid ? validPreviewMaterial : invalidPreviewMaterial);
    }

    private bool HasPhysicalBlocker(Vector3 center, Vector2Int footprint, float cellSize)
    {
        Vector3 halfExtents = new Vector3(
            footprint.x * cellSize * 0.48f,
            Mathf.Max(0.05f, blockerCheckHeight * 0.5f),
            footprint.y * cellSize * 0.48f);
        Vector3 checkCenter = center + Vector3.up * halfExtents.y;

        return Physics.CheckBox(
            checkCenter,
            halfExtents,
            Quaternion.identity,
            placementBlockMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool AreCellsAvailable(Vector2Int origin, Vector2Int footprint)
    {
        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                if (occupiedCells.Contains(origin + new Vector2Int(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void TryPlaceSelectedStructure()
    {
        if (!SpendResources())
        {
            return;
        }

        GameObject prefab = GetSelectedPrefab();
        Vector2Int footprint = GetFootprint();
        MarkCellsOccupied(previewGridOrigin, footprint);
        GameObject structure = Instantiate(prefab, previewWorldPosition, Quaternion.identity);
        StartCoroutine(BuildStructure(structure));
    }

    private IEnumerator BuildStructure(GameObject structure)
    {
        isConstructing = true;
        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }

        Vector3 finishedScale = structure.transform.localScale;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, buildingTime);

        if (progressText != null)
        {
            progressText.gameObject.SetActive(true);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            structure.transform.localScale = Vector3.Lerp(finishedScale * 0.05f, finishedScale, progress);

            if (progressText != null)
            {
                progressText.text = "Building: " + Mathf.RoundToInt(progress * 100f) + "%";
            }

            yield return null;
        }

        structure.transform.localScale = finishedScale;
        isConstructing = false;

        if (progressText != null)
        {
            progressText.text = "Building: 100%";
            yield return new WaitForSeconds(0.35f);
            progressText.gameObject.SetActive(false);
        }
    }

    private bool HasResources()
    {
        switch (selectedStructure)
        {
            case StructureType.Campfire:
                return wallet != null && wallet.CanAfford(CurrencyType.Wood, 8) &&
                    wallet.CanAfford(CurrencyType.Stone, 4);
            case StructureType.Bed:
                return wallet != null && wallet.CanAfford(CurrencyType.Wood, 4) &&
                    wallet.CanAfford(CurrencyType.Wool, 6);
            case StructureType.WoodWall:
                return wallet != null && wallet.CanAfford(CurrencyType.Wood, 4);
            default:
                return false;
        }
    }

    private bool SpendResources()
    {
        if (!HasResources())
        {
            return false;
        }

        switch (selectedStructure)
        {
            case StructureType.Campfire:
                wallet.TrySpend(CurrencyType.Wood, 8);
                wallet.TrySpend(CurrencyType.Stone, 4);
                break;
            case StructureType.Bed:
                wallet.TrySpend(CurrencyType.Wood, 4);
                wallet.TrySpend(CurrencyType.Wool, 6);
                break;
            case StructureType.WoodWall:
                wallet.TrySpend(CurrencyType.Wood, 4);
                break;
        }

        return true;
    }

    private Vector2Int GetFootprint()
    {
        switch (selectedStructure)
        {
            case StructureType.Campfire:
                return new Vector2Int(2, 2);
            case StructureType.Bed:
                return new Vector2Int(2, 1);
            case StructureType.WoodWall:
                return new Vector2Int(1, 1);
            default:
                return Vector2Int.one;
        }
    }

    private GameObject GetSelectedPrefab()
    {
        switch (selectedStructure)
        {
            case StructureType.Campfire:
                return campfirePrefab;
            case StructureType.Bed:
                return bedPrefab;
            case StructureType.WoodWall:
                return woodWallPrefab;
            default:
                return null;
        }
    }

    private GameObject GetSelectedPreviewPrefab()
    {
        switch (selectedStructure)
        {
            case StructureType.Campfire:
                return campfirePreviewPrefab != null ? campfirePreviewPrefab : campfirePrefab;
            case StructureType.Bed:
                return bedPreviewPrefab != null ? bedPreviewPrefab : bedPrefab;
            case StructureType.WoodWall:
                return woodWallPreviewPrefab != null ? woodWallPreviewPrefab : woodWallPrefab;
            default:
                return null;
        }
    }

    private void MarkCellsOccupied(Vector2Int origin, Vector2Int footprint)
    {
        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                occupiedCells.Add(origin + new Vector2Int(x, y));
            }
        }
    }

    private void ApplyPreviewMaterial(Material material)
    {
        if (previewRenderers == null)
        {
            return;
        }

        for (int i = 0; i < previewRenderers.Length; i++)
        {
            Renderer targetRenderer = previewRenderers[i];
            Material[] materials = new Material[targetRenderer.sharedMaterials.Length];
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = material;
            }
            targetRenderer.sharedMaterials = materials;
        }
    }

    private Material CreatePreviewMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        Material material = new Material(shader);
        material.color = color;
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }

    private void ClearSelection()
    {
        selectedStructure = StructureType.None;
        previewIsValid = false;
        DestroyPreview();
    }

    private void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
        previewObject = null;
        previewRenderers = null;
    }

    private void OnDestroy()
    {
        if (isBuildingMode)
        {
            Cursor.visible = previousCursorVisible;
            Cursor.lockState = previousCursorLockState;
        }
        IsBuildingModeActive = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Destroy(validPreviewMaterial);
        Destroy(invalidPreviewMaterial);
    }
}
