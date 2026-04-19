using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    
    [Header("Combat")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public int attackDamage = 10;
    
    [Header("Hitbox Setup")]
    public Transform attackPoint;
    public float hitboxRadius = 0.5f;
    public LayerMask playerLayer;

    [Header("Audio & Visuals")]
    public AudioClip attackSfx;
    public GameObject attackVfx;

    private Transform playerTransform;
    private NavMeshAgent agent;
    private float nextAttackTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;

        // Find the player object using its tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure the player object has the 'Player' tag.");
        }
    }

    void Update()
    {
        // If the player exists, track and possibly attack them
        if (playerTransform != null)
        {
            // Calculate distance to player (ignoring Y axis height difference)
            Vector3 flatPlayerPos = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            Vector3 flatEnemyPos = new Vector3(transform.position.x, 0, transform.position.z);
            float distanceToPlayer = Vector3.Distance(flatEnemyPos, flatPlayerPos);

            // Calculate direction to player manually to face them
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0; // Ignore the Y-axis 

            // Check if player is within attack range
            if (distanceToPlayer <= attackRange)
            {
                // Stop moving to attack
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                
                // Make the enemy face the player
                if (direction != Vector3.zero)
                {
                    transform.forward = direction;
                }
                
                // Check if attack cooldown has finished
                if (Time.time >= nextAttackTime)
                {
                    Attack();
                    nextAttackTime = Time.time + attackCooldown; // Reset cooldown
                }
            }
            else
            {
                // Move the enemy using NavMeshAgent
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(playerTransform.position);
                }
            }
        }
    }

    private void Attack()
    {
        Debug.Log("Enemy swings at player!");

        if (attackSfx != null)
        {
            AudioSource.PlayClipAtPoint(attackSfx, transform.position);
        }

        if (attackVfx != null && attackPoint != null)
        {
            Instantiate(attackVfx, attackPoint.position, attackPoint.rotation);
        }

        // We can't do the hitbox check if the attack point isn't assigned
        if (attackPoint == null)
        {
            Debug.LogWarning("Attack Point is missing! Please assign an empty GameObject to the Attack Point slot.");
            return;
        }

        // Check for player inside the hitbox (overlap sphere)
        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, hitboxRadius, playerLayer);
        
        foreach (Collider hitPlayer in hitPlayers)
        {
            Debug.Log("Enemy hit " + hitPlayer.name + " for " + attackDamage + " damage!");
            
            PlayerHealth health = hitPlayer.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
            }
        }
    }

    // This method draws the hitbox visually in the Unity Editor Scene view
    private void OnDrawGizmosSelected()
    {
        // Draw the yellow stopping/attack range ring
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw the red actual attack hitbox (where the damage happens)
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, hitboxRadius);
        }
    }
}
