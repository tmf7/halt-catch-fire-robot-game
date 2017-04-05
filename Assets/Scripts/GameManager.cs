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
	public static GameManager instance = null;

	// TODO: these hazards are already in the scene, not spawned, so just drag their object instances into the GameManager script inputs
//	public GameObject furnace;	
//	public GameObject pit;
//	public GameObject fence;
//	public GameObject crusher;

	// heierarchy organization
	public Transform 		boxHolder;		
	public Transform		robotHolder;

	private List<Box> 		allBoxes;
	private List<Robot> 	allRobots;		// some robots alreay exist in the scene, grab those and add them to the list, then add any that are spawned
/*
	[HideInInspector] public bool playersTurn = true;		//Boolean to check if it's players turn, hidden in inspector but public.

	private Text levelText;									//Text to display current level number.
	private GameObject levelImage;							//Image to block out level as levels are being set up, background for levelText.
	private int level = 1;									//Current level number, expressed in game as "Day 1".
	private List<Enemy> enemies;							//List of all Enemy units, used to issue them move commands.
	private bool enemiesMoving;								//Boolean to check if enemies are moving.
	private bool doingSetup = true;							//Boolean to check if we're setting up board, prevent Player from moving during setup.
*/

	void Awake() {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
		allBoxes = new List<Box>();
		allRobots = new List<Robot> ();
		/*		
		enemies = new List<Enemy>();
		
		//Get a component reference to the attached BoardManager script
		boardScript = GetComponent<BoardManager>();
		InitGame();
*/
	}

	public void AddBox(Box newBox) {
		newBox.transform.SetParent (boxHolder);
		newBox.SetShadowParent (boxHolder);
		// also set the shadows parent
		allBoxes.Add (newBox);
	}

	public void AddRobot(Robot newRobot) {
		newRobot.transform.SetParent (robotHolder);
		allRobots.Add (newRobot);
	}

	public void RemoveBox(Box oldBox) {
		allBoxes.Remove (oldBox);
	}

	public void RemoveRobot(Robot oldRobot) {
		allRobots.Remove (oldRobot);
	}

	public Transform GetRandomBoxTarget() {
		return allBoxes.Count > 0 ? allBoxes [Random.Range (0, allBoxes.Count - 1)].transform : null;
	}

	public Transform GetRandomRobotTarget () {
		return allRobots.Count > 0 ? allRobots [Random.Range (0, allRobots.Count - 1)].transform : null;
	}

/*
    //this is called only once, and the paramter tell it to be called only after the scene was loaded
    //(otherwise, our Scene Load callback would be called the very first load, and we don't want that)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static public void CallbackInitialization()
    {
        //register the callback to be called everytime the scene is loaded
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        instance.level++;
        instance.InitGame();
    }

	
	//Initializes the game for each level.
	void InitGame()
	{
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
	
	void HideLevelImage()
	{
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
	
	public void AddEnemyToList(Enemy script) {
		enemies.Add(script);
	}

	public void GameOver() {
		//Set levelText to display number of levels passed and game over message
		levelText.text = "After " + level + " days, you starved.";
		
		//Enable black background image gameObject.
		levelImage.SetActive(true);
		
		//Disable this GameManager.
		enabled = false;
	}
	//Coroutine to move enemies in sequence.
	IEnumerator MoveEnemies()
	{
		enemiesMoving = true;

		yield return new WaitForSeconds(turnDelay);
		
		if (enemies.Count == 0) {
			yield return new WaitForSeconds(turnDelay);
		}
		
		//Loop through List of Enemy objects.
		for (int i = 0; i < enemies.Count; i++) {
			enemies[i].MoveEnemy ();
			yield return new WaitForSeconds(enemies[i].moveTime);
		}

		playersTurn = true;
		enemiesMoving = false;
	}
*/
}