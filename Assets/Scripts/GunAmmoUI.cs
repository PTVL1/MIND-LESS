using UnityEngine;
using UnityEngine.UI;

public class GunAmmoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Left Hand UI")]
    [SerializeField] private GameObject leftAmmoRoot;
    [SerializeField] private Text leftAmmoText;

    [Header("Right Hand UI")]
    [SerializeField] private GameObject rightAmmoRoot;
    [SerializeField] private Text rightAmmoText;

    [Header("Display")]
    [SerializeField] private bool hideEmptySlots;
    [SerializeField] private bool showWeaponName = true;
    [SerializeField] private string emptySlotText = "";
    [SerializeField] private string noAmmoText = "—";
    [SerializeField] private string leftLabel = "L";
    [SerializeField] private string rightLabel = "R";

    private WeaponItem currentLeftWeapon;
    private WeaponItem currentRightWeapon;
    private IAmmoWeapon currentLeftAmmoWeapon;
    private IAmmoWeapon currentRightAmmoWeapon;

    private void Awake()
    {
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
        }

        if (leftAmmoRoot == null && leftAmmoText != null)
        {
            leftAmmoRoot = leftAmmoText.gameObject;
        }

        if (rightAmmoRoot == null && rightAmmoText != null)
        {
            rightAmmoRoot = rightAmmoText.gameObject;
        }
    }

    private void OnEnable()
    {
        if (playerInventory != null)
        {
            playerInventory.InventoryChanged += HandleInventoryChanged;
        }

        RefreshBindings();
        RefreshTexts();
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.InventoryChanged -= HandleInventoryChanged;
        }

        UnbindLeftAmmoWeapon();
        UnbindRightAmmoWeapon();
    }

    private void HandleInventoryChanged()
    {
        RefreshBindings();
        RefreshTexts();
    }

    private void RefreshBindings()
    {
        BindLeftWeapon(playerInventory != null ? playerInventory.LeftHandWeapon : null);
        BindRightWeapon(playerInventory != null ? playerInventory.RightHandWeapon : null);
    }

    private void BindLeftWeapon(WeaponItem weapon)
    {
        if (currentLeftWeapon == weapon)
        {
            return;
        }

        UnbindLeftAmmoWeapon();
        currentLeftWeapon = weapon;
        currentLeftAmmoWeapon = GetAmmoWeapon(weapon);

        if (currentLeftAmmoWeapon != null)
        {
            currentLeftAmmoWeapon.AmmoChanged += HandleAmmoChanged;
        }
    }

    private void BindRightWeapon(WeaponItem weapon)
    {
        if (currentRightWeapon == weapon)
        {
            return;
        }

        UnbindRightAmmoWeapon();
        currentRightWeapon = weapon;
        currentRightAmmoWeapon = GetAmmoWeapon(weapon);

        if (currentRightAmmoWeapon != null)
        {
            currentRightAmmoWeapon.AmmoChanged += HandleAmmoChanged;
        }
    }

    private IAmmoWeapon GetAmmoWeapon(WeaponItem weapon)
    {
        return weapon != null && weapon.TryGetAmmoWeapon(out IAmmoWeapon ammoWeapon)
            ? ammoWeapon
            : null;
    }

    private void HandleAmmoChanged(IAmmoWeapon ammoWeapon)
    {
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        RefreshSingleSlot(currentLeftWeapon, currentLeftAmmoWeapon, leftAmmoText, leftAmmoRoot, leftLabel);
        RefreshSingleSlot(currentRightWeapon, currentRightAmmoWeapon, rightAmmoText, rightAmmoRoot, rightLabel);
    }

    private void RefreshSingleSlot(
        WeaponItem weapon,
        IAmmoWeapon ammoWeapon,
        Text text,
        GameObject root,
        string handLabel)
    {
        if (text == null)
        {
            return;
        }

        if (weapon == null)
        {
            text.text = emptySlotText;
            if (root != null)
            {
                root.SetActive(!hideEmptySlots);
            }
            return;
        }

        if (root != null)
        {
            root.SetActive(true);
        }

        string displayName = ammoWeapon != null ? ammoWeapon.AmmoDisplayName : weapon.WeaponName;
        string ammo = ammoWeapon != null
            ? ammoWeapon.CurrentAmmo + " / " + ammoWeapon.MaxAmmo
            : noAmmoText;

        text.text = showWeaponName
            ? handLabel + " " + displayName + ": " + ammo
            : handLabel + ": " + ammo;
    }

    private void UnbindLeftAmmoWeapon()
    {
        if (currentLeftAmmoWeapon != null)
        {
            currentLeftAmmoWeapon.AmmoChanged -= HandleAmmoChanged;
        }
        currentLeftAmmoWeapon = null;
        currentLeftWeapon = null;
    }

    private void UnbindRightAmmoWeapon()
    {
        if (currentRightAmmoWeapon != null)
        {
            currentRightAmmoWeapon.AmmoChanged -= HandleAmmoChanged;
        }
        currentRightAmmoWeapon = null;
        currentRightWeapon = null;
    }
}
