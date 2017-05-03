using UnityEngine;
using UnityEngine.UI;


public class RobotGrabber : MonoBehaviour {

	public static RobotGrabber 	instance = null;

	public LayerMask			grabbleMask;
	public LayerMask 			grabbedRobotMask;
	public float				grabRadius = 10.0f;
	public float				mouseJointDistance = 0.1f;
	public float				touchJointDistance = 1.0f;
	public float				forceMultiplier = 2.0f;

	private Robot 				grabbedRobot;
    private DistanceJoint2D 	joint;
	private SpriteRenderer		spriteRenderer;
	private bool				secondClickOnRobot = false;

	public Robot currentGrabbedRobot {
		get { 
			return grabbedRobot;
		}
	}

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
	}

	void Start () {
		spriteRenderer = GetComponent<SpriteRenderer> ();

		#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER
			spriteRenderer.enabled = true;
		#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
			spriteRenderer.enabled = false;
		#endif
	}

    void Update() {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
		worldPosition.z = 0.0f;

		#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER

		if (Time.timeScale == 0.0f || worldPosition.y > 7.0f) {		// FIXME: magic number specific to the current y-position of the HUD interface
			Cursor.visible = true;
			spriteRenderer.enabled = false;
		} else {
			Cursor.visible = false;
			spriteRenderer.enabled = true;
		}

		// robotGrabber follows the mouse 1:1
		// unless the player has clicked on a robot once
		if (!Cursor.visible && (grabbedRobot == null || secondClickOnRobot)) {
			transform.position = worldPosition;		
		} else if (grabbedRobot != null) {
			transform.position = grabbedRobot.transform.position + Vector3.up * joint.distance;		// FIXME: don't shake the grabber-sprite if the robot is shaking
			Cursor.visible = true;
		}
		
		#endif

		// first click LOCKS the robot on the ground (ie NOT HOVER via Robot.grabbed bool)
		// SET RobotGrabber SPRITE position DIRECTLY over grabbedRobot, and enable the cursor to manipulate the SLIDER, or draw a PATH

		// click and release, followed by second click and release logic:
		// if the user RELEASES the mouse the robot WILL UNLOCK and  GRAB to hover in place (remains the activly grabbedRobot)
		// if the user CLICKS AGAIN, start MOVING the RobotGrabber SPRITE again, and throw the robot via its joint as normal

		// click and drag, followed by single release logic:
		// if the user instead HOLDS AND DRAGS the mouse, then the robot STAYS LOCKED and starts getting a NEW PATH 
		// if the user RELEASES the mouse, UNLOCK the robot to follow the new path, and start MOVING the RobotGrabber SPRITE again as normal

		// input release
		if (Input.GetMouseButtonDown (0)) {
			if (joint == null) {

				// find the closest robot within the given radius of the click, if any
				grabbedRobot = null;
				int closestHitIndex = -1;
				Collider2D[] hits = Physics2D.OverlapCircleAll (worldPosition, grabRadius, grabbleMask);
				float closestRobotRangeSqr = float.MaxValue;

				for (int i = 0; i < hits.Length; i++) {
					if (hits [i].tag == "Robot") {
						float rangeSqr = (hits [i].transform.position - worldPosition).sqrMagnitude;
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
				grabbedRobot.lockedByPlayer = true;
				grabbedRobot.gameObject.layer = Mathf.RoundToInt(Mathf.Log(grabbedRobotMask.value, 2.0f));
				grabbedRobot.PlaySingleSoundFx (grabbedRobot.playerGrabbedSound);

				// create a joint on the robot sprite
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
			} else {
				Collider2D hit = Physics2D.OverlapCircle (worldPosition, grabRadius, grabbedRobotMask);
				secondClickOnRobot = hit != null;
			}

		}

		// dragging the input
		if (joint != null && (Input.GetAxis ("Mouse X") != 0.0f || Input.GetAxis ("Mouse Y") != 0.0f)) {
			if (secondClickOnRobot)
				joint.connectedAnchor = worldPosition;
			else
				grabbedRobot.TryAddPathPoint (worldPosition);
		}
			
		// input click
		if (!Input.GetMouseButton (0)) {
			if (grabbedRobot != null && grabbedRobot.lockedByPlayer) {
				grabbedRobot.lockedByPlayer = false;
				grabbedRobot.grabbedByPlayer = true;
				grabbedRobot.FinishDrawingPath ();
				return;
			}				

			if (grabbedRobot != null && grabbedRobot.grabbedByPlayer) {
				grabbedRobot.gameObject.layer = Mathf.RoundToInt(Mathf.Log (grabbleMask.value, 2.0f));
				Vector3 dropForce = new Vector3(joint.connectedAnchor.x, joint.connectedAnchor.y) - grabbedRobot.transform.TransformPoint(new Vector3(joint.anchor.x, joint.anchor.y));
				if (dropForce.sqrMagnitude <= 2.0f * (joint.distance * joint.distance))
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
