using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckDoorOpen : StateMachineBehaviour {

	public Robot 	robotPrefab;
	public float	doorCloseDelay = 1.0f;
	public bool 	spawnEnabled = true;

	private float	doorCloseTime;
	private bool 	spawnOn;
	private bool	hasSpawned = false;
	private bool	doorOpened;

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		spawnOn = spawnEnabled && (GameManager.instance.robotCount <= GameManager.instance.maxRobots);

		if (stateInfo.normalizedTime > 1.0f) {
			doorOpened = true;
		} else {
			doorOpened = false;
			hasSpawned = false;
		}
			
		if (spawnOn && doorOpened && !hasSpawned) {
			doorCloseTime = Time.time + doorCloseDelay;
			// the next spawnTime should be set after the door is determined to be fully closed
			Robot spawnedRobot = Instantiate<Robot> (robotPrefab, animator.transform.position, Quaternion.identity);
			GameManager.instance.AddRobot (spawnedRobot);
			hasSpawned = true;
		}

		if (spawnOn && doorOpened && Time.time > doorCloseTime) {
			animator.SetTrigger ("CloseDoor");
		}
	}
}
