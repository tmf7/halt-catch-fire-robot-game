using UnityEngine;


public class RobotGrabber : MonoBehaviour {

	private Robot 				grabbedRobot;
    private DistanceJoint2D 	joint;
	private Collider2D 			collider;

    void Update() {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
		worldPosition.z = 0.0f;

        if (Input.GetMouseButtonDown(0)) {
			if (joint == null) {
				RaycastHit2D rayHit = Physics2D.GetRayIntersection (Camera.main.ScreenPointToRay (Input.mousePosition));

				collider = rayHit.collider;
				if (!collider || collider.tag != "Robot")
					return;
  
				// stop the robot from pathfinding/following while grabbed
				grabbedRobot = collider.gameObject.GetComponent<Robot> ();
				if (grabbedRobot.currentState == Robot.RobotStates.STATE_REPAIRING) {
					grabbedRobot = null;
					return;
				}

				grabbedRobot.grabbed = true;
				grabbedRobot.whoGrabbed = gameObject;

				// create a hinge on the robot sprite at its top-center for a cleaner effect
				joint = collider.gameObject.AddComponent<DistanceJoint2D> ();
				joint.autoConfigureConnectedAnchor = false;
				joint.autoConfigureDistance = false;
				joint.enableCollision = true;
				joint.anchor = Vector3.up * collider.bounds.extents.y;	// put the joint in robot local space slightly above its head
				joint.connectedAnchor = worldPosition;
				joint.distance = 0.1f;

				// allow the robot to swing on the hinge and spin in flight
				collider.attachedRigidbody.constraints = RigidbodyConstraints2D.None;
			} 

		} else if (joint != null && (Input.GetAxis("Mouse X") != 0.0f || Input.GetAxis("Mouse Y") != 0.0f)) {
			joint.connectedAnchor = worldPosition;
		} 
			
		if (!Input.GetMouseButton (0)) {
			if (grabbedRobot != null) {
				Vector3 dropForce = new Vector3(joint.connectedAnchor.x, joint.connectedAnchor.y)- grabbedRobot.transform.TransformPoint(new Vector2(joint.anchor.x, joint.anchor.y));
				grabbedRobot.dropForce = dropForce;
				Destroy (joint);
				joint = null;
				grabbedRobot.grabbed = false;
				grabbedRobot = null;
			}
		}
    }
}
