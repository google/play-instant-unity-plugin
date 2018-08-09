using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodenCrateExplosion : MonoBehaviour
{
    private float destroyDelay = 3f;
    private float timer = 0;
    public GameObject sphere;
    public AudioSource explosionClip;



    void Start() {
        explosionClip = GetComponent<AudioSource>();
        sphere = GameObject.Find("Sphere");
        Collider[] currentColliders = GetComponentsInChildren<BoxCollider>();

        for (int j = 0; j < currentColliders.Length; j++)
        {
            Physics.IgnoreCollision(currentColliders[j], sphere.GetComponent<SphereCollider>());
        }
        explosionClip.Play();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > destroyDelay)
        {
            Destroy(gameObject);
            Debug.Log("Timer is done");
            BaseGame bg = GameObject.Find("BaseGame").GetComponent<BaseGame>();
            bg.CreateCrate();
        }
    }
}
