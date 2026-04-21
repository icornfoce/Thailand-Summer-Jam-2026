using UnityEngine;

/// <summary>
/// ติดบน Item Pickup ในโลก (พร้อม Collider ที่ติ๊ก Is Trigger)
/// เมื่อ Player เดินเข้ามาในขอบเขต → สลับปืนให้ตาม weaponType ที่กำหนด
/// แล้วทำลายตัวเองทิ้ง (หรือจะเก็บไว้ก็ได้ — ดูที่ destroyOnPickup)
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    //  Enum — ประเภทปืนที่ Pickup นี้มอบให้
    // ─────────────────────────────────────────────────────────
    public enum WeaponType
    {
        NoobGun,
        SciFiPistol,
        SciFiSMG,
        Railgun,
    }

    [Header("=== Pickup Settings ===")]
    [Tooltip("ปืนที่ Item นี้จะมอบให้เมื่อ Player เข้ามาในขอบเขต")]
    public WeaponType weaponType = WeaponType.SciFiPistol;

    [Tooltip("ทำลาย Item นี้หลังจาก Pickup หรือไม่?")]
    public bool destroyOnPickup = true;

    [Header("=== Optional Effects ===")]
    [Tooltip("ไฟล์เสียงเมื่อ Pickup (ไม่บังคับ)")]
    public AudioClip pickupSFX;

    [Tooltip("VFX Prefab เมื่อ Pickup (ไม่บังคับ)")]
    public GameObject pickupVFX;

    // ─────────────────────────────────────────────────────────
    //  OnTriggerEnter — ตรวจจับ Player
    // ─────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        // ตรวจว่าเป็น Player ด้วย Tag
        if (!other.CompareTag("Player")) return;

        Debug.Log($"[WeaponPickup] Player เข้ามาใน Trigger ของ {gameObject.name} | Collider ที่โดน: {other.name}");

        // หา WeaponManager — ลองจาก Collider ก่อน แล้ว Fallback ไปหาใน Scene
        WeaponManager weaponManager = other.GetComponentInParent<WeaponManager>();

        if (weaponManager == null)
        {
            // Fallback: หาจากทั้ง Scene (กรณี Collider ไม่ได้อยู่บน root เดียวกับ WeaponManager)
            weaponManager = FindFirstObjectByType<WeaponManager>();
            if (weaponManager != null)
                Debug.Log("[WeaponPickup] พบ WeaponManager ผ่าน Fallback Search");
        }

        if (weaponManager == null)
        {
            Debug.LogError("[WeaponPickup] ❌ ไม่พบ WeaponManager ในทั้ง Scene! " +
                           "ตรวจสอบว่าได้ Add Component 'WeaponManager' บน Player หรือยัง", this);
            return;
        }

        Debug.Log($"[WeaponPickup] ✅ พบ WeaponManager บน: {weaponManager.gameObject.name} | กำลัง Equip: {weaponType}");

        // สลับปืนตาม weaponType
        switch (weaponType)
        {
            case WeaponType.NoobGun:
                weaponManager.EquipNoobGun();
                break;

            case WeaponType.SciFiPistol:
                weaponManager.EquipSciFiPistol();
                break;

            case WeaponType.SciFiSMG:
                weaponManager.EquipSciFiSMG();
                break;

            case WeaponType.Railgun:
                weaponManager.EquipRailgun();
                break;
        }

        // เล่นเสียง Pickup
        if (pickupSFX != null)
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position);

        // เล่น VFX Pickup
        if (pickupVFX != null)
            Instantiate(pickupVFX, transform.position, Quaternion.identity);

        // ทำลาย Item
        if (destroyOnPickup)
            Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────
    //  Gizmos — แสดงขอบ Trigger ใน Scene View
    // ─────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        // วาดวงสีทองแสดงขอบเขตของ Pickup
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.4f);

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box)
                Gizmos.DrawWireCube(box.center, box.size);
            else if (col is SphereCollider sphere)
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
    }
}
