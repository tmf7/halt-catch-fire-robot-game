using UnityEngine;
using UnityEngine.UI;


public class RobotGrabber : MonoBehaviour {

	public LayerMask			grabbleMask;
	public float				grabRadius = 10.0f;
	public float				mouseJointDistance = 0.1f;
	public float				touchJointDistance = 1.0f;
	public float				forceMultiplier = 2.0f;

	private Robot 				grabbedRobot;
    private DistanceJoint2D 	joint;
	private SpriteRenderer		spriteRenderer;

	void Start () {
		spriteRenderer = GetComponent<SpriteRenderer> ();

		// UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
		// UNITY_STANDALONE || UNITY_WEBPLAYER

		#if UNITY_EDITOR
			spriteRenderer.enabled = true;
		#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
			spriteRenderer.enabled = false;
		#endif
	}

    void Update() {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
		worldPosition.z = 0.0f;

		#if UNITY_EDITOR

		if (Time.timeScale == 0.0f || worldPosition.y > 7.0f) {		// FIXME: magic number specific to the current y-position of the HUD interface
			Cursor.visible = true;
			spriteRenderer.enabled = false;
		} else {
			Cursor.visible = false;
			spriteRenderer.enabled = true;
		}

		if (!Cursor.visible)
			transform.position = worldPosition;		// robotGrabber follows the mouse 1:1
		
		#endif

        if (Input.GetMouseButtonDown(0)) {
			if (joint == null) {

				// find the closest robot within the given radius of the click, if any
				grabbedRobot = null;
				int closestHitIndex = -1;
				Collider2D[] hits = Physics2D.OverlapCircleAll(worldPosition, grabRadius, grabbleMask);
				float closestRobotRangeSqr = float.MaxValue;

				for (int i = 0; i < hits.Length; i++) {
					if (hits[i].tag  == "Robot") {
						float rangeSqr = (hits[i].transform.position - worldPosition).sqrMagnitude;
						if (rangeSqr < closestRobotRangeSqr) {
							closestRobotRangeSqr = rangeSqr;
							closestHitIndex = i;
						}
					}
				}

				if (closestHitIndex == -1)
					return;

				Collider2D closestHit = hits [closestHitIndex];
				grabbedRobot = closestHit.GetComponent<Robot> ();
/*
				RaycastHit2D rayHit = Physics2D.GetRayIntersection (Camera.main.ScreenPointToRay (Input.mousePosition));

				hit = rayHit.collider;
				if (!hit || hit.tag != "Robot")
					return;
  
				grabbedRobot = hit.gameObject.GetComponent<Robot> ();
*/

				if (grabbedRobot.GetState() == Robot.RobotStates.STATE_REPAIRING) {
					grabbedRobot = null;
					return;
				}

				grabbedRobot.grabbedByPlayer = true;
				grabbedRobot.PlaySingleSoundFx (grabbedRobot.playerGrabbedSound);

				// create a hinge on the robot sprite at its top-center for a cleaner effect
				joint = closestHit.gameObject.AddComponent<DistanceJoint2D> ();
				joint.autoConfigureConnectedAnchor = false;
				joint.autoConfigureDistance = false;
				joint.enableCollision = true;
				joint.anchor = Vector2.up * closestHit.bounds.extents.y;	// put the joint in robot local space slightly above its head
				joint.connectedAnchor = worldPosition;

				#if UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
					joint.distance = touchJointDistance;
				#else
					joint.distance = mouseJointDistance;
				#endif

				// allow the robot to swing on the hinge and spin in flight
				closestHit.attachedRigidbody.constraints = RigidbodyConstraints2D.None;
			} 

		} else if (joint != null && (Input.GetAxis("Mouse X") != 0.0f || Input.GetAxis("Mouse Y") != 0.0f)) {
			joint.connectedAnchor = worldPosition;
		} 

		if (!Input.GetMouseButton (0)) {
			if (grabbedRobot != null) {
				Vector3 dropForce = new Vector3(joint.connectedAnchor.x, joint.connectedAnchor.y) - grabbedRobot.transform.TransformPoint(new Vector3(joint.anchor.x, joint.anchor.y));
				if (dropForce.sqrMagnitude <= (joint.distance * joint.distance))
					dropForce = Vector3.zero;
					
				grabbedRobot.dropForce = forceMultiplier * dropForce;
				Destroy (joint);
				joint = null;
				grabbedRobot.grabbedByPlayer = false;
				grabbedRobot = null;
			}
		}
    }
}
