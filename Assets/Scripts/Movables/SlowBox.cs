using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowBox : MonoBehaviour {

	public float 		freakoutDuration = 1.0f;

	private Animator 	animator;
	private Box 		box;
	private float 		electroExplodeTime;
	private bool 		freakingOut = false;

	void Awake () {
		animator = GetComponent<Animator> ();
		box = GetComponent<Box> ();
	}
	
	void Update () {
		Robot carrier = box.GetCarrier ();
		if (!freakingOut && carrier != null && carrier.currentState != Robot.RobotStates.STATE_SUICIDAL) {
			electroExplodeTime = Time.time + freakoutDuration;
			freakingOut = true;
			animator.SetTrigger ("Freakout");
		}

		if (freakingOut) {
			if (carrier == null || carrier.currentState == Robot.RobotStates.STATE_SUICIDAL) {
				freakingOut = false;
				animator.SetTrigger ("Calmdown");
			}

			if (Robot.isHalted)
				electroExplodeTime += Time.deltaTime;
		}

		if (freakingOut && Time.time > electroExplodeTime)
			box.Explode ();
	}
}
