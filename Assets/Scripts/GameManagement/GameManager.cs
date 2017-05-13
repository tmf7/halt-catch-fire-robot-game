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
	public int 					maxRobots = 3;
	public int					maxBoxes = 20;
	public float 				acceptableSearchRangeSqr = 50.0f;		// stop looking for something closer if currently queried item is within this range
	public bool					levelEnded = false;

	// heierarchy organization
	private Transform 			boxHolder;		
	private Transform			robotHolder;

	private Grid				grid;
	private List<Box> 			allBoxes;
	private List<Robot> 		allRobots;
	private List<RobotDoor>		allDoors;
	private List<Transform>		deliveryPoints;
	private List<Transform> 	hazardPoints;

	// respawn handling
	private int					initialMaxRobots;
	private int 				pendingRobots;

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
		initialMaxRobots = maxRobots;
	}

	void Update() {
		if (!levelEnded) 
			CheckIfLevelOver ();
	}

	private void SpawnInitialRobots() {
		pendingRobots = HUDManager.instance.robotsRemaining;
		foreach (RobotDoor door in allDoors)
			door.TriggerDoorOpen ();
	}

	private void CheckIfLevelOver() {
		if ((HUDManager.instance.allRobotsFired && HUDManager.instance.boxesRemaining <= 0) || HUDManager.instance.isLevelTimeUp) {
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
		grid = GameObject.FindObjectOfType<Grid> ();
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
		levelEnded = false;
		HUDManager.instance.StartLevelTimer ();
		SpawnInitialRobots ();
	}
		
	public int robotBuildCost {
		get {
			return (robotCount > 0 ? (int)(15.0f * Mathf.Log10 ((float)(robotCount + pendingRobots)) + 1.0f) : 1);
		}
	}

	public void ResetMaxRobots () {
		maxRobots = initialMaxRobots;
	}

	public void IncrementMaxRobots() {
		maxRobots++;
		if (maxRobots >= RobotNames.Instance.maxAvailableNames)
			maxRobots = RobotNames.Instance.maxAvailableNames - 1;
		else
			pendingRobots++;

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

	public Transform robotParent {
		get { 
			return robotHolder;
		}
	}

	public Transform boxParent {
		get { 
			return boxHolder;
		}
	}

	public void AddRobot(Robot newRobot) {
		newRobot.transform.SetParent (robotHolder);
		newRobot.SetShadowParent (robotHolder);
		allRobots.Add (newRobot);
		pendingRobots--;
		if (pendingRobots < 0)
			pendingRobots = 0;
	}

	public void Remove(Throwable item) {
		if (item is Box) {
			allBoxes.Remove (item as Box);
		} else if (item is Robot) {
			HUDManager.instance.FireRobot ();
			allRobots.Remove (item as Robot);
		}
	}

	public bool IsHazard (Transform hit) {
		return hazardPoints.Contains (hit);
	}

	public Collider2D IsTouchingHazard (Collider2D collider2D) {
		foreach (Transform hazard in hazardPoints) {
			Collider2D testHit = hazard.GetComponent<Collider2D> ();
			if (collider2D.IsTouching (testHit)) {
				return testHit;
			}
		}
		return null;
	}

	public Collider2D IsTouchingRobot (Collider2D collider2D) {
		foreach (Robot robot in allRobots) {
			Collider2D testHit = robot.GetComponent<Collider2D> ();
			if (collider2D.IsTouching (testHit)) {
				return testHit;
			}
		}
		return null;
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

	public Transform GetRandomHazardTarget() {
		return hazardPoints[Random.Range(0, hazardPoints.Count)];
	}

	public Transform GetRandomRobotTarget(Robot homicidalRobot) {
		if (allRobots.Count <= 0)
			return null;

		Transform robot = null;
		int numTries = 0;
		int index = -1;
		for (/*numTries*/; numTries < maxRobots; numTries++) {
			index = Random.Range(0, allRobots.Count);
			Robot tryBot = allRobots [index];
			if (tryBot != homicidalRobot && !tryBot.isBeingCarried && !tryBot.isTargeted && !tryBot.fellInPit && !tryBot.grabbedByPlayer && !tryBot.lockedByPlayer)
				break;
		}
		if (numTries < maxRobots)
			robot = allRobots [index].transform;

		return robot;
	}

	// FIXME(~): assumes the resulting node can be pathed to
	public Vector3 GetRandomWorldPointTarget(Robot robot) {
		GridNode node = null;
		Vector3 nodePos = Vector3.zero;
		float rangeSqr = 0.0f;
		int row = -1;
		int col = -1;
		int numTries = 0;
		do {
			row = Random.Range (0, grid.GridRows);
			col = Random.Range (0, grid.GridCols);
			node = grid.NodeFromRowCol(row, col);
			nodePos = new Vector3 (node.worldPosition.x, node.worldPosition.y);
			rangeSqr = (nodePos - robot.transform.position).sqrMagnitude;
			numTries++;
		} while (!node.walkable && (rangeSqr < acceptableSearchRangeSqr) && numTries < grid.MaxSize);

		if (numTries >= grid.MaxSize)
			return Vector3.zero;
		return nodePos;
	}
}