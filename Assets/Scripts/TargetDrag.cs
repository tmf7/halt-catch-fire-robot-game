using UnityEngine;


public class TargetDrag : MonoBehaviour {

	private Robot grabbedRobot;
    private DistanceJoint2D joint;
	private Collider2D collider;

    void Update() {

        var mousePos = Input.mousePosition;
    //    mousePos.z = 10;

        var worldPosition = Camera.main.ScreenToWorldPoint(mousePos);

        if (Input.GetMouseButtonDown(0)) {

			if (joint == null) {
				RaycastHit2D rayHit = Physics2D.GetRayIntersection (Camera.main.ScreenPointToRay (Input.mousePosition));


				collider = rayHit.collider;
				if (!collider || !collider.attachedRigidbody || collider.gameObject.tag != "Robot")
					return;
  
				// stop the robot from pathfinding/following while grabbed
				grabbedRobot = collider.gameObject.GetComponent<Robot> ();
				grabbedRobot.grabbed = true;

				// create a hinge on the robot sprite at its top-center for a cleaner effect
				joint = collider.gameObject.AddComponent<DistanceJoint2D> ();
				joint.anchor = grabbedRobot.transform.InverseTransformPoint (grabbedRobot.transform.position) + Vector3.up * collider.bounds.extents.y;	// put the joint in robot local space slightly above its head
				joint.connectedAnchor = worldPosition + Vector3.up * collider.bounds.extents.y;
			//	joint.maxDistanceOnly = true;
				joint.distance = collider.bounds.size.y;

				// allow the robot to swing on the hinge and spin in flight
				collider.attachedRigidbody.constraints = RigidbodyConstraints2D.None;
			} 

		} else if (joint != null && (Input.GetAxis("Mouse X") != 0.0f || Input.GetAxis("Mouse Y") != 0.0f)) {

			print ("JOINT MOVED");
			joint.connectedAnchor = worldPosition + Vector3.up * collider.bounds.extents.y;
			joint.distance = collider.bounds.size.y;
			// FIXME: moving the transform directly isn't quite the solution because it ignores physics/rigidbody reaction to a motion
			//grabbedRobot.transform.position = new Vector3 (worldPosition.x, worldPosition.y, 0.0f);
		} 

		if (Input.GetMouseButtonUp (0)) {
			if (grabbedRobot != null) {
				Destroy (joint);
				joint = null;
				grabbedRobot.grabbed = false;
			}
		}
    }
}
