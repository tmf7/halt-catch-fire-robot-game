using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour {

	public static HUDManager	instance = null;
//	public  float				difficulty = 0;
	public int					levelDuration = 30;
	public int 					robotsEarnedToAdd = 2;

	[HideInInspector]
	public int 					robotsFiredThisLevel = 0;
	[HideInInspector]
	public int 					robotsBuiltThisLevel = 0;
	[HideInInspector]
	public int 					firesPutOutThisLevel = 0;
	[HideInInspector]
	public int 					boxesThisLevel = 0;
	[HideInInspector]
	public bool 				playSprinklerSystem = false;
	[HideInInspector]
	public bool 				resetSprinklerCooldown = false;

	// player stats
	private Text 				boxesText;
	private Text 				robotsText;
	private Text 				timeText;
	private float				levelEndTime;
	private float				lastTimeRemainingValue;
	private int 				boxesCollected = 0;
	private int 				robotsFired = 0;
	private int 				robotsBuilt = 0;
	private int 				firesPutOut = 0;
	private int 				robotIncreaseThreshold = 10;
	private bool				emotionHandleHeld = false;

	private ImageSwapButton 	pauseButton;
	private Slider 				globalEmotionSlider;
	private Button 				globalEmotionButton;
	private Image				globalEmotionImage;

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

	public int totalBoxesCollected {
		get { 
			return boxesCollected;
		}
	}

	public int totalFiresPutOut {
		get { 
			return firesPutOut;
		}
	}

	public int totalRobotsBuilt {
		get { 
			return robotsBuilt;
		}
	}

	public bool isEmotionSliderHeld {
		get { 
			return emotionHandleHeld;
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
		globalEmotionSlider = GameObject.Find ("EmotionSlider").GetComponent<Slider> ();
		globalEmotionButton = GameObject.Find ("EmotionButton").GetComponent<Button> ();
		globalEmotionImage = GameObject.Find ("EmotionImage").GetComponent<Image> ();
		globalEmotionImage.enabled = false;
		UpdatetGlobalEmotionInterface ();
	}
		
	void Update() {
		if (!GameManager.instance.levelEnded)
			lastTimeRemainingValue = levelTimeRemaining;
		
		boxesText.text = "Boxes Orbited: " + boxesCollected.ToString();
		robotsText.text = "Robots Left: " + GameManager.instance.robotCount.ToString();
		timeText.text = "Time: " + (GameManager.instance.levelEnded ? lastTimeRemainingValue : levelTimeRemaining).ToString();
		UpdatetGlobalEmotionInterface ();
	}

	public void UpdatetGlobalEmotionInterface() {
		Robot robot = RobotGrabber.instance.currentGrabbedRobot;
		globalEmotionSlider.gameObject.SetActive (robot != null);
		globalEmotionButton.gameObject.SetActive (robot != null);
		if (!globalEmotionSlider.gameObject.activeSelf)
			return;

		if (!emotionHandleHeld)
			globalEmotionSlider.value = robot.emotionalStability;
		else
			robot.emotionalStability = globalEmotionSlider.value;
		
		globalEmotionButton.interactable = robot.emotionalStability >= 1.0f;
		globalEmotionImage.enabled = globalEmotionButton.interactable;
		globalEmotionImage.sprite = robot.currentSpeech.sprite;
	}
		
	public void HoldEmotionHandle() {
		instance.emotionHandleHeld = true;
	}
		
	public void ReleaseEmotionHandle() {
		instance.emotionHandleHeld = false;
	}
		
	public void ToggleRobotEmotionalState() {
		Robot robot = RobotGrabber.instance.currentGrabbedRobot;
		if (robot == null)
			return;
		robot.ToggleCrazyEmotion ();
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
		}
	}

	public void ExtinguishFire() {
		firesPutOut++;
		firesPutOutThisLevel++;
	}

	public void FireRobot() {
		robotsFired++;
		robotsFiredThisLevel++;
	}

	public void BuildRobot() {
		robotsBuilt++;
		robotsBuiltThisLevel++;
	}

	public void ResetGameStats() {
		firesPutOut = 0;
		boxesCollected = 0;
		robotsBuilt = 0;
		robotsFired = 0;
	}

	public void ResetLevelStats() {
		firesPutOutThisLevel = 0;
		robotsFiredThisLevel = 0;
		robotsBuiltThisLevel = 0;
		boxesThisLevel = 0;
	}
}
