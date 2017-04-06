using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckDoorClosed : StateMachineBehaviour {

	public float 	spawnDelay = 3.0f;

	private float	nextSpawnTime;
	private bool	doorClosed;
	private bool	waitSpawn;

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (stateInfo.normalizedTime > 1.0f) {
			doorClosed = true;
		} else {
			doorClosed = false;
		}

		if (doorClosed && !waitSpawn) {
			waitSpawn = true;
			nextSpawnTime = Time.time + spawnDelay;
		}

		if (doorClosed && Time.time > nextSpawnTime) {
			// doorClosed = false;
			animator.SetTrigger ("OpenDoor");
		}
	}
}
