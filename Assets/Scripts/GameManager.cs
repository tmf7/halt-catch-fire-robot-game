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
	public int 					maxRobots = 10;
	public int					maxBoxes = 20;

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

		allBoxes = new List<Box>();
		allRobots = new List<Robot> ();
		boxHolder = new GameObject ("Boxes").transform;
		robotHolder = new GameObject ("Robots").transform;
	}

	void Start() {

		deliveryPoints = new List<Transform> ();
		hazardPoints = new List<Transform> ();

		GameObject[] exits = GameObject.FindGameObjectsWithTag ("BoxExit");
		GameObject[] conveyors = GameObject.FindGameObjectsWithTag ("Conveyor");
		foreach (GameObject exit in exits)
			deliveryPoints.Add (exit.transform);
		foreach (GameObject conveyor in conveyors)
			deliveryPoints.Add (conveyor.transform);

		GameObject[] crushers = GameObject.FindGameObjectsWithTag ("Crusher");
		GameObject[] furnaces = GameObject.FindGameObjectsWithTag ("Furnace");
		GameObject[] pits = GameObject.FindGameObjectsWithTag ("Pit");
		foreach (GameObject crusher in crushers)
			hazardPoints.Add (crusher.transform);
		foreach (GameObject furnace in furnaces)
			hazardPoints.Add (furnace.transform);
		foreach (GameObject pit in pits)
			hazardPoints.Add (pit.transform);
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

	public Transform GetRandomBoxTarget() {
		if (allBoxes.Count > 0) {
			Box targetBox = allBoxes [Random.Range (0, allBoxes.Count)];
			if (!targetBox.isClaimed) {
				return targetBox.transform;	
			} else {
				return null;	// try to get a different target next frame
			}
		} else {
			return null;
		}
	}

	public Transform GetRandomRobotTarget () {
		if (allRobots.Count > 0) {
			Robot targetRobot = allRobots [Random.Range (0, allRobots.Count)];
			if (targetRobot.isClaimed) {
				return null;	// try to get a different target next frame
			} else {
				return targetRobot.transform;
			}
		} else {
			return null;
		}
	}

	public Transform GetRandomDeliveryTarget () {
		return deliveryPoints.Count > 0 ? deliveryPoints [Random.Range (0, deliveryPoints.Count)] : null;
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

void Update() {
	if(playersTurn || enemiesMoving || doingSetup)
		return;
	
	//Start moving enemies.
	StartCoroutine (MoveEnemies ());
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