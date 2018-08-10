using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodenCrate : MonoBehaviour
{
    public GameObject explodingVersion;
 
    public void Swap_Explode()
    {
        Debug.Log("Swap_Explode triggered");
        Instantiate(explodingVersion, transform.position, transform.rotation);
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("collision with crate detected");
        BaseGame bg = GameObject.Find("BaseGame").GetComponent<BaseGame>();
        bg.IncrementScore();
        bg.ResetTimer();
        bg.UpdateScoreDisplay();
        Swap_Explode();
    }
}
