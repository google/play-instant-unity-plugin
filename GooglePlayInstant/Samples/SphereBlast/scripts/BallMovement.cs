﻿// Copyright 2018 Google LLC
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
    private float _speed = 20F;
    private float _boostMultiplier = 100F;
    private float _keyDownMultiplier = 10F;
    private Rigidbody _rigidBody;
    private BaseGame _baseGame;

    private readonly string[] _outOfBoundsObjectNames = new string[]
    {
        "OOBNorth",
        "OOBEast",
        "OOBSouth",
        "OOBWest",
        "OOBBottom",
        "OOBTop"
    };

    public void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _baseGame = GameObject.Find("BaseGame").GetComponent<BaseGame>();
    }

    public void FixedUpdate()
    {
        var acc = Input.acceleration;
        _rigidBody.AddForce(acc.x * _speed * acc.magnitude, 0, acc.y * _speed * acc.magnitude);
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            //Debug.Log ("DownArrow down " + Vector3.back);
            _rigidBody.AddForce(Vector3.back * _speed * _keyDownMultiplier);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            //Debug.Log ("UpArrow down " + Vector3.forward);
            _rigidBody.AddForce(Vector3.forward * _speed * _keyDownMultiplier);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            //Debug.Log ("DownArrow down " + Vector3.back);
            _rigidBody.AddForce(Vector3.left * _speed * _keyDownMultiplier);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            //Debug.Log ("DownArrow down " + Vector3.back);
            _rigidBody.AddForce(Vector3.right * _speed * _keyDownMultiplier);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Boost();
        }

        UpdateSpeed();
    }

    public void UpdateSpeed()
    {
        var velocity = GetComponent<Rigidbody>().velocity; //to get a Vector3 representation of the velocity
        var speed = System.Math.Round(velocity.magnitude, 1);
        GameObject.Find("SpeedText").GetComponent<Text>().text = "" + speed;
    }

    public void Boost()
    {
        var rigidBody = GameObject.Find("Sphere").GetComponent<Rigidbody>();
        var localVelocity = rigidBody.velocity;
        //Debug.Log ("localVelocity" + localVelocity.x + " " + localVelocity.y);
        rigidBody.AddForce(localVelocity.x * _boostMultiplier, localVelocity.y * _boostMultiplier,
            localVelocity.z * _boostMultiplier);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision with " + collision.gameObject.name);
        if (System.Array.IndexOf(_outOfBoundsObjectNames, collision.gameObject.name) <= -1) return;
        _baseGame.ShowGameOver();
        Destroy(this);
    }
}