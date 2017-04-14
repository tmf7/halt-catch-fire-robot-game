using UnityEngine;


public class RobotGrabber : MonoBehaviour {

	private Robot 				grabbedRobot;
    private DistanceJoint2D 	joint;
	private Collider2D 			hit;

    void Update() {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
		worldPosition.z = 0.0f;

        if (Input.GetMouseButtonDown(0)) {
			if (joint == null) {
				RaycastHit2D rayHit = Physics2D.GetRayIntersection (Camera.main.ScreenPointToRay (Input.mousePosition));

				hit = rayHit.collider;
				if (!hit || hit.tag != "Robot")
					return;
  
				grabbedRobot = hit.gameObject.GetComponent<Robot> ();
				if (grabbedRobot.GetState() == Robot.RobotStates.STATE_REPAIRING) {
					grabbedRobot = null;
					return;
				}

				grabbedRobot.grabbedByPlayer = true;
				grabbedRobot.PlaySingleSoundFx (grabbedRobot.playerGrabbedSound);

				// create a hinge on the robot sprite at its top-center for a cleaner effect
				joint = hit.gameObject.AddComponent<DistanceJoint2D> ();
				joint.autoConfigureConnectedAnchor = false;
				joint.autoConfigureDistance = false;
				joint.enableCollision = true;
				joint.anchor = Vector2.up * hit.bounds.extents.y;	// put the joint in robot local space slightly above its head
				joint.connectedAnchor = worldPosition;
				joint.distance = 0.1f;

				// allow the robot to swing on the hinge and spin in flight
				hit.attachedRigidbody.constraints = RigidbodyConstraints2D.None;
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
				grabbedRobot.grabbedByPlayer = false;
				grabbedRobot = null;
			}
		}
    }
}
