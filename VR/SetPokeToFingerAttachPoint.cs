using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SetPokeToFingerAttachPoint : MonoBehaviour
{
    public Transform PokeAttacPoint;    //where to attach poke interact

    private XRPokeInteractor _xrPokeInteractor;
    // Start is called before the first frame update
    void Start()
    {
        _xrPokeInteractor = transform.parent.parent.GetComponentInChildren<XRPokeInteractor>();
        SetPokeAttachPoint();
    }

    // Update is called once per frame
    void SetPokeAttachPoint()
    {
        if (PokeAttacPoint == null)
        {
            Debug.Log("Attach point's not connected");
            return;
        }

        if(_xrPokeInteractor == null)
        {
            Debug.Log("Poke Interactor Null");
            return;
        }

        _xrPokeInteractor.attachTransform = PokeAttacPoint;
    }
}
