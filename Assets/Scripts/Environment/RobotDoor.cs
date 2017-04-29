using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotDoor : MonoBehaviour {

	public AudioClip 	doorSlideSound;
	public Robot 		robotPrefab;
	public SlimeRobot 	slimeRobotPrefab;
	public float 		spawnDelay = 0.5f;

	[HideInInspector]
	public bool			isClosed = true;
	[HideInInspector]
	public bool 		spawnEnabled = true;

	private AudioSource source;
	private Animator	animator;
	private bool		spawnSlimeBot = false;

	void Awake() {
		source = GetComponent<AudioSource> ();
		animator = GetComponent<Animator> ();
	}

	public IEnumerator SpawnSlimeBot() {
		spawnSlimeBot = true;
		GameManager.instance.StopAllRobots();
		SoundManager.instance.PlayLevelEndSound ();

		while (SoundManager.instance.globalSFxSource.isPlaying)
			yield return null;

		TriggerDoorOpen ();
	}

	// RobotDoorOpen animation event triggers this co-routine
	IEnumerator SpawnRobots() {
		if (!spawnSlimeBot) {
			while (spawnEnabled && (GameManager.instance.robotCount < GameManager.instance.maxRobots) && (GameManager.instance.robotCount < HUDManager.instance.robotsRemaining)) {
				Robot spawnedRobot = Instantiate<Robot> (robotPrefab, animator.transform.position, Quaternion.identity);
				GameManager.instance.AddRobot (spawnedRobot);
				yield return new WaitForSeconds (spawnDelay);
			}
		} else {
			Instantiate<SlimeRobot> (slimeRobotPrefab, animator.transform.position, Quaternion.identity);
			yield return new WaitForSeconds (spawnDelay);
		}
		TriggerDoorClose ();
	}
		
	//  GameManager invokes this on all doors once the global spawnDelay has elapsed
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
