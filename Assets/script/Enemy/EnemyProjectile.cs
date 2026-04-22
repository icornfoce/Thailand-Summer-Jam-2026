using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [HideInInspector]
    public int damage = 10; // This is set by the RangedEnemy script
    
    public float lifetime = 5f; // How long before the bullet destroys itself if it misses

    /// <summary>
    /// เมื่อถูก Parry จะเป็น true → ทำดาเมจ Enemy แทน Player
    /// </summary>
    [HideInInspector]
    public bool isParried = false;

    void Start()
    {
        // Destroy the bullet after 'lifetime' seconds to prevent them floating forever
        Destroy(gameObject, lifetime);
    }

    // This handles physical collisions (if the bullet has a non-trigger collider)
    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    // This handles trigger overlaps (if you checked "Is Trigger" on the bullet's collider)
    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    [Header("Parry Logic")]
    public string enemyLayerName = "Enemy";

    private void HandleHit(GameObject hitObject)
    {
        // ───── ถ้าถูก Parry แล้ว → ทำดาเมจ Enemy แทน ─────
        if (isParried)
        {
            // ข้าม Player (ไม่ทำดาเมจตัวเอง)
            if (hitObject.CompareTag("Player")) return;

            // เช็คว่าอยู่ใน Layer ศัตรูหรือไม่ (ตามที่ USER ต้องการ)
            bool hitEnemyLayer = hitObject.layer == LayerMask.NameToLayer(enemyLayerName);

            EnemyHealth enemyHealth = hitObject.GetComponentInParent<EnemyHealth>();
            EnemyHP oldHP = hitObject.GetComponentInParent<EnemyHP>();

            // ถ้าโดน Layer ศัตรู หรือมีสคริปต์เลือด ให้ทำดาเมจและทำลายตัวเอง
            if (hitEnemyLayer || enemyHealth != null || oldHP != null)
            {
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                    Debug.Log($"[Parried Projectile] ⚡ โดน {hitObject.name} (EnemyHealth) → {damage} DMG");
                }
                else if (oldHP != null)
                {
                    oldHP.TakeDamage((float)damage);
                    Debug.Log($"[Parried Projectile] ⚡ โดน {hitObject.name} (EnemyHP) → {damage} DMG");
                }
                else
                {
                    Debug.Log($"[Parried Projectile] ⚡ โดน {hitObject.name} (Enemy Layer เท่านั้น)");
                }

                Destroy(gameObject);
                return;
            }

            // ถ้าชนอะไรอย่างอื่น (กำแพง, พื้น) ให้ทำลายทิ้งด้วย
            if (!hitObject.CompareTag("Player"))
            {
                Debug.Log($"[Parried Projectile] 💥 ชนสิ่งกีดขวาง {hitObject.name} (Tag: {hitObject.tag}) และสลายไป");
                Destroy(gameObject);
            }
            return;
        }

        // ───── ปกติ → ทำดาเมจ Player ─────
        if (hitObject.CompareTag("Player"))
        {
            Debug.Log("Bullet hit the Player for " + damage + " damage!");
            PlayerHealth health = hitObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
