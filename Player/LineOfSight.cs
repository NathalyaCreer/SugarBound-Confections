using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineOfSight : MonoBehaviour
{
    // The Field of View angle for the player's gaze, adjustable in the inspector
    [Range(1, 180)]
    public float fieldOfViewAngle = 60f;
    public float sightRange = 30f;
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer;

    private FearMeter fearMeter;
    private Collider[] enemyColliders = new Collider[50]; // Pre-allocated array for performance

    void Start()
    {
        // Find the FearMeter script on the player
        fearMeter = GetComponentInParent<FearMeter>();
        if (fearMeter == null)
        {
            Debug.LogError("FearMeter script not found on player parent object!");
        }
    }

    void Update()
    {
        CheckForEnemiesInSight();
    }

    void CheckForEnemiesInSight()
    {
        // Use OverlapSphereNonAlloc for better performance and no garbage collection
        int numEnemies = Physics.OverlapSphereNonAlloc(transform.position, sightRange, enemyColliders, enemyLayer);

        for (int i = 0; i < numEnemies; i++)
        {
            Transform enemy = enemyColliders[i].transform;
            Vector3 directionToEnemy = (enemy.position - transform.position).normalized;

            // Check if the enemy is within the VR headset's field of view
            if (Vector3.Angle(transform.forward, directionToEnemy) < fieldOfViewAngle / 2)
            {
                RaycastHit hit;
                // Check for line of sight, ignoring obstacles
                if (Physics.Raycast(transform.position, directionToEnemy, out hit, sightRange, obstacleLayer | enemyLayer))
                {
                    // If the raycast hits the enemy, increase fear
                    if (hit.collider.CompareTag("Animatronic"))
                    {
                        if (fearMeter != null)
                        {
                            fearMeter.IncreaseFear(fearMeter.fearIncreaseRate * Time.deltaTime);
                        }
                    }
                }
            }
        }
    }
}
