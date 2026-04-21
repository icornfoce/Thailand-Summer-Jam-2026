using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ติดบน Player Root Object
/// ระบบปืนแบบ Dynamic Slot — หมายเลขปืนจะเรียงตามลำดับที่เก็บมา
/// กด 1/2/3 เพื่อสลับปืนในลิสต์
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("=== Weapon Slots ===")]
    [Tooltip("ปืนเริ่มต้น — มีตั้งแต่เริ่มเกม (Slot 1 เสมอ)")]
    public GameObject noobGun;

    [Tooltip("ปืน Sci-Fi Pistol — ได้เมื่อเก็บ Pickup")]
    public GameObject sciFiPistol;

    [Tooltip("ปืน Sci-Fi SMG — ได้เมื่อเก็บ Pickup")]
    public GameObject sciFiSMG;

    [Tooltip("ปืน Railgun — ได้เมื่อเก็บ Pickup")]
    public GameObject railgun;

    // ─────────────────────────────────────────────────────────
    //  Dynamic Weapon List — เรียงตามลำดับที่เก็บ
    // ─────────────────────────────────────────────────────────
    private List<GameObject> collectedWeapons = new List<GameObject>();
    private int currentIndex = 0;

    private readonly KeyCode[] numberKeys = new KeyCode[]
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
    };

    // ─────────────────────────────────────────────────────────
    //  Start — NoobGun เป็น Slot [1] เสมอ
    // ─────────────────────────────────────────────────────────
    void Start()
    {
        SetWeapon(noobGun,     false);
        SetWeapon(sciFiPistol, false);
        SetWeapon(sciFiSMG,    false);
        SetWeapon(railgun,     false);

        if (noobGun != null)
        {
            collectedWeapons.Add(noobGun);
            SwitchToIndex(0);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Update — กด 1/2/3 สลับปืน
    // ─────────────────────────────────────────────────────────
    void Update()
    {
        for (int i = 0; i < numberKeys.Length; i++)
        {
            if (Input.GetKeyDown(numberKeys[i]))
            {
                SwitchToIndex(i);
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Public API — WeaponPickup เรียกเมื่อเก็บปืน
    // ─────────────────────────────────────────────────────────

    public void EquipNoobGun()     => AddAndEquip(noobGun,     "NoobGun");
    public void EquipSciFiPistol() => AddAndEquip(sciFiPistol, "Sci-Fi Pistol");
    public void EquipSciFiSMG()    => AddAndEquip(sciFiSMG,    "Sci-Fi SMG");
    public void EquipRailgun()     => AddAndEquip(railgun,     "Railgun");

    // ─────────────────────────────────────────────────────────
    //  Private — Unlock ปืน (ถ้ายังไม่มี) แล้วสวมทันที
    // ─────────────────────────────────────────────────────────
    private void AddAndEquip(GameObject weapon, string weaponName)
    {
        if (weapon == null) return;

        if (!collectedWeapons.Contains(weapon))
        {
            collectedWeapons.Add(weapon);
            Debug.Log($"[WeaponManager] 🔓 Unlocked: {weaponName} → Slot [{collectedWeapons.Count}]");
        }

        SwitchToIndex(collectedWeapons.IndexOf(weapon));
    }

    // ─────────────────────────────────────────────────────────
    //  Private — สลับไปปืน index ที่กำหนด
    // ─────────────────────────────────────────────────────────
    private void SwitchToIndex(int index)
    {
        if (index < 0 || index >= collectedWeapons.Count)
        {
            Debug.Log($"[WeaponManager] Slot [{index + 1}] ว่างอยู่ ยังไม่มีปืน");
            return;
        }

        foreach (var w in collectedWeapons)
            SetWeapon(w, false);

        currentIndex = index;
        SetWeapon(collectedWeapons[currentIndex], true);
        Debug.Log($"[WeaponManager] 🔫 Slot [{index + 1}]: {collectedWeapons[index].name}");
    }

    private void SetWeapon(GameObject weapon, bool active)
    {
        if (weapon != null)
            weapon.SetActive(active);
    }
}
