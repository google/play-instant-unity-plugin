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
using GooglePlayInstant;

public class BaseGame : MonoBehaviour
{
	public Transform crate;
    public Camera thirdPersonCamera;
    public Camera overheadCamera;
	private static Vector3 position1 = new Vector3(37, 3.87f, 11);
    private static Vector3 position2 = new Vector3(37, 3.87f, -4);
    private static Vector3 position3 = new Vector3(37, 3.87f, -16);
    private static Vector3 position4 = new Vector3(0, 3.87f, -16);
    private static Vector3 position5 = new Vector3(0, 3.87f, -4);
    private static Vector3 position6 = new Vector3(0, 3.87f, 11);
    private static Vector3 position7 = new Vector3(-23, 3.87f, 11);
    private static Vector3 position8 = new Vector3(-23, 3.87f, -4);
    private static Vector3 position9 = new Vector3(-23, 3.87f, -16);

	private int MAX_TIME = 10;
	private static int MAX_SCORE_PER_LEVEL = 5;
	private int timer;
	private int level;
	private int score;
	private float deltaTimeSum;
	private GameObject gameOver;
	private GameObject sphere;
	private GameObject installButtonPersistent;
	private GameObject installButton;
	private Text timeLeftText;
	private Text levelText;
	private Text scoreText;
	private PlayInstant pi;



	private int[] levelTimeLimitMapping = new int []
	{
		10,
		9,
		8,
		7,
		6,
		5,
		4
	};

	static Vector3[] positions = new Vector3 []
	{
		position1,
		position2,
		position3,
		position4,
		position5,
		position6,
		position7,
		position8,
		position9
	};

	// Use this for initialization
	void Start()
	{
		timer = MAX_TIME;
		gameOver = GameObject.Find("GameOverPanel").gameObject;
		timeLeftText = GameObject.Find("TimeLeftText").GetComponent<Text>();
		levelText = GameObject.Find("LevelText").GetComponent<Text>();
		scoreText = GameObject.Find("ScoreText").GetComponent<Text>();
		sphere = GameObject.Find("Sphere");
		installButton = GameObject.Find("InstallButton");
		installButtonPersistent = GameObject.Find("InstallButtonPersistent");
		installButton.SetActive(false);
		installButtonPersistent.SetActive(false);
		pi = new PlayInstant();
		SetUpGameState();
		UpdateTimer();
		ReadGameStateFromCookie();
		SetLevel(level);
		SetScore(score);
		HideGameOver();
		CreateCrate();
        //ShowFirstPersonView();
	}

	void SetUpGameState()
	{
		level = 1;
		deltaTimeSum = 0;
		score = 0;
		#if PLAY_INSTANT
		installButton.SetActive(true);
		installButtonPersistent.SetActive(true);
		#endif
	}

	void FixedUpdate()
	{
		if (timer == -1)
		{
			return;
		}

		deltaTimeSum += Time.deltaTime;
		if ((int)deltaTimeSum == 1)
		{
			timer--;
			deltaTimeSum = 0;
		}

		UpdateTimer();

		if (timer <= 0)
		{
			Destroy(sphere);
			ShowGameOver();
		}
	}

	/* TIMER METHODS */

	public void ResetTimer()
	{
		if (timer > 0)
		{
			timer = MAX_TIME;
		}
	}

	public void UpdateTimer()
	{
		timeLeftText.text = "" + System.Math.Max(timer, 0);
	}

    /* SCENE SET UP METHODS */

	public void CreateCrate()
	{
		int randomPosition = Random.Range(0, 9);
		Instantiate(crate, positions[randomPosition], Quaternion.identity);
	}

	public void WriteGameStateToCookie(int level, int score)
	{
		string gameStateCSV = level + "," + score;
		pi.SetCookie(gameStateCSV);
	}

	public void ReadGameStateFromCookie()
	{
		string results = pi.GetCookie();
		Debug.Log("readGameStateFromCookie: " + results);
		if (results != null
		    && results.Length > 0)
		{
			string[] attributes = results.Split(',');
			if (attributes.Length < 2)
			{
				return;
			}
			string cookieLevel = attributes[0];
			string cookieScore = attributes[1];
			level = int.Parse(cookieLevel);
			score = int.Parse(cookieScore);
			Debug.Log("CookieLevel: " + cookieLevel);
			Debug.Log("CookieScore: " + cookieScore);
		}
	}

	/* LEVEL METHODS */

	public void IncrementLevel()
	{
		SetLevel(level + 1);
	}

	public void SetLevel(int newLevel)
	{
		level = newLevel;
		MAX_TIME = levelTimeLimitMapping[level - 1];
		timer = MAX_TIME;
		levelText.text = "" + level;
	}

	public int GetLevel()
	{
		return level;
	}

	/* UI METHODS */

	public void ShowGameOver()
	{	
		gameOver.SetActive(true);
	}

	public void HideGameOver()
	{
		gameOver.SetActive(false);
	}

	public void UpdateScoreDisplay()
	{
		int displayScore = score % MAX_SCORE_PER_LEVEL;
		scoreText.text = "" + displayScore;
	}

	public void SetScore(int newScore)
	{
		score = newScore;
		Debug.Log("Is instant app: " + pi.IsInstantApp());
		Debug.Log("before setting level " + (score % MAX_SCORE_PER_LEVEL > 0));
		if (score != 0
		    && score % MAX_SCORE_PER_LEVEL == 0)
		{
			IncrementLevel();
		}
		UpdateScoreDisplay();
	}

	public void QuitGame()
	{
		Application.Quit();
	}

	public void RestartGame()
	{
		Application.LoadLevel("SphereScene");
	}

	public void IncrementScore()
	{
		Debug.Log("Increment score");
		SetScore(score + 1);
	}

	public void Install()
	{
		Debug.Log("Installing game");
		WriteGameStateToCookie(level, score);
		Debug.Log("After writing GameState");
		ReadGameStateFromCookie();
		Debug.Log("After reading GameState");
		pi.Install();
	}

    public void ShowOverheadView() {
        thirdPersonCamera.enabled = false;
        overheadCamera.enabled = true;
    }

    public void ShowFirstPersonView() {
        thirdPersonCamera.enabled = true;
        overheadCamera.enabled = false;
    }

}
