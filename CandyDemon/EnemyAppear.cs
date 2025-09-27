using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAppear : MonoBehaviour
{
    private EnemyStateManager stateManager;
    private Animator animator;
    private Dictionary<string, NodeData> nodeName2data;
    private EnemyDifficultySettings currentSettings;

    // Defines the different animation poses the AI can take upon appearing.
    [System.Serializable]
    public enum Actions
    {
        TPose, AroundCorner, Standing, PeekingBelow, AtTable, AttackingDoor, Pounced, Kill
    }

    // Connects a Node in the scene to a specific action and a weight for selection.
    [System.Serializable]
    public class NodeData
    {
        public Node node;
        public bool weight; // If false, this node will be ignored.
        public Actions action;
    }

    public Node startLocation;
    [SerializeField]
    private Node currentLocation;
    public NodeData[] nodeData;

    [Header("Behavior Settings")]
    public Transform playerTransform;

    void Awake()
    {
        stateManager = GetComponentInParent<EnemyStateManager>();
        if (stateManager == null)
        {
            Debug.LogError("EnemyStateManager not found! Disabling EnemyAppear.");
            enabled = false;
            return;
        }
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on the enemy GameObject!");
        }

        // Create a dictionary for fast lookups of NodeData based on the node's name.
        nodeName2data = new Dictionary<string, NodeData>();
        foreach (var data in nodeData)
        {
            if (data.node != null)
            {
                nodeName2data[data.node.name] = data;
            }
        }
    }

    // This method is called by the EnemyStateManager when this behavior is activated.
    void OnEnable()
    {
        Transition();
    }

    public void SetDifficultySettings(EnemyDifficultySettings settings)
    {
        currentSettings = settings;
    }

    // Main logic to decide where and how the enemy should appear.
    public void Transition()
    {
        if (stateManager.canUnlockAbility3)
        {
            // In the most aggressive phase, teleport directly near the player.
            TeleportToPlayer();
        }
        else if (stateManager.canUnlockAbility2)
        {
            // In the middle phase, choose nodes but prioritize those closer to the player.
            ChooseTeleportNode(true);
        }
        else
        {
            // In the early phase, choose nodes randomly.
            ChooseTeleportNode(false);
        }
    }

    private void TeleportToPlayer()
    {
        if (playerTransform != null && currentSettings != null)
        {
            Vector3 playerPos = playerTransform.position;
            // Find a random point within a certain range of the player.
            Vector3 teleportPosition = playerPos + (Random.insideUnitSphere * currentSettings.teleportToPlayerRange);
            teleportPosition.y = transform.position.y; // Keep the same height.

            NavMeshHit hit;
            // Check if the random point is a valid location on the NavMesh.
            if (NavMesh.SamplePosition(teleportPosition, out hit, currentSettings.teleportToPlayerRange, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                transform.LookAt(playerTransform); // Face the player.
                SetPose(Actions.Pounced); // Use a specific "pounce" animation.
            }
            else
            {
                // If a valid NavMesh point isn't found, fall back to node-based teleporting.
                ChooseTeleportNode(true);
            }
        }
    }

    private void ChooseTeleportNode(bool aggressive)
    {
        currentLocation = currentLocation ?? startLocation;

        if (currentLocation == null)
        {
            Debug.LogError("Both current and start locations are null! Cannot transition.");
            return;
        }

        Node[] outboundNodes = currentLocation.Nodes;
        if (outboundNodes == null || outboundNodes.Length == 0)
        {
            Debug.LogWarning("No valid outbound nodes found. Switching back to walking patrol.");
            stateManager.OnPatrolComplete(); // Tell the state manager this behavior is done.
            return;
        }

        List<NodeData> potentialNodes = new List<NodeData>();
        foreach (var outboundNode in outboundNodes)
        {
            // Filter for nodes that have corresponding NodeData and are weighted to be selectable.
            if (outboundNode != null && nodeName2data.TryGetValue(outboundNode.name, out var temp) && temp.weight)
            {
                potentialNodes.Add(temp);
            }
        }

        if (potentialNodes.Count > 0)
        {
            NodeData chosenNodeData;
            if (aggressive)
            {
                // If aggressive, sort nodes by distance to the player (closest first).
                potentialNodes.Sort((a, b) => Vector3.Distance(a.node.transform.position, playerTransform.position)
                                             .CompareTo(Vector3.Distance(b.node.transform.position, playerTransform.position)));
                // Pick one of the 3 closest nodes to add some variety.
                chosenNodeData = potentialNodes[Random.Range(0, Mathf.Min(3, potentialNodes.Count))];
            }
            else
            {
                // If not aggressive, pick any random valid node.
                chosenNodeData = potentialNodes[Random.Range(0, potentialNodes.Count)];
            }

            // Move to the chosen node and set the corresponding pose.
            currentLocation = chosenNodeData.node;
            transform.position = currentLocation.transform.position;
            transform.rotation = currentLocation.transform.rotation;
            SetPose(chosenNodeData.action);
        }
        else
        {
            Debug.LogWarning("No valid outbound nodes with weight found. Switching back to walking patrol.");
            stateManager.OnPatrolComplete();
        }
    }

    private void SetPose(Actions action)
    {
        if (animator != null)
        {
            // This loop is a safe way to ensure no other animation triggers are active.
            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.ResetTrigger(parameter.name);
                }
            }

            // Set the trigger for the chosen action's animation.
            switch (action)
            {
                case Actions.TPose: animator.SetTrigger("TPoseTrigger"); break;
                case Actions.AroundCorner: animator.SetTrigger("AroundCornerTrigger"); break;
                case Actions.Standing: animator.SetTrigger("StandingTrigger"); break;
                case Actions.PeekingBelow: animator.SetTrigger("PeekingBelowTrigger"); break;
                case Actions.AtTable: animator.SetTrigger("AtTableTrigger"); break;
                case Actions.AttackingDoor: animator.SetTrigger("AttackingDoorTrigger"); break;
                case Actions.Pounced: animator.SetTrigger("PouncedTrigger"); break;
                case Actions.Kill: animator.SetTrigger("KillTrigger"); break;
            }
        }
    }
}