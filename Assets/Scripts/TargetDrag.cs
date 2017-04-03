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
				joint.distance = 0.0f;//collider.bounds.extents.y;

				// allow the robot to swing on the hinge and spin in flight
				collider.attachedRigidbody.constraints = RigidbodyConstraints2D.None;
			} 

		} else if (joint != null && (Input.GetAxis("Mouse X") != 0.0f || Input.GetAxis("Mouse Y") != 0.0f)) {
			
			joint.connectedAnchor = worldPosition + Vector3.up * collider.bounds.extents.y;
			joint.distance = collider.bounds.size.y;
		} 
			
		if (!Input.GetMouseButton (0)) {
			if (grabbedRobot != null) {
				Destroy (joint);
				joint = null;
				grabbedRobot.grabbed = false;
				grabbedRobot = null;
			}
		}
    }
}
