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
		// check who the carrier is, if the carrier isn't suicidal
		// then start looping the freakout animation for a set duration
		// if that duration passes without being dropped/carried by suicidal again
		// explode with point effector (and electrical explosion animation spawn)
		// delivering this box is worth 50

		Robot carrier = box.GetCarrier ();
		if (!freakingOut && carrier != null && carrier.currentState != Robot.RobotStates.STATE_SUICIDAL) {
			electroExplodeTime = Time.time + freakoutDuration;
			freakingOut = true;
			animator.SetTrigger ("Freakout");
		}

		if (freakingOut && (carrier == null || carrier.currentState == Robot.RobotStates.STATE_SUICIDAL)) {
			freakingOut = false;
			animator.SetTrigger ("Calmdown");
		}

		if (freakingOut && Time.time > electroExplodeTime)
			box.Explode ();
	}
}
