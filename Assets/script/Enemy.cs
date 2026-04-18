using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
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

    private Transform playerTransform;
    private Rigidbody rb;
    private float nextAttackTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze rotation so the physics engine doesn't make the enemy tip over
        rb.freezeRotation = true;

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

    void FixedUpdate()
    {
        // If the player exists, track and possibly attack them
        if (playerTransform != null)
        {
            // Calculate distance to player (ignoring Y axis height difference)
            Vector3 flatPlayerPos = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            Vector3 flatEnemyPos = new Vector3(transform.position.x, 0, transform.position.z);
            float distanceToPlayer = Vector3.Distance(flatEnemyPos, flatPlayerPos);

            // Calculate direction to player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0; // Ignore the Y-axis 

            // Make the enemy face the player
            if (direction != Vector3.zero)
            {
                transform.forward = direction;
            }

            // Check if player is within attack range
            if (distanceToPlayer <= attackRange)
            {
                // Stop moving to attack
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); 
                
                // Check if attack cooldown has finished
                if (Time.time >= nextAttackTime)
                {
                    Attack();
                    nextAttackTime = Time.time + attackCooldown; // Reset cooldown
                }
            }
            else
            {
                // Move the enemy using Rigidbody
                Vector3 targetPosition = transform.position + direction * speed * Time.fixedDeltaTime;
                rb.MovePosition(targetPosition);
            }
        }
    }

    private void Attack()
    {
        Debug.Log("Enemy swings at player!");

        // We can't do the hitbox check if the attack point isn't assigned
        if (attackPoint == null)
        {
            Debug.LogWarning("Attack Point is missing! Please assign an empty GameObject to the Attack Point slot.");
            return;
        }

        // Check for player inside the hitbox (overlap sphere)
        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, hitboxRadius, playerLayer);
        
        foreach (Collider player in hitPlayers)
        {
            Debug.Log("Enemy hit " + player.name + " for " + attackDamage + " damage!");
            
            // TODO: Call your player's damage script here
            // Example: player.GetComponent<PlayerHealth>().TakeDamage(attackDamage);
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
