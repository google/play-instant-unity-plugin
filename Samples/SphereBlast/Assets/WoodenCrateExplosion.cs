// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
