using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour {

	public static HUDManager	instance = null;
	public int					levelDuration = 30;
	public int 					robotsEarnedToAdd = 2;

	[HideInInspector]
	public int 					robotsFiredThisLevel = 0;
	[HideInInspector]
	public int 					repairsThisLevel = 0;
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
	private int 				robotsRepaired = 0;
	private int 				firesPutOut = 0;
	private int 				robotIncreaseThreshold = 10;
	private bool 				wasEmotionButtonInteractable = false;
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

	public int totalFiresPutOut {
		get { 
			return firesPutOut;
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
		globalEmotionButton = globalEmotionSlider.GetComponentInChildren<Button> ();
		globalEmotionImage = globalEmotionButton.GetComponentInChildren<Image> ();
		UpdatetGlobalEmotionSlider ();
	}
		
	void Update() {
		if (!GameManager.instance.levelEnded)
			lastTimeRemainingValue = levelTimeRemaining;
		
		boxesText.text = "Boxes Orbited: " + boxesCollected.ToString();
		robotsText.text = "Robots Left: " + GameManager.instance.robotCount.ToString();
		timeText.text = "Time: " + (GameManager.instance.levelEnded ? lastTimeRemainingValue : levelTimeRemaining).ToString();
		UpdatetGlobalEmotionSlider ();
	}

	public void UpdatetGlobalEmotionSlider() {
		Robot robot = RobotGrabber.instance.currentGrabbedRobot;
		globalEmotionSlider.gameObject.SetActive (robot != null);
		if (!globalEmotionSlider.gameObject.activeSelf)
			return;

		if (!emotionHandleHeld)
			globalEmotionSlider.value = robot.emotionalStability;
		else 
			robot.emotionalStability = globalEmotionSlider.value;
		
		globalEmotionButton.interactable = robot.emotionalStability >= 1.0f;
		globalEmotionImage.sprite = robot.currentSpeech.sprite;

		if (robot.emotionalStability >= 1.0f && !wasEmotionButtonInteractable)
			robot.QuickEmotionalBreakdownToggle ();

		wasEmotionButtonInteractable = globalEmotionButton.interactable;
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
		robot.QuickEmotionalBreakdownToggle ();
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

	public void ExtinguishFire() {
		firesPutOut++;
		firesPutOutThisLevel++;
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
		firesPutOut = 0;
		robotsRepaired = 0;
		boxesCollected = 0;
		robotsFired = 0;
	}

	public void ResetLevelStats() {
		firesPutOutThisLevel = 0;
		repairsThisLevel = 0;
		robotsFiredThisLevel = 0;
		boxesThisLevel = 0;
	}
}
