using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AI;

public class NodeManager : MonoBehaviour
{

    public static NodeManager Instance;

    public Node[] nodes;

    public EnemyAppear[] animatronics;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        nodes = FindObjectsOfType<Node>();  //returns nodes
        animatronics = FindObjectsOfType<EnemyAppear>();
    }

    public void TransitionOccured()
    {
        Debug.Log("AI Manager: Transition Occured!");

        for (int i = 0; i < animatronics.Length; i++)
        {
            animatronics[i].Transition();           
        }
    }
}
