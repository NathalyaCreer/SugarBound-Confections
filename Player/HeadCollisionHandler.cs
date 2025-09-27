using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadCollisionHandler : MonoBehaviour
{
    [SerializeField]
    private HeadCollisionDetector _detector;        //references to detector
    [SerializeField]
    private CharacterController _characterController;
    [SerializeField]
    public float pushBackStrength = 1.0f;           //strength of pushback
    private Vector3 CalculatePushBackDirection(List<RaycastHit> colliderHits)   //calculates direction to push
    {
        Vector3 combinedNormal = Vector3.zero;
        foreach (RaycastHit hitPoint in colliderHits)
        {
            combinedNormal +=
                new Vector3(hitPoint.normal.x, 0, hitPoint.normal.z); ;
        }
        return combinedNormal;
    }

    private void Update()       //visualize pushback direction
    {
        if (_detector.DetectedColliderHits.Count <= 0)
        {
            return;
        }
        Vector3 pushBackDirection
            = CalculatePushBackDirection(_detector.DetectedColliderHits);

        Debug.DrawRay(transform.position, pushBackDirection.normalized, Color.magenta);

        _characterController
            .Move(pushBackDirection.normalized * pushBackStrength * Time.deltaTime); //keep pushing til not colliding
    }
}