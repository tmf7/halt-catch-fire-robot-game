using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour {

	public static HUDManager 	instance = null;
	public int					levelDuration = 30;

	// player stats
	private Text 	boxesText;
	private Text 	robotsText;
	private Text 	timeText;
	private float	levelEndTime;
	private int 	boxesCollected = 0;
	private int 	robotsFired = 0;
	private int 	robotsRepaired = 0;

	private ImageSwapButton pauseButton;

	public bool isLevelTimeUp {
		get { 
			return Time.time > levelEndTime;
		}
	}

	public bool allRobotsFired {
		get { 
			return robotsFired >= RobotNames.Instance.maxNames;
		}
	}

	public int robotsFiredThisLevel {
		get { 
			return robotsFired;			// TODO: separate this per level (temporary knowledge)
		}
	}

	public int repairsThisLevel {
		get { 
			return robotsRepaired;		// TODO: separate this per level (temporary knowledge)
		}
	}

	public int boxesThisLevel {
		get { 
			return boxesCollected;		// TODO: separate this per level (temporary knowledge)
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
		boxesText.text = "Boxes Shipped: " + boxesCollected;
		robotsText.text = "Robots Left: " + (RobotNames.Instance.maxNames - robotsFired);	
		timeText.text = "Time: " + (levelDuration - Mathf.RoundToInt(Time.timeSinceLevelLoad));
	}

	public void TogglePauseButtonImage() {
		pauseButton.ToggleImage ();
	}

	public void StartLevelTimer() {
		levelEndTime = Time.time + levelDuration;
	}

	public void CollectBox() {
		boxesCollected++;
	}

	public void FireRobot() {
		robotsFired++;
	}

	public void RobotRepairComplete() {
		robotsRepaired++;
	}
}
