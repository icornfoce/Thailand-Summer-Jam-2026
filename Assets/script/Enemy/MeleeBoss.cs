using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MeleeBoss : MonoBehaviour
{
    public enum BossState { Chasing, Charging, Dashing, Cooldown }
    
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float rotationSpeed = 720f;
    public float dashSpeed = 25f;
    
    [Header("Combat Settings")]
    public float attackRange = 2f;
    public float minDashDistance = 6f; // The boss will only dash if the player is further than this
    public float chargeTime = 1.2f;
    public float dashDuration = 0.5f;
    public float attackCooldown = 3f;
    public int attackDamage = 25;

    [Header("Animation")]
    public Animator animator;
    public string chargingBool = "isCharging";
    public string dashingBool = "isDashing";
    public string attackTrigger = "Attack";
    public string runAnimationBool = "isRunning";

    [Header("Audio & Visuals")]
    public GameObject chargeVfxPrefab;
    public GameObject dashVfxPrefab;
    public Transform vfxPoint;
    public AudioClip chargeSfx;
    public AudioClip dashSfx;
    public AudioClip attackSfx;

    [Header("Detection")]
    public LayerMask playerLayer;

    private NavMeshAgent agent;
    private Transform playerTransform;
    private BossState currentState = BossState.Chasing;
    private float stateTimer = 0f;
    private Vector3 dashDirection;
    private GameObject currentChargeVfx;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;
        agent.updateRotation = false; // We handle rotation manually

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        // Move the enemy using NavMeshAgent
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
        }
            
        if (playerTransform == null) return;
        // Calculate distance to player (ignoring Y axis height difference)
            Vector3 flatPlayerPos = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            Vector3 flatEnemyPos = new Vector3(transform.position.x, 0, transform.position.z);
            float distanceToPlayer = Vector3.Distance(flatEnemyPos, flatPlayerPos);

            // Calculate direction to player manually to face them
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0; // Ignore the Y-axis 

            // Manually rotate towards the player at all times
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

        switch (currentState)
        {
            case BossState.Chasing:
                HandleChasing();
                break;
            case BossState.Charging:
                HandleCharging();
                break;
            case BossState.Dashing:
                HandleDashing();
                break;
            case BossState.Cooldown:
                HandleCooldown();
                break;
        }

        // Update Animation
        if (animator != null)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
            animator.SetBool(runAnimationBool, isMoving);
        }
    }

    private void HandleChasing()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Always face the player
        RotateTowards(playerTransform.position);

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
        }

        // Trigger Dash as a gap-closer if player is far away
        if (distanceToPlayer >= minDashDistance && Time.time >= stateTimer)
        {
            StartCharging();
        }
        // Basic melee attack if very close (optional addition for better gameplay)
        else if (distanceToPlayer <= attackRange && Time.time >= stateTimer)
        {
            PerformBasicAttack();
        }
    }

    private void StartCharging()
    {
        currentState = BossState.Charging;
        stateTimer = Time.time + chargeTime;
        
        if (agent.isOnNavMesh) agent.isStopped = true;
        
        if (animator != null) animator.SetBool(chargingBool, true);

        // Play Charge SFX
        if (chargeSfx != null)
        {
            AudioSource.PlayClipAtPoint(chargeSfx, transform.position);
        }

        // Spawn Charge VFX
        if (chargeVfxPrefab != null)
        {
            Transform spawnPoint = vfxPoint != null ? vfxPoint : transform;
            currentChargeVfx = Instantiate(chargeVfxPrefab, spawnPoint.position, spawnPoint.rotation);
            currentChargeVfx.transform.SetParent(spawnPoint); // Make it follow the boss
        }
        
        Debug.Log("Boss is charging...");
    }

    private void HandleCharging()
    {
        // Boss keeps eyes on the player while charging
        RotateTowards(playerTransform.position);

        if (Time.time >= stateTimer)
        {
            StartDashing();
        }
    }

    private void StartDashing()
    {
        currentState = BossState.Dashing;

        // Cleanup Charge VFX
        if (currentChargeVfx != null)
        {
            Destroy(currentChargeVfx);
        }

        // Spawn Dash VFX at current position (Teleport Out)
        if (dashVfxPrefab != null)
        {
            Instantiate(dashVfxPrefab, transform.position, transform.rotation);
        }

        // Play Dash/Teleport SFX
        if (dashSfx != null)
        {
            AudioSource.PlayClipAtPoint(dashSfx, transform.position);
        }
        
        // --- Teleport Logic ---
        Vector3 targetPos = playerTransform.position;
        
        // Try to find a valid position on the NavMesh near the player
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        else
        {
            // Fallback: just move to player pos if NavMesh check fails
            transform.position = targetPos;
        }

        // Spawn Dash VFX at new position (Teleport In)
        if (dashVfxPrefab != null)
        {
            Instantiate(dashVfxPrefab, transform.position, transform.rotation);
        }

        // Face the player after teleporting
        RotateTowards(playerTransform.position);

        if (animator != null)
        {
            animator.SetBool(chargingBool, false);
            // We trigger the attack immediately after teleporting
            animator.SetTrigger(attackTrigger);
        }

        Debug.Log("Boss teleported to player!");
        
        // Immediate damage check after teleport
        CheckForHit();
        
        // Skip the dash duration and go straight to cooldown
        EndDash();
    }

    private void HandleDashing()
    {
        // This is now handled instantly in StartDashing
    }

    private void EndDash()
    {
        currentState = BossState.Cooldown;
        stateTimer = Time.time + attackCooldown;

        if (animator != null)
        {
            animator.SetBool(dashingBool, false);
            animator.SetTrigger(attackTrigger);
        }

        // Play Attack SFX
        if (attackSfx != null)
        {
            AudioSource.PlayClipAtPoint(attackSfx, transform.position);
        }

        Debug.Log("Dash ended, boss cooling down.");
    }

    private void HandleCooldown()
    {
        // Face player while cooling down but don't move
        RotateTowards(playerTransform.position);
        
        if (agent.isOnNavMesh) agent.isStopped = true;

        if (Time.time >= stateTimer)
        {
            currentState = BossState.Chasing;
        }
    }

    private void RotateTowards(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void CheckForHit()
    {
        // Simple overlap check to see if we hit the player during the dash
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward, 1.5f, playerLayer);
        foreach (var hitCollider in hitColliders)
        {
            PlayerHealth health = hitCollider.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
                // We could stop the dash here if we wanted to
                // EndDash(); 
                // break;
            }
        }
    }

    private void PerformBasicAttack()
    {
        stateTimer = Time.time + attackCooldown;
        if (animator != null) animator.SetTrigger(attackTrigger);
        
        // Play Attack SFX
        if (attackSfx != null)
        {
            AudioSource.PlayClipAtPoint(attackSfx, transform.position);
        }

        Debug.Log("Boss performs basic melee attack!");
        
        // Damage check for basic attack
        CheckForHit();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDashDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
