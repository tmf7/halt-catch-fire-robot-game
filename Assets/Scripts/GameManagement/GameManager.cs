using UnityEngine;
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
	public Gradient 			redWaveGradient;
	public Gradient				blueWaveGradient;
	public Gradient				greenWaveGradient;
	public int					shiftTimeRemaining = 120;
	public float				globalSpawnDelay = 5.0f;
	public int 					maxRobots = 10;
	public int					maxBoxes = 20;
	public float 				acceptableSearchRangeSqr = 50.0f;		// stop looking for somthing closer if currently queried item is within this range

	// heierarchy organization
	private Transform 		boxHolder;		
	private Transform		robotHolder;
	private List<Box> 		allBoxes;
	private List<Robot> 	allRobots;
	private List<RobotDoor>	allDoors;
	private List<Transform>	deliveryPoints;
	private List<Transform> hazardPoints;

	// player stats
	private Text boxesText;
	private Text robotsText;
	private Text repairText;
	private Text timeText;
	private Text spawnText;

	private int boxesCollected = 0;
	private int robotsFired = 0;
	private int robotsRepaired = 0;
	private float	nextRobotSpawnTime;
	private bool spawningRobots = false;

	void Awake() {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
		nextRobotSpawnTime = Time.time + globalSpawnDelay;
	}

	void Update() {
		if (SceneManager.GetActiveScene ().buildIndex > 0) {
			boxesText.text = "Boxes Collected: " + boxesCollected;
			robotsText.text = "Robots Fired: " + robotsFired + "/" + RobotNames.Instance.numRobotNames;		// FIXME: this should just start at names.Length, then count down with each death (easier to understand)
			repairText.text = "Repairs Made: " + robotsRepaired;
			timeText.text = "Shift Change In: " + (shiftTimeRemaining - Mathf.RoundToInt(Time.timeSinceLevelLoad));

			int recruitTime = Mathf.RoundToInt(nextRobotSpawnTime - Time.time);
			if (recruitTime < 0)
				recruitTime = 0;
			spawnText.text = "New Recruits In: " + recruitTime;

			if (Time.time > nextRobotSpawnTime) {
				if (!spawningRobots && robotCount < maxRobots) {
					spawningRobots = true;

					foreach (RobotDoor door in allDoors) {
						door.TriggerDoorOpen ();
					}
				} else if (robotCount == maxRobots) {		// FIXME: this assumes that none died during the spawn cycle, so foreach door.spawnEnabled = false after what WOULD have replenished the supply have been added
					bool allClosed = true;					// IE: robotsToAdd = maxRobots - robotCount; then robotsAdded = 0; then robotsAdded++; for each addRobot call

					foreach (RobotDoor door in allDoors) {
						if (!door.isClosed) {
							allClosed = false;
							break;
						}
					}

					if (allClosed) {
						nextRobotSpawnTime = Time.time + globalSpawnDelay;
						spawningRobots = false;
					}
				}
			}
		}
	}
		
	public void InitLevel() {

		boxesText = GameObject.Find ("BoxesText").GetComponent<Text>();
		robotsText = GameObject.Find ("RobotsText").GetComponent<Text>();
		repairText = GameObject.Find ("RepairText").GetComponent<Text>();
		timeText = GameObject.Find ("TimeText").GetComponent<Text>();
		spawnText = GameObject.Find ("SpawnText").GetComponent<Text>();

		boxHolder = new GameObject ("Boxes").transform;
		robotHolder = new GameObject ("Robots").transform;
		allBoxes = new List<Box>();
		allRobots = new List<Robot> ();
		allDoors = new List<RobotDoor> ();
		deliveryPoints = new List<Transform> ();
		hazardPoints = new List<Transform> ();

		GameObject[] doors = GameObject.FindGameObjectsWithTag ("Respawn");
		for (int i = 0; i < doors.Length; i++)
			allDoors.Add (doors[i].GetComponent<RobotDoor>());

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
			if ((item as Box).hasExited)
				boxesCollected++;
			allBoxes.Remove (item as Box);
		} else if (item is Robot) {
			robotsFired++;
			allRobots.Remove (item as Robot);
		}
	}

	public void RobotRepairComplete() {
		robotsRepaired++;
	}

	public Transform GetClosestBoxTarget(Robot robot) {
		Box closestBox = null;
		float minRangeSqr = float.MaxValue;

		foreach (Box box in allBoxes) {
			if (box.isBeingCarried || box.isTargeted || box.hasExited || box.fellInPit)
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
			if (robot.isBeingCarried || robot.isTargeted || robot.fellInPit)
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
		return closestDelivery;
	}

	public Transform GetClosestHazardTarget(Robot robot) {
		Transform closestHazard = null;
		float minRangeSqr = float.MaxValue;

		foreach (Transform hazard in hazardPoints) {
			float rangeSqr = (hazard.position - robot.transform.position).sqrMagnitude;

			if (rangeSqr < acceptableSearchRangeSqr) {
				return hazard;
			} else if (rangeSqr < minRangeSqr) {
				minRangeSqr = rangeSqr;
				closestHazard = hazard;
			}
		}
		return closestHazard;
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