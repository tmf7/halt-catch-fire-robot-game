using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotSpawner : MonoBehaviour {

	public Robot 	robotPrefab;
	public float	doorCloseDelay = 1.0f;
	public float 	spawnDelay = 3.0f;		// continuous spawning
	public bool 	spawnOn;				// instanced spawning

//	[HideInInspector]
	public bool 	doorOpened;				// control passed from the animation script
//	[HideInInspector]
	public bool		doorClosed;				// control passed from the animation script

	private Animator animator;
	private float	nextSpawnTime;
	private float	doorCloseTime;
	private bool	hasSpawned = false;

	// TODO: add a script to the edgedoor to cause the animator to stop when the dooropen animation exits
	// and set a bool visible in this script to spawn a robot

	// TODO: if at maxRobots, then set a trigger to stop the edgedoor animator when the doorclose animation exits (that behavior script #2)

	// TODO: if not at maxRobots, then wait spawnDelay seconds before starting the dooropen animation

	void Start () {
		animator = GetComponent<Animator> ();
		nextSpawnTime = Time.time + spawnDelay;
	}

	// DEBUG: the door starts closed, so the doorCloseTime is not initialized
	void Update () {
		// TODO: allow external control of spawnOn (ie this && that, here)
		spawnOn = GameManager.instance.robotCount <= GameManager.instance.maxRobots;

		if (spawnOn && doorOpened && !hasSpawned) {
			doorCloseTime = Time.time + doorCloseDelay;
			// the next spawnTime should be set after the door is determined to be fully closed
			Robot spawnedRobot = Instantiate<Robot> (robotPrefab, transform.position, Quaternion.identity);
			GameManager.instance.AddRobot (spawnedRobot);
			hasSpawned = true;
		}

		if (doorOpened && Time.time > doorCloseTime) {
			doorOpened = false;
			animator.SetTrigger ("CloseDoor");
		}

		if (doorClosed && Time.time > nextSpawnTime) {
			nextSpawnTime = Time.time + spawnDelay;
			animator.SetTrigger ("OpenDoor");
			hasSpawned = false;
			doorClosed = false;
		}
	}
}
