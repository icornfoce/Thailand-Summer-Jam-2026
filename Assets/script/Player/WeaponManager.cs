using UnityEngine;

/// <summary>
/// ติดบน Player Root Object
/// จัดการการเปิด/ปิด GameObject ของปืนแต่ละกระบอก
/// ปืนทั้งหมดต้องเป็น Children ของ Player และ Disable ไว้ใน Inspector ยกเว้น NoobGun
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("=== Weapon Slots ===")]
    [Tooltip("ปืนเริ่มต้น (เปิดตั้งแต่เริ่มเกม)")]
    public GameObject noobGun;

    [Tooltip("ปืน Sci-Fi Pistol (ปิดไว้ก่อน)")]
    public GameObject sciFiPistol;

    [Tooltip("ปืน Sci-Fi SMG (ปิดไว้ก่อน)")]
    public GameObject sciFiSMG;

    // เพิ่มปืนใหม่ตรงนี้ได้เลย
    // public GameObject sciFiShotgun;

    // ─────────────────────────────────────────────────────────
    //  Start — เปิดแค่ NoobGun, ปิดปืนที่เหลือทั้งหมด
    // ─────────────────────────────────────────────────────────
    void Start()
    {
        EquipNoobGun();
    }

    // ─────────────────────────────────────────────────────────
    //  Public API — เรียกจาก WeaponPickup
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// เปิด NoobGun, ปิดปืนอื่นทั้งหมด
    /// </summary>
    public void EquipNoobGun()
    {
        SetAllWeapons(false);
        SetWeapon(noobGun, true);
        Debug.Log("[WeaponManager] Equipped: NoobGun");
    }

    /// <summary>
    /// เปิด Sci-Fi Pistol, ปิดปืนอื่นทั้งหมด
    /// </summary>
    public void EquipSciFiPistol()
    {
        SetAllWeapons(false);
        SetWeapon(sciFiPistol, true);
        Debug.Log("[WeaponManager] Equipped: Sci-Fi Pistol");
    }

    /// <summary>
    /// เปิด Sci-Fi SMG, ปิดปืนอื่นทั้งหมด
    /// </summary>
    public void EquipSciFiSMG()
    {
        SetAllWeapons(false);
        SetWeapon(sciFiSMG, true);
        Debug.Log("[WeaponManager] Equipped: Sci-Fi SMG");
    }

    // ─────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// ปิด/เปิด GameObject ปืนทั้งหมดใน Slots
    /// </summary>
    private void SetAllWeapons(bool active)
    {
        SetWeapon(noobGun,      active);
        SetWeapon(sciFiPistol,  active);
        SetWeapon(sciFiSMG,     active);
        // SetWeapon(sciFiShotgun, active);
    }

    /// <summary>
    /// ปิด/เปิด GameObject ปืนตัวเดียว (null-safe)
    /// </summary>
    private void SetWeapon(GameObject weapon, bool active)
    {
        if (weapon != null)
            weapon.SetActive(active);
    }
}
