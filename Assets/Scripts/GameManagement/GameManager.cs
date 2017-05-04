using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {

	public static GameManager 	instance = null;
	public Gradient 			redWaveGradient;
	public Gradient				blueWaveGradient;
	public Gradient				greenWaveGradient;
	public Gradient				silverWaveGradient;
	public Gradient 			blackWaveGradient;
	public float				globalSpawnDelay = 5.0f;
	public int 					maxRobots = 3;
	public int					maxBoxes = 20;
	public float 				acceptableSearchRangeSqr = 50.0f;		// stop looking for something closer if currently queried item is within this range
	public float				drawnPathLengthThresholdSqr = 1.0f;
	public float 				maxDrawnPathLengthSqr = 10.0f;
	public bool 				spawningRobots = false;
	public bool					levelEnded = false;

	// heierarchy organization
	private Transform 			boxHolder;		
	private Transform			robotHolder;

	private List<Box> 			allBoxes;
	private List<Robot> 		allRobots;
	private List<RobotDoor>		allDoors;
	private List<Transform>		deliveryPoints;
	private List<Transform> 	hazardPoints;

	// respawn handling
	private List<Text> 			spawnTexts;
	private float				nextRobotSpawnTime;
	private int 				robotsToSpawnThisCycle;
	private int 				robotsAddedThisCycle;
	private int					initialMaxRobots;

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

	void Awake() {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
		spawnTexts = new List<Text> ();
		initialMaxRobots = maxRobots;
	}

	void Update() {
		if (!levelEnded) {
			UpdateRespawnText ();
			CheckIfLevelOver ();
		}
	}

	// regulate robot spawn rate
	private void UpdateRespawnText() {
		if (robotCount < maxRobots && robotCount < HUDManager.instance.robotsRemaining && HUDManager.instance.levelTimeRemaining > Mathf.RoundToInt(globalSpawnDelay)) {
			if (Time.time > nextRobotSpawnTime) {
				if (!spawningRobots) {
					robotsToSpawnThisCycle = HUDManager.instance.robotsRemaining - robotCount;
					robotsAddedThisCycle = 0;
					spawningRobots = true;

					foreach (RobotDoor door in allDoors) {
						door.spawnEnabled = true;
						door.TriggerDoorOpen ();
					}
				} else if (robotsAddedThisCycle >= robotsToSpawnThisCycle) {
					bool allClosed = true;

					foreach (RobotDoor door in allDoors) {
						door.spawnEnabled = false;
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

			int spawnTime = Mathf.RoundToInt (nextRobotSpawnTime - Time.time);
			if (spawnTime <= 0) {
				HideSpawnText ();
			} else {
				foreach (Text spawnText in spawnTexts)
					spawnText.text = spawnTime.ToString ();
			}
		} else {
			HideSpawnText ();
		}
	}

	private void HideSpawnText() {
		foreach (Text spawnText in spawnTexts)
			spawnText.text = "";
	}

	private void CheckIfLevelOver() {
		if (HUDManager.instance.allRobotsFired || HUDManager.instance.isLevelTimeUp) {
			levelEnded = true;
			AccountForSurvivingRobots ();
			StartCoroutine(allDoors [0].SpawnSlimeBot ());
		}
	}

	private void AccountForSurvivingRobots() {
		foreach (Robot robot in allRobots) {
			RobotNames.Instance.AddRobotSurvivalTime (robot.name, Time.time - robot.spawnTime);
		}
	}

	public void InitLevel() {
		// each RobotDoor has its own spawn text
		spawnTexts.Clear();
		GameObject[] spawnTextObjs = GameObject.FindGameObjectsWithTag ("SpawnText");
		foreach (GameObject spawnTextObj in spawnTextObjs)
			spawnTexts.Add(spawnTextObj.GetComponent<Text>());

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

		Cursor.visible = false;
		spawningRobots = false;
		levelEnded = false;
		nextRobotSpawnTime = Time.time;
		HUDManager.instance.StartLevelTimer ();
	}

	public void ResetMaxRobots () {
		maxRobots = initialMaxRobots;
	}

	public void IncreaseMaxRobots(int increaseBy) {
		maxRobots += increaseBy;
		if (maxRobots > RobotNames.Instance.maxAvailableNames)
			maxRobots = RobotNames.Instance.maxAvailableNames;
	}

	public void KillAllRobots() {
		while (allRobots.Count > 0) {
			allRobots[0].howDied = RobotNames.MethodOfDeath.DEATH_BY_BOMB;
			allRobots[0].Remove ();
		}
	}

	public void StopAllRobots() {
		foreach (Robot robot in allRobots)
			robot.DropItem ();
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
		robotsAddedThisCycle++;
	}

	public void Remove(Throwable item) {
		if (item is Box) {
			allBoxes.Remove (item as Box);
		} else if (item is Robot) {
			HUDManager.instance.FireRobot ();
			allRobots.Remove (item as Robot);
		}
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
			if (robot == homicidalRobot || robot.isBeingCarried || robot.isTargeted || robot.fellInPit || robot.grabbedByPlayer || robot.lockedByPlayer)
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
}