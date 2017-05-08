using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Furnace : MonoBehaviour {

	public float burnRobotsDelay = 3.0f;

	private float burnRobotsTime;

	void Awake () {
		burnRobotsTime = Time.time + burnRobotsDelay;
	}

	void Update () {
		if (Time.time > burnRobotsTime)
			burnRobotsTime = Time.time + burnRobotsDelay;
	}

	void OnCollisionEnter2D (Collision2D collision) {
		Robot robotToBurn = collision.collider.GetComponent<Robot> ();
		if (robotToBurn != null)
			robotToBurn.onFire = true;
	}
		
	void OnCollisionStay2D (Collision2D collision) {
		if (Time.time > burnRobotsTime) {
			Robot robotToBurn = collision.collider.GetComponent<Robot> ();
			if (robotToBurn != null)
				robotToBurn.onFire = true;
		}
	}
}
