using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPresencePhysics : MonoBehaviour
{

    public Transform target;
    public float positionStrength = 1000f;
    public float rotationStrength = 100f;
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
       rb =  GetComponent<Rigidbody>();
    }

    
    void FixedUpdate()
    {
        //position
        Vector3 positionDelta = target.position - transform.position;
        rb.velocity = positionDelta * positionStrength * Time.fixedDeltaTime;

        //rotation
        Quaternion rotationDifference = target.rotation * Quaternion.Inverse(transform.rotation);
        rotationDifference.ToAngleAxis(out float angleInDegree, out Vector3 rotationAxis);

        Vector3 rotationDifferenceInDegree = angleInDegree * rotationAxis;

        rb.angularVelocity = rotationDifferenceINRadians * rotationStrength * Time.fixedDeltaTime;
    }
}
