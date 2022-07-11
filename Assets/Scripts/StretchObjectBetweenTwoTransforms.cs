using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StretchObjectBetweenTwoTransforms : MonoBehaviour 
{
    public Transform StartTransform;
    public Transform EndTransform;

    Vector3 endV; 
    Vector3 startV;
    Vector3 rotAxisV;
    Vector3 dirV;
    Vector3 cylDefaultOrientation = new Vector3(0,1,0);

    float dist;
    private Vector3 localScale;

    void Start ()
    {
        transform.parent = null;
        localScale = transform.localScale;
    }
    
    void Update () 
    {
        // Position
        endV   = StartTransform.position;
        startV = EndTransform.position;
        transform.position = (endV + startV)/2.0F;

        // Rotation
        dirV = Vector3.Normalize(endV - startV);
        rotAxisV = dirV + cylDefaultOrientation;
        rotAxisV = Vector3.Normalize(rotAxisV);
        transform.rotation = new Quaternion(rotAxisV.x, rotAxisV.y, rotAxisV.z, 0);

        // Scale        
        dist = Vector3.Distance(endV, startV);
        transform.localScale = new Vector3(localScale.x, dist/2, localScale.z);

    }
}