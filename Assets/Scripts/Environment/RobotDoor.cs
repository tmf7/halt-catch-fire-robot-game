using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotDoor : MonoBehaviour {

	public AudioClip doorSlideSound;
	public Robot 	robotPrefab;
	public float 	spawnDelay = 1.0f;
	public float	doorCloseDelay = 2.0f;
	public bool 	spawnEnabled = true;

	private AudioSource source;
	private Animator animator;
	private bool	doorClosed = true;
//	private bool 	spawnOn;
//	private bool	openTriggerSet = false;

	void Awake() {
		source = GetComponent<AudioSource> ();	// FIXME: make the GameManager or SoundManager's buttonClickSource (rename globalSfxSource) make the door open sound ONCE for each spawn cycle (instead of ALL making the sound)
		animator = GetComponent<Animator> ();
	}


	// TODO: don't use Update(), instead use a public function called by GameManager when the global spawn delay has elapsed
	// and it should be a co-routine that continues to loop until spawnOn is false
	// GameManager.intance.nextSpawnTime = Time.time + GameManager.instance.spawnDelay;
	// then "New Recruits In: " + (nextSpawnTime - Time.time);
	// AND that value is only set once ALL doors have doorClosed == true; IE: foreach (RobotDoor door in allDoors) {allClosed = door.isClosed;} if (allClosed) nextSpawnTime...

	// can an animation event directly start a co-routine?? if not just make a helper function (seems it can, but it might not function as a proper co-routine)
	IEnumerator SpawnRobots() {
		while (spawnEnabled && (GameManager.instance.robotCount < GameManager.instance.maxRobots)) {
			Robot spawnedRobot = Instantiate<Robot> (robotPrefab, animator.transform.position, Quaternion.identity);
			GameManager.instance.AddRobot (spawnedRobot);
			yield return new WaitForSeconds (spawnDelay);
		}
		Invoke ("TriggerDoorClose", doorCloseDelay);
	}
		
/*
	void Update() {
		spawnOn = spawnEnabled && (GameManager.instance.robotCount < GameManager.instance.maxRobots);
		if (spawnOn && doorClosed && !openTriggerSet) {	// once the door is closed, issue exactly one TriggerDoorOpen command after spawnDelay
			openTriggerSet = true;
			Invoke ("TriggerDoorOpen", spawnDelay);
		}
	}

	// the RobotDoor open animation has an animation event that calls this at the end
	private void SpawnRobot() {
		doorClosed = false;
		openTriggerSet = false;
		Robot spawnedRobot = Instantiate<Robot> (robotPrefab, animator.transform.position, Quaternion.identity);
		GameManager.instance.AddRobot (spawnedRobot);
		Invoke ("TriggerDoorClose", doorCloseDelay);		// TODO: only trigger this once spawnOn is false && !doorClosed
	}
*/

	public bool isClosed {
		get { 
			return doorClosed;
		}
	}

	// TODO: GameManager should invoke this on all doors once the global spawnDelay has elapsed
	public void TriggerDoorOpen() {
		animator.SetTrigger ("OpenDoor");
	}

	private void TriggerDoorClose() {
		animator.SetTrigger ("CloseDoor");
	}

	private void SetDoorClosed() {
		doorClosed = true;
	}

	private void PlaySlideSound() {
		source.clip = doorSlideSound;
		source.Play ();
	}

}
