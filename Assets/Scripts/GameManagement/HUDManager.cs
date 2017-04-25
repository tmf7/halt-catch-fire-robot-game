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

	public int levelTimeRemaining {
		get { 
			return isLevelTimeUp ? 0 : levelDuration - Mathf.RoundToInt(Time.timeSinceLevelLoad);
		}
	}

	[HideInInspector]
	public int robotsFiredThisLevel = 0;
	[HideInInspector]
	public int repairsThisLevel = 0;
	[HideInInspector]
	public int boxesThisLevel = 0;

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
		boxesText.text = "Boxes Shipped: " + boxesCollected.ToString();
		robotsText.text = "Robots Left: " + (RobotNames.Instance.maxNames - robotsFired).ToString();
		timeText.text = "Time: " + levelTimeRemaining.ToString();
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
	}

	public void FireRobot() {
		robotsFired++;
		robotsFiredThisLevel++;
	}

	public void RobotRepairComplete() {
		robotsRepaired++;
		repairsThisLevel++;
	}

	public void ResetLevelStats() {
		repairsThisLevel = 0;
		robotsFiredThisLevel = 0;
		boxesThisLevel = 0;
	}
}
