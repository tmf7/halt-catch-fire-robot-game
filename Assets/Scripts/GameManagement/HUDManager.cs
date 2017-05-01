using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour {

	public static HUDManager 	instance = null;
	public int					levelDuration = 30;
	public int 					robotsEarnedToAdd = 2;

	[HideInInspector]
	public int robotsFiredThisLevel = 0;
	[HideInInspector]
	public int repairsThisLevel = 0;
	[HideInInspector]
	public int boxesThisLevel = 0;
	[HideInInspector]
	public bool 	playSprinklerSystem = false;
	[HideInInspector]
	public bool 	resetSprinklerCooldown = false;

	// player stats
	private Text 	boxesText;
	private Text 	robotsText;
	private Text 	timeText;
	private float	levelEndTime;
	private float	lastTimeRemainingValue;
	private int 	boxesCollected = 0;
	private int 	robotsFired = 0;
	private int 	robotsRepaired = 0;
	private int 	robotIncreaseThreshold = 10;

	private ImageSwapButton pauseButton;

	public bool isLevelTimeUp {
		get { 
			return Time.time > levelEndTime;
		}
	}

	public bool allRobotsFired {
		get { 
			return robotsFired >= GameManager.instance.maxRobots;
		}
	}

	public int robotsRemaining {
		get { 
			return GameManager.instance.maxRobots - robotsFired;			
		}																	
	}														
		
	public int levelTimeRemaining {
		get { 
			return isLevelTimeUp ? 0 : levelDuration - Mathf.RoundToInt(Time.timeSinceLevelLoad);
		}
	}

	public int totalRobotsRepaired {
		get { 
			return robotsRepaired;
		}
	}

	public int totalBoxesCollected {
		get { 
			return boxesCollected;
		}
	}

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
	}

	void Start () {
		boxesText = GameObject.Find ("BoxesText").GetComponent<Text>();
		robotsText = GameObject.Find ("RobotsText").GetComponent<Text>();
		timeText = GameObject.Find ("TimeText").GetComponent<Text>();
		pauseButton = GameObject.Find ("PauseButton").GetComponentInChildren<ImageSwapButton> ();
	}
		
	void Update() {
		if (!GameManager.instance.levelEnded)
			lastTimeRemainingValue = levelTimeRemaining;
		
		boxesText.text = "Boxes Orbited: " + boxesCollected.ToString();
		robotsText.text = "Robots Left: " + GameManager.instance.robotCount.ToString();
		timeText.text = "Time: " + (GameManager.instance.levelEnded ? lastTimeRemainingValue : levelTimeRemaining).ToString();
	}

	public void StartSprinklerSystem () {
		instance.playSprinklerSystem = true;
	}

	public void ResetSprinklerCooldown() {
		instance.resetSprinklerCooldown = true;
	}

	public void TogglePauseButtonImage() {
		pauseButton.ToggleImage ();
	}

	public void StartLevelTimer() {
		levelEndTime = Time.time + levelDuration;
		ResetLevelStats ();
	}

	public void CollectBox() {
		boxesCollected++;
		boxesThisLevel++;
		if (boxesCollected % robotIncreaseThreshold == 0) {
			GameManager.instance.IncreaseMaxRobots (robotsEarnedToAdd);
			// TODO: start at 3, increase by 2 every 10 up to 10, then every 30
			// allow the threshold to oscillate if the robotCount drops
			// ? make the threshold visible ?
		}
	}

	public void FireRobot() {
		robotsFired++;
		robotsFiredThisLevel++;
	}

	public void RobotRepairComplete() {
		robotsRepaired++;
		repairsThisLevel++;
	}

	public void ResetGameStats() {
		robotsRepaired = 0;
		boxesCollected = 0;
		robotsFired = 0;
	}

	public void ResetLevelStats() {
		repairsThisLevel = 0;
		robotsFiredThisLevel = 0;
		boxesThisLevel = 0;
	}
}
