using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class explode : MonoBehaviour {
    private float MAX_TIME = 0.5f;
    private float timer = 0;

    // On start, increase the size of the explosion blast sphere
	public void Start () {
        this.transform.localScale += new Vector3(2.5F, 2.5F, 2.5F);
        timer = 0;
        Collider sphereCollider = GameObject.Find("Sphere").GetComponent<SphereCollider>();
        Physics.IgnoreCollision(GetComponent<SphereCollider>(), sphereCollider);
	}
	
	// After a half second, destroy the explosion blast sphere
	void FixedUpdate () {
        timer += Time.fixedDeltaTime;
        if (timer > MAX_TIME)
            Destroy(gameObject);
	}
}
