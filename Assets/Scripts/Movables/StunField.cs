using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunField : MonoBehaviour {

	void OnTriggerStay2D (Collider2D hitCollider) {
		Robot robot = hitCollider.GetComponent<Robot> ();
		if (robot != null) {
			robot.StopMoving ();
			robot.StartCoroutine (robot.Freakout ());
			if (RobotGrabber.instance.currentGrabbedRobot == robot) {
				RobotGrabber.instance.ReleaseRobot ();
			}
		}
	}

}
