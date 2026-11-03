using TMPro;
using UnityEngine;

public class AmmoHUD : MonoBehaviour
{
    [SerializeField] private PlayerWeaponController weapons;
    [SerializeField] private PlayerAmmoInventory ammo;
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text magazineText;
    [SerializeField] private TMP_Text reserveText;
    [SerializeField] private GameObject reloadingIndicator;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowAmmoColor = new Color(0.9f, 0.3f, 0.2f);

    private Weapon current;

    private void OnEnable()
    {
        weapons.WeaponChanged += OnWeaponChanged;
        ammo.Changed += OnReserveChanged;
        if (weapons.CurrentWeapon != null) OnWeaponChanged(weapons.CurrentWeapon);
    }

    private void OnDisable()
    {
        weapons.WeaponChanged -= OnWeaponChanged;
        ammo.Changed -= OnReserveChanged;
        Unsubscribe();
    }

    private void OnWeaponChanged(Weapon weapon)
    {
        Unsubscribe();
        current = weapon;
        if (current != null)
        {
            current.AmmoChanged += Refresh;
            current.ReloadStarted += Refresh;
            current.ReloadFinished += Refresh;
        }
        Refresh();
    }

    private void Unsubscribe()
    {
        if (current == null) return;
        current.AmmoChanged -= Refresh;
        current.ReloadStarted -= Refresh;
        current.ReloadFinished -= Refresh;
    }

    private void OnReserveChanged(AmmoTypeSO type, int amount) => Refresh();

    private void Refresh()
    {
        if (current == null) return;

        WeaponDataSO data = current.Data;
        if (weaponNameText != null) weaponNameText.text = data.displayName;

        magazineText.text = current.CurrentMagazine.ToString();
        bool lowAmmo = current.CurrentMagazine <= Mathf.CeilToInt(data.magazineSize * 0.25f);
        magazineText.color = lowAmmo ? lowAmmoColor : normalColor;

        reserveText.text = "/ " + current.Reserve;
        if (reloadingIndicator != null) reloadingIndicator.SetActive(current.IsReloading);
    }
}
