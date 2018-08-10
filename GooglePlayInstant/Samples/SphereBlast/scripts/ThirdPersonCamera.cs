using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class thirdPersonCamera : MonoBehaviour {

    Quaternion rotation;
    public Transform sphere;
    Vector3 offset;

    void Start () {
        offset = transform.position - sphere.transform.position;
    }

    void FixedUpdate()
    {
        if (sphere == null)
            return;
        transform.LookAt(sphere);
        Vector3 desiredPosition = sphere.transform.position + offset;
        transform.position = desiredPosition;
    }    

//    void LateUpdate()
//    {
//        transform.rotation = rotation;
//    }
}
