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

    private void HandleHit(GameObject hitObject)
    {
        // ───── ถ้าถูก Parry แล้ว → ทำดาเมจ Enemy แทน ─────
        if (isParried)
        {
            // ข้าม Player (ไม่ทำดาเมจตัวเอง)
            if (hitObject.CompareTag("Player")) return;

            EnemyHealth enemyHealth = hitObject.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                Debug.Log($"[Parried Projectile] ⚡ โดน {hitObject.name} → {damage} DMG");
                Destroy(gameObject);
                return;
            }

            // Fallback: EnemyHP
            EnemyHP oldHP = hitObject.GetComponentInParent<EnemyHP>();
            if (oldHP != null)
            {
                oldHP.TakeDamage((float)damage);
                Debug.Log($"[Parried Projectile] ⚡ โดน {hitObject.name} → {damage} DMG (EnemyHP)");
                Destroy(gameObject);
                return;
            }


            // ถ้าชนอะไรอย่างอื่น (กำแพง, พื้น) ให้ทำลายทิ้งด้วย
            Debug.Log($"[Parried Projectile] 💥 ชน {hitObject.name} (กำแพง/พื้น) และสลายไป");
            Destroy(gameObject);
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
