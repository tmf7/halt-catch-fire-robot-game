using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotDoor : MonoBehaviour {

	public AudioClip doorSlideSound;
	public Robot 	robotPrefab;
	public float 	spawnDelay = 0.5f;

	[HideInInspector]
	public bool	isClosed = true;
	[HideInInspector]
	public bool 	spawnEnabled = true;

	private AudioSource source;
	private Animator animator;

	void Awake() {
		source = GetComponent<AudioSource> ();	// FIXME: make the GameManager or SoundManager's buttonClickSource (rename globalSfxSource) make the door open sound ONCE for each spawn cycle (instead of ALL making the sound)
		animator = GetComponent<Animator> ();
	}

	// RobotDoorOpen animation event triggers this co-routine
	IEnumerator SpawnRobots() {
		while (spawnEnabled && (GameManager.instance.robotCount < GameManager.instance.maxRobots) && !RobotNames.Instance.atMaxNames) {
			Robot spawnedRobot = Instantiate<Robot> (robotPrefab, animator.transform.position, Quaternion.identity);
			GameManager.instance.AddRobot (spawnedRobot);
			yield return new WaitForSeconds (spawnDelay);
		}
		TriggerDoorClose ();
	}
		
	//  GameManager should invokes this on all doors once the global spawnDelay has elapsed
	public void TriggerDoorOpen() {
		isClosed = false;
		animator.SetTrigger ("OpenDoor");
	}

	private void TriggerDoorClose() {
		animator.SetTrigger ("CloseDoor");
	}

	public void SetDoorClosed() {
		isClosed = true;
	}

	private void PlaySlideSound() {
		source.clip = doorSlideSound;
		source.Play ();
	}
}
