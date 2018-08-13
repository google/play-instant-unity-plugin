using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodPanel : MonoBehaviour {
    private AudioSource woodPanelClip;
    private Hashtable played;
    void Start() 
    {
        woodPanelClip = GetComponent<AudioSource>();
        played = new Hashtable();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (played.ContainsKey(gameObject.name))
        {
            return;
        }

        Debug.Log("WOOD PANEL COLLISION with " + collision.gameObject.name);
        woodPanelClip.pitch = Random.Range(0.75f, 1.5f);
        woodPanelClip.volume = Random.Range(0.5f, 2.0f);
        woodPanelClip.PlayDelayed(Random.Range(0.0f,1.0f));
        //played.Add(gameObject.name, true);
    }
	
}
