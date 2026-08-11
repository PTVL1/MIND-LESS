using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    public event Action InventoryChanged;
    public WeaponItem LeftHandWeapon => slot1;
    public WeaponItem RightHandWeapon => slot2;

    public bool HasAnyWeaponEquipped()
    {
        return slot1 != null || slot2 != null;
    }

    public float GetEquippedWalkSpeedMultiplier()
    {
        float multiplier = 1f;
        multiplier = Mathf.Max(multiplier, GetMeleeWalkSpeedMultiplier(slot1));
        multiplier = Mathf.Max(multiplier, GetMeleeWalkSpeedMultiplier(slot2));
        return multiplier;
    }

    private float GetMeleeWalkSpeedMultiplier(WeaponItem weapon)
    {
        if (weapon == null)
        {
            return 1f;
        }

        MeleeWeapon meleeWeapon = weapon.GetComponent<MeleeWeapon>();
        return meleeWeapon != null ? meleeWeapon.WalkSpeedMultiplier : 1f;
    }

    [Header("Input")]
    [SerializeField] private KeyCode switchSlotsKey = KeyCode.Q;
    [SerializeField] private KeyCode collectKey = KeyCode.E;
    [SerializeField] private KeyCode dropSlotOneKey = KeyCode.Backspace;

    [Header("Hand Mounts")]
    [SerializeField] private Transform leftHandMount;
    [SerializeField] private Transform rightHandMount;
    [SerializeField] private Transform dropPoint;

    [Header("Collect")]
    [SerializeField] private float collectRadius = 2f;
    [SerializeField] private LayerMask collectMask = ~0;

    [Header("UI")]
    [SerializeField] private Image slot1IconImage;
    [SerializeField] private Image slot2IconImage;
    [SerializeField] private Image slot1HighlightImage;
    [SerializeField] private Image slot2HighlightImage;
    [SerializeField] private Text slot1BulletsText;
    [SerializeField] private Text slot2BulletsText;
    [SerializeField] private Color activeSlotColor = Color.white;
    [SerializeField] private Color inactiveSlotColor = new Color(1f, 1f, 1f, 0.35f);

    [Header("Debug")]
    [SerializeField] private WeaponItem slot1;
    [SerializeField] private WeaponItem slot2;
    [SerializeField] [Range(1, 2)] private int activeSlot = 1;

    private readonly List<WeaponItem> nearbyWeapons = new List<WeaponItem>();
    private readonly List<WeaponItem> previewedWeaponsLastFrame = new List<WeaponItem>();

    private void Start()
    {
        AttachSlotWeaponsToHands();
        RefreshInventoryVisuals();
        InventoryChanged?.Invoke();
    }

    private void Update()
    {
        UpdatePickupPreview();
        RefreshBulletsText();

        if (Input.GetKeyDown(switchSlotsKey))
        {
            SwitchSlots();
        }

        if (Input.GetKeyDown(dropSlotOneKey))
        {
            DropSlotOneWeapon();
        }

        if (Input.GetKeyDown(collectKey))
        {
            TryCollectNearbyWeapon();
        }
    }

    private void SwitchSlots()
    {
        WeaponItem temp = slot1;
        slot1 = slot2;
        slot2 = temp;

        activeSlot = activeSlot == 1 ? 2 : 1;

        AttachSlotWeaponsToHands();
        RefreshInventoryVisuals();
        InventoryChanged?.Invoke();
    }

    private void TryCollectNearbyWeapon()
    {
        if (slot1 != null && slot2 != null)
        {
            return;
        }

        WeaponItem nearbyWeapon = FindClosestCollectibleWeapon();
        if (nearbyWeapon == null)
        {
            return;
        }

        CollectWeapon(nearbyWeapon);
    }

    private WeaponItem FindClosestCollectibleWeapon()
    {
        FillNearbyCollectibleWeapons(nearbyWeapons);
        return GetClosestWeapon(nearbyWeapons);
    }

    private void FillNearbyCollectibleWeapons(List<WeaponItem> output)
    {
        output.Clear();

        Collider[] hits = Physics.OverlapSphere(transform.position, collectRadius, collectMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            WeaponItem weapon = hits[i].GetComponentInParent<WeaponItem>();
            if (weapon == null || output.Contains(weapon))
            {
                continue;
            }

            if (weapon == slot1 || weapon == slot2)
            {
                continue;
            }

            output.Add(weapon);
        }
    }

    private WeaponItem GetClosestWeapon(List<WeaponItem> weapons)
    {
        WeaponItem closest = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponItem weapon = weapons[i];
            float sqrDistance = (weapon.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance >= bestSqrDistance)
            {
                continue;
            }

            closest = weapon;
            bestSqrDistance = sqrDistance;
        }

        return closest;
    }

    private void UpdatePickupPreview()
    {
        FillNearbyCollectibleWeapons(nearbyWeapons);
        WeaponItem targetWeapon = GetClosestWeapon(nearbyWeapons);

        for (int i = 0; i < previewedWeaponsLastFrame.Count; i++)
        {
            WeaponItem oldPreview = previewedWeaponsLastFrame[i];
            if (oldPreview == null)
            {
                continue;
            }

            oldPreview.SetPickupPreview(false, false);
        }

        previewedWeaponsLastFrame.Clear();

        for (int i = 0; i < nearbyWeapons.Count; i++)
        {
            WeaponItem nearby = nearbyWeapons[i];
            bool isTarget = nearby == targetWeapon;

            nearby.SetPickupPreview(isTarget, true);
            previewedWeaponsLastFrame.Add(nearby);
        }
    }

    private void CollectWeapon(WeaponItem weapon)
    {
        if (slot1 == null)
        {
            slot1 = weapon;
            AttachToHand(slot1, leftHandMount);
        }
        else if (slot2 == null)
        {
            slot2 = weapon;
            AttachToHand(slot2, rightHandMount);
        }
        else
        {
            return;
        }

        weapon.SetWorldState(false);
        RefreshInventoryVisuals();
        InventoryChanged?.Invoke();
    }

    private void DropSlotOneWeapon()
    {
        if (slot1 == null)
        {
            return;
        }

        DropWeapon(slot1);
        slot1 = null;

        if (activeSlot == 1)
        {
            activeSlot = 2;
        }

        RefreshInventoryVisuals();
        InventoryChanged?.Invoke();
    }

    private void DropWeapon(WeaponItem weapon)
    {
        weapon.transform.SetParent(null);

        Vector3 spawnPosition = dropPoint != null
            ? dropPoint.position
            : transform.position + (transform.forward * 1.2f);

        weapon.transform.position = spawnPosition;
        weapon.SetWorldState(true);

        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(transform.forward * 2f, ForceMode.VelocityChange);
        }
    }

    private void AttachSlotWeaponsToHands()
    {
        if (slot1 != null)
        {
            AttachToHand(slot1, leftHandMount);
            slot1.SetWorldState(false);
        }

        if (slot2 != null)
        {
            AttachToHand(slot2, rightHandMount);
            slot2.SetWorldState(false);
        }
    }

    private void AttachToHand(WeaponItem weapon, Transform handMount)
    {
        Transform parent = handMount != null ? handMount : transform;
        weapon.transform.SetParent(parent, true);

        Transform grabPoint = weapon.GrabPoint;
        if (grabPoint != null)
        {
            Quaternion deltaRotation = parent.rotation * Quaternion.Inverse(grabPoint.rotation);
            weapon.transform.rotation = deltaRotation * weapon.transform.rotation;

            Vector3 deltaPosition = parent.position - grabPoint.position;
            weapon.transform.position += deltaPosition;
        }
        else
        {
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }

        weapon.gameObject.SetActive(true);
    }

    private void RefreshInventoryVisuals()
    {
        RefreshSlotIcon(slot1IconImage, slot1 != null ? slot1.Icon : null);
        RefreshSlotIcon(slot2IconImage, slot2 != null ? slot2.Icon : null);
        RefreshBulletsText();

        SetHighlightState(slot1HighlightImage, activeSlot == 1);
        SetHighlightState(slot2HighlightImage, activeSlot == 2);
    }

    private void RefreshBulletsText()
    {
        RefreshSlotBulletsText(slot1BulletsText, slot1);
        RefreshSlotBulletsText(slot2BulletsText, slot2);
    }

    private void RefreshSlotBulletsText(Text bulletsText, WeaponItem weapon)
    {
        if (bulletsText == null)
        {
            return;
        }

        PistolWeapon pistol = weapon != null ? weapon.GetComponent<PistolWeapon>() : null;
        if (pistol == null)
        {
            bulletsText.text = string.Empty;
            bulletsText.enabled = false;
            return;
        }

        bulletsText.enabled = true;
        bulletsText.text = pistol.GetCurrentBullets() + " / " + pistol.GetMaxBullets();
    }

    private void RefreshSlotIcon(Image image, Sprite icon)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = icon;
        image.enabled = icon != null;
    }

    private void SetHighlightState(Image image, bool isActive)
    {
        if (image == null)
        {
            return;
        }

        image.color = isActive ? activeSlotColor : inactiveSlotColor;
    }


    public bool IsWeaponActive(WeaponItem weapon)
    {
        if (weapon == null)
        {
            return false;
        }

        if (slot1 == null && slot2 == weapon)
        {
            return true;
        }

        if (slot2 == null && slot1 == weapon)
        {
            return true;
        }

        return activeSlot == 1 ? slot1 == weapon : slot2 == weapon;
    }

    public WeaponItem GetActiveWeapon()
    {
        return activeSlot == 1 ? slot1 : slot2;
    }


    public int GetWeaponSlot(WeaponItem weapon)
    {
        if (weapon == null)
        {
            return 0;
        }

        if (slot1 == weapon)
        {
            return 1;
        }

        if (slot2 == weapon)
        {
            return 2;
        }

        return 0;
    }

    public bool IsWeaponEquipped(WeaponItem weapon)
    {
        return GetWeaponSlot(weapon) != 0;
    }

    public bool IsWeaponTriggerHeld(WeaponItem weapon)
    {
        int slot = GetWeaponSlot(weapon);

        if (slot == 1)
        {
            return Input.GetMouseButton(0);
        }

        if (slot == 2)
        {
            return Input.GetMouseButton(1);
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectRadius);
    }
}
