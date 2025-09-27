using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemywalk : MonoBehaviour
{
    private enum EnemyState
    {
        Patrolling,
        Chasing,
        Stealing,
        Distracted,
        ForcingCook,
        Idle // Use for temporary pauses or inactive states
    }

    private EnemyStateManager stateManager;
    private NavMeshAgent navMeshAgent;
    private CookingManager cookingManager;
    private EnemyDifficultySettings currentSettings;
    private EnemyState currentState = EnemyState.Idle;

    [Header("Patrol Nodes")]
    public Node[] walkNodes;
    private Transform previousNode;

    [Header("Chasing Player")]
    public Transform playerTransform;
    public LayerMask playerLayer;

    [Header("Stealing")]
    public Transform cookingStationStealLocation; // A specific location to steal from
    public float ingredientStealRadius = 2f; // The missing variable
    private bool hasStolenThisCapture = false;

    void Awake()
    {
        stateManager = GetComponentInParent<EnemyStateManager>();
        if (stateManager == null)
        {
            Debug.LogError("EnemyStateManager not found! Disabling Enemywalk.");
            enabled = false;
            return;
        }
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent not found on the enemy! Disabling Enemywalk.");
            enabled = false;
        }
        // Use singleton pattern for CookingManager
        cookingManager = CookingManager.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for player only when actively chasing
        if (currentState == EnemyState.Chasing && !hasStolenThisCapture)
        {
            if (other.CompareTag("Player"))
            {
                CapturePlayer();
            }
        }
    }

    void OnDisable()
    {
        currentState = EnemyState.Idle;
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.ResetPath();
            navMeshAgent.velocity = Vector3.zero;
        }
    }

    public void SetDifficultySettings(EnemyDifficultySettings settings)
    {
        currentSettings = settings;
    }

    public void StartPatrol()
    {
        if (walkNodes.Length == 0)
        {
            Debug.LogWarning("No walk nodes assigned. Ending patrol.");
            stateManager.OnPatrolComplete();
            return;
        }
        currentState = EnemyState.Patrolling;
        FindNewNode();
    }
    
    // Changed public void RetreatFromCooking() to a private coroutine
    private IEnumerator RetreatPause()
    {
        navMeshAgent.isStopped = true;
        yield return new WaitForSeconds(5.0f);
        navMeshAgent.isStopped = false;
        stateManager.OnPatrolComplete();
    }

    void Update()
    {
        // Priority 1: Distraction (highest priority)
        if (stateManager.isDistracted)
        {
            navMeshAgent.SetDestination(stateManager.distractionLocation);
            currentState = EnemyState.Distracted;
            return;
        }
        
        // Priority 2: Force Cook (high priority)
        if (stateManager.isForcingCook)
        {
            navMeshAgent.SetDestination(cookingManager.bakingStationTeleportPoint.position);
            currentState = EnemyState.ForcingCook;
            return;
        }

        // Priority 3: Stealing from cooking station
        if (stateManager.canSteal && cookingManager != null && cookingManager.isMinigameActive)
        {
            float distanceToStation = Vector3.Distance(transform.position, cookingManager.transform.position);
            if (distanceToStation <= ingredientStealRadius)
            {
                if (currentState != EnemyState.Stealing)
                {
                    StealIngredientAtStation();
                }
            }
            else
            {
                if (currentState != EnemyState.Stealing)
                {
                    currentState = EnemyState.Stealing;
                    navMeshAgent.SetDestination(cookingStationStealLocation.position);
                }
            }
        }
        // Priority 4: Chasing the player
        else if (stateManager.canUnlockAbility2)
        {
            // Use OverlapSphere to detect player
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, currentSettings.detectionRadius, playerLayer);
            if (hitColliders.Length > 0)
            {
                currentState = EnemyState.Chasing;
                navMeshAgent.SetDestination(playerTransform.position);
            }
            else if (currentState == EnemyState.Chasing) // Revert to patrol if player lost
            {
                StartPatrol();
            }
        }
        // Priority 5: Patrolling (lowest priority)
        else if (currentState == EnemyState.Patrolling && navMeshAgent.enabled && !navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
        {
            FindNewNode();
        }
    }

    private void CapturePlayer()
    {
        Debug.Log("Enemy caught the player! Stealing ingredient.");
        stateManager.StealIngredientFromPlayer();
        stateManager.PlayRandomVoiceLine();
        hasStolenThisCapture = true;
        StartCoroutine(CapturePause());
    }
    
    private IEnumerator CapturePause()
    {
        navMeshAgent.isStopped = true;
        yield return new WaitForSeconds(3.0f);
        hasStolenThisCapture = false;
        navMeshAgent.isStopped = false;
        StartPatrol();
    }

    private void StealIngredientAtStation()
    {
        Debug.Log("Enemy is stealing ingredient from cooking station.");
        // Call the method on the CookingManager to handle the actual stealing
        // if (cookingManager.StealIngredientFromStation() != null)
        // {
        //    // stealing logic and visuals
        // }
        stateManager.OnPatrolComplete(); // Stealing is a one-off event, so patrol resumes
    }

    private void FindNewNode()
    {
        if (walkNodes.Length == 0) return;

        // Retrieve last player sighting from memory
        Vector3? lastPlayerSighting = null;
        if (stateManager.memorySystem != null)
        {
            Memory? lastSightingMemory = stateManager.memorySystem.GetLastMemoryOfType(Memory.MemoryType.PlayerSighted);
            if (lastSightingMemory.HasValue)
            {
                lastPlayerSighting = lastSightingMemory.Value.location;
            }
        }

        // Weighted random selection for next node
        int totalWeight = 0;
        foreach (var node in walkNodes)
        {
            // You can add more dynamic weight factors here
            totalWeight += 1; // Base weight
            if (lastPlayerSighting.HasValue)
            {
                float distance = Vector3.Distance(node.transform.position, lastPlayerSighting.Value);
                if (distance < 10)
                {
                    totalWeight += 5; // Add bonus weight for being near last known location
                }
            }
        }
        
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        Transform chosenNode = null;

        foreach (var node in walkNodes)
        {
            currentWeight += 1;
            if (lastPlayerSighting.HasValue)
            {
                float distance = Vector3.Distance(node.transform.position, lastPlayerSighting.Value);
                if (distance < 10)
                {
                    currentWeight += 5;
                }
            }

            if (randomValue < currentWeight)
            {
                chosenNode = node.transform;
                break;
            }
        }
        
        if (chosenNode != null)
        {
            previousNode = chosenNode;
            navMeshAgent.SetDestination(chosenNode.position);
        }
        else
        {
            // Fallback to simple random if all weights are zero
            chosenNode = walkNodes[Random.Range(0, walkNodes.Length)].transform;
            previousNode = chosenNode;
            navMeshAgent.SetDestination(chosenNode.position);
        }
    }
}