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
using UnityEngine.UI;

public class BallMovement : MonoBehaviour
{
	private float speed = 20F;
	private float boostMultiplier = 100F;
	private float keyDownMultiplier = 10F;
	private Rigidbody rb;
	private BaseGame baseGame;
	string[] outOfBoundsObjectNames = new string[]
	{
		"OOBNorth",
		"OOBEast",
		"OOBSouth",
		"OOBWest",
		"OOBBottom",
		"OOBTop"
	};

	void Start()
	{
		rb = GetComponent<Rigidbody>();
		baseGame = GameObject.Find("BaseGame").GetComponent<BaseGame>();
	}

	void FixedUpdate()
	{
		Vector3 acc = Input.acceleration;
		rb.AddForce(acc.x * speed * acc.magnitude, 0, acc.y * speed * acc.magnitude);
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			//Debug.Log ("DownArrow down " + Vector3.back);
			rb.AddForce(Vector3.back * speed * keyDownMultiplier);
		}

		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			//Debug.Log ("UpArrow down " + Vector3.forward);
			rb.AddForce(Vector3.forward * speed * keyDownMultiplier);
		}

		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			//Debug.Log ("DownArrow down " + Vector3.back);
			rb.AddForce(Vector3.left * speed * keyDownMultiplier);
		}

		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			//Debug.Log ("DownArrow down " + Vector3.back);
			rb.AddForce(Vector3.right * speed * keyDownMultiplier);
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			Boost();
		}

		UpdateSpeed();

	}

	public void UpdateSpeed()
	{
		Vector3 velocity = GetComponent<Rigidbody>().velocity;      //to get a Vector3 representation of the velocity
		double speed = System.Math.Round(velocity.magnitude, 1);
		GameObject.Find("SpeedText").GetComponent<Text>().text = "" + speed;
	}

	public void Boost()
	{
		Rigidbody rb = GameObject.Find("Sphere").GetComponent<Rigidbody>();
		Vector3 localVelocity = rb.velocity;
		//Debug.Log ("localVelocity" + localVelocity.x + " " + localVelocity.y);
		rb.AddForce(localVelocity.x * boostMultiplier, localVelocity.y * boostMultiplier, localVelocity.z * boostMultiplier);
	}

	void OnCollisionEnter(Collision collision)
	{
		Debug.Log("Collision with " + collision.gameObject.name);
		if (System.Array.IndexOf(outOfBoundsObjectNames, collision.gameObject.name) > -1)
		{
			baseGame.ShowGameOver();
			Destroy(this);
		}
	}

}
