﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

using System.Collections.Generic;
using UnityEngine.UI;					//Allows us to use UI.
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {
/*	
	public float levelStartDelay = 2f;						//Time to wait before starting level, in seconds.
*/	
	public static GameManager 	instance = null;
	public int 					maxRobots = 10;
	public int					maxBoxes = 20;
	public float 				acceptableSearchRangeSqr = 50.0f;		// stop looking for somthing closer if currently queried item is within this range

	// heierarchy organization
	private Transform 		boxHolder;		
	private Transform		robotHolder;
	private List<Box> 		allBoxes;
	private List<Robot> 	allRobots;
	private List<Transform>	deliveryPoints;
	private List<Transform> hazardPoints;

	void Awake() {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
	}

	public void InitLevel() {

		boxHolder = new GameObject ("Boxes").transform;
		robotHolder = new GameObject ("Robots").transform;
		allBoxes = new List<Box>();
		allRobots = new List<Robot> ();
		deliveryPoints = new List<Transform> ();
		hazardPoints = new List<Transform> ();

		GameObject[] exits = GameObject.FindGameObjectsWithTag ("BoxExit");
		for (int i = 0; i < exits.Length; i++)
			deliveryPoints.Add (exits[i].transform);

		GameObject[] crushers = GameObject.FindGameObjectsWithTag ("Crusher");
		GameObject[] furnaces = GameObject.FindGameObjectsWithTag ("Furnace");
		GameObject[] pits = GameObject.FindGameObjectsWithTag ("Pit");
		for (int i = 0; i < crushers.Length; i++)
			hazardPoints.Add (crushers[i].transform);
		for (int i = 0; i < furnaces.Length; i++)
			hazardPoints.Add (furnaces[i].transform);
		for (int i = 0; i < pits.Length; i++)
			hazardPoints.Add (pits[i].transform);
	}

	public void AddBox(Box newBox) {
		newBox.transform.SetParent (boxHolder);
		newBox.SetShadowParent (boxHolder);
		allBoxes.Add (newBox);
	}

	public void AddRobot(Robot newRobot) {
		newRobot.transform.SetParent (robotHolder);
		newRobot.SetShadowParent (robotHolder);
		allRobots.Add (newRobot);
	}

	public void Remove(Throwable item) {
		if (item is Box) {
			allBoxes.Remove (item as Box);
		} else if (item is Robot) {
			allRobots.Remove (item as Robot);
		}
	}

	public Transform GetClosestBoxTarget(Robot robot) {
		Box closestBox = null;
		float minRangeSqr = float.MaxValue;

		foreach (Box box in allBoxes) {
			if (box.isBeingCarried || box.isTargeted)
				continue;

			float rangeSqr = (box.transform.position - robot.transform.position).sqrMagnitude;

			if (rangeSqr < acceptableSearchRangeSqr) {
				box.SetTargeter (robot);
				return box.transform;
			} else if (rangeSqr < minRangeSqr) {
				minRangeSqr = rangeSqr;
				closestBox = box;
			}
		}

		if (closestBox != null) {
			closestBox.SetTargeter (robot);
			return closestBox.transform;
		}
		return null;
	}

	public Transform GetClosestRobotTarget(Robot homicidalRobot) {
		Robot closestRobot = null;
		float minRangeSqr = float.MaxValue;

		foreach (Robot robot in allRobots) {
			if (robot.isBeingCarried || robot.isTargeted)
				continue;

			float rangeSqr = (robot.transform.position - homicidalRobot.transform.position).sqrMagnitude;

			if (rangeSqr < acceptableSearchRangeSqr) {
				robot.SetTargeter (homicidalRobot);
				return robot.transform;
			} else if (rangeSqr < minRangeSqr) {
				minRangeSqr = rangeSqr;
				closestRobot = robot;
			}
		}

		if (closestRobot != null) {
			closestRobot.SetTargeter (homicidalRobot);
			return closestRobot.transform;
		}
		return null;
	}

	public Transform GetClosestDeliveryTarget(Robot robot) {
		Transform closestDelivery = null;
		float minRangeSqr = float.MaxValue;

		foreach (Transform deliver in deliveryPoints) {
			float rangeSqr = (deliver.position - robot.transform.position).sqrMagnitude;

			if (rangeSqr < acceptableSearchRangeSqr) {
				return deliver;
			} else if (rangeSqr < minRangeSqr) {
				minRangeSqr = rangeSqr;
				closestDelivery = deliver;
			}
		}

		if (closestDelivery != null)
			return closestDelivery;
	
		return null;
	}

	public Transform GetRandomHazardTarget () {
		return hazardPoints.Count > 0 ? hazardPoints [Random.Range (0, hazardPoints.Count)] : null;
	}

	public int robotCount {
		get {
			return allRobots.Count;
		}
	}
		
	public int boxCount {
		get {
			return allBoxes.Count;
		}
	}
}
/*
//this is called only once, and the paramter tell it to be called only after the scene was loaded
//(otherwise, our Scene Load callback would be called the very first load, and we don't want that)
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static public void CallbackInitialization() {
    //register the callback to be called everytime the scene is loaded
    SceneManager.sceneLoaded += OnSceneLoaded;
}

static private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1) {
    instance.level++;
    instance.InitGame();
}


//Initializes the game for each level.
void InitGame() {
	//While doingSetup is true the player can't move, prevent player from moving while title card is up.
	doingSetup = true;
	
	levelImage = GameObject.Find("LevelImage");
	
	//Get a reference to our text LevelText's text component by finding it by name and calling GetComponent.
	levelText = GameObject.Find("LevelText").GetComponent<Text>();
	
	levelText.text = "Day " + level;
	
	//Set levelImage to active blocking player's view of the game board during setup.
	levelImage.SetActive(true);
	
	//Call the HideLevelImage function with a delay in seconds of levelStartDelay.
	Invoke("HideLevelImage", levelStartDelay);

	enemies.Clear();
	boardScript.SetupScene(level);
	
}

void HideLevelImage() {
	levelImage.SetActive(false);
	
	//Set doingSetup to false allowing player to move again.
	doingSetup = false;
}


public void GameOver() {
	//Set levelText to display number of levels passed and game over message
	levelText.text = "After " + level + " days, you starved.";
	
	//Enable black background image gameObject.
	levelImage.SetActive(true);
	
	//Disable this GameManager.
	enabled = false;
}
*/