using UnityEngine;

// This attribute automatically adds a Collider component to any GameObject this script is attached to.
// This can be useful if you want to detect when the AI or player enters the node's area.
[RequireComponent(typeof(Collider))]
public class Node : MonoBehaviour
{
    // An array to hold references to other Node objects this node is connected to.
    // The [SerializeField] attribute makes this private variable visible in the Unity Inspector.
    [SerializeField] private Node[] nodes;

    // A public property to safely access the array of connected nodes from other scripts,
    // like your EnemyAppear or Enemywalk script. This follows good encapsulation practice.
    public Node[] Nodes
    {
        get { return nodes; }
    }

    // This is a special Unity method that is called only in the Editor when the object is selected.
    // It's used here to draw visual aids (Gizmos) in the Scene view.
    private void OnDrawGizmosSelected()
    {
        // Set the color of the lines that will be drawn.
        Gizmos.color = Color.yellow;

        // Loop through all the nodes connected to this one.
        for (int i = 0; i < nodes.Length; i++)
        {
            // Check if the connected node reference is not null to avoid errors.
            if (nodes[i] != null)
            {
                // Draw a yellow line from this node's position to the connected node's position.
                // This creates a visual map of your AI's potential paths.
                Gizmos.DrawLine(this.transform.position, nodes[i].transform.position);
            }
        }
    }
}



