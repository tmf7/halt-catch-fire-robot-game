using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotDoor : MonoBehaviour {

	public AudioClip doorSlideSound;
	public Robot 	robotPrefab;
	public float 	spawnDelay = 3.0f;
	public float	doorCloseDelay = 1.0f;
	public bool 	spawnEnabled = true;

	private AudioSource source;
	private Animator animator;
	private bool	doorClosed = true;
	private bool 	spawnOn;
	private bool	openTriggerSet = false;

	void Awake() {
		source = GetComponent<AudioSource> ();
		animator = GetComponent<Animator> ();
	}

	void Update() {		 // too many Invoke calls put on the stack, only put one at a time
		spawnOn = spawnEnabled && (GameManager.instance.robotCount < GameManager.instance.maxRobots);
		if (spawnOn && doorClosed && !openTriggerSet) {
			openTriggerSet = true;
			Invoke ("TriggerDoorOpen", spawnDelay);
		}
	}
		
	private void PlaySlideSound() {
		source.clip = doorSlideSound;
		source.Play ();
	}

	private void SpawnRobot() {
		doorClosed = false;
		openTriggerSet = false;
		Robot spawnedRobot = Instantiate<Robot> (robotPrefab, animator.transform.position, Quaternion.identity);
		GameManager.instance.AddRobot (spawnedRobot);
		Invoke ("TriggerDoorClose", doorCloseDelay);
	}

	private void TriggerDoorOpen() {
		animator.SetTrigger ("OpenDoor");
	}

	private void TriggerDoorClose() {
		animator.SetTrigger ("CloseDoor");
	}

	private void SetDoorClosed() {
		doorClosed = true;
	}
}
