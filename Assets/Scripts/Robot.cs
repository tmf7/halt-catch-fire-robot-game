using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Robot : Throwable {

	public ParticleSystem firePrefab;
	public ParticleSystem robotBeamPrefab;
	public AudioClip 	repairingSound;
	public AudioClip	finishRepairSound;
	public AudioClip	catchFireSound;

	public AudioClip 	playerGrabbedSound;
	public AudioClip 	robotGrabbedSound;
	public AudioClip	thrownSound;

	public Transform 	target;
	public float 		speed = 2.0f;
	public float 		slowdownDistance = 2.0f;
	public float 		grabHeight = 10.0f;
	public float		damageRate = 9.0f;
	public float		healRate = 10.0f;
	public float		maxHealth = 100.0f;

	// state machine
	[HideInInspector]
	public GameObject	whoGrabbed;
	[HideInInspector]
	public float 		health = 100;

	[HideInInspector]
	public bool onFire {
		get { 
			return fireInstance != null;
		} set { 
			if (!onFire && value) {
				fireInstance = Instantiate<ParticleSystem> (firePrefab, transform.position, Quaternion.identity, transform);
				PlaySingleSoundFx (catchFireSound);
			} else if (onFire) {
				Destroy (fireInstance.gameObject);
			}
		}
	}
		
	public enum RobotStates {
		STATE_FINDBOX,
		STATE_SUICIDAL,
		STATE_HOMICIDAL,
		STATE_ONFIRE,
		STATE_DELIVERING,
		STATE_REPAIRING
	};
	public RobotStates	currentState;
	public RobotStates	oldState;

	// pathing
	private Vector3[] 	path;
	private int 		targetIndex;
	private Vector3 	currentWaypoint;
	private Vector3		targetLastKnownPosition;
	private float 		sqrTargetSlowdownDistance;
	private float 		carryItemDistance;
	private const float	pathUpdateMoveThreshold = 0.25f;
	private const float minWaitTime = 0.2f;
	private const float stoppingThreshold = 0.01f;

	// state machine
	private bool 		justReleased;
	private Throwable 	carriedItem;
	private ParticleSystem fireInstance;
	private CircleCollider2D circleCollider;

	void Start() {
		circleCollider = GetComponent<CircleCollider2D> ();
		carryItemDistance = 2.0f * circleCollider.radius;
		currentState = RobotStates.STATE_FINDBOX;
		oldState = RobotStates.STATE_FINDBOX;
		sqrTargetSlowdownDistance = slowdownDistance * slowdownDistance;
	}

	public void OnPathFound(Vector3[] newPath, bool pathSuccessful) {
		if (pathSuccessful) {
			path = newPath;
			targetIndex = 0;
			currentWaypoint = path [targetIndex];
		}
	}

	void Update() {
		if (grabbed) {
			// TODO: another robot can set the grabbed boolean
			// picking up a grabbed robot keeps the two attached and drops/ungrabs all 
			// picking up a grabbing robot makes it drop its carried item
			if (carriedItem != null) {
				carriedItem.transform.SetParent (null);
				carriedItem = null;
			}

			justReleased = false;
			if (whoGrabbed.tag == "Player") {
				SetHeight (grabHeight);
			} else if (whoGrabbed.tag == "Robot" && robotBeam == null) {
				PlaySingleSoundFx (robotGrabbedSound);	// distressed robot better indicates it's a victim
			} 
			StopMoving ();
		} else if (!grounded && !justReleased) {
			justReleased = true;
			SetHeight (grabHeight);
			rb2D.velocity = new Vector2 (dropForce.x, dropForce.y);
			Throw (0.0f, -1.0f);
			PlaySingleSoundFx (thrownSound);
		}

		if (onFire)
			health -= Time.deltaTime * damageRate;

		if (currentState == RobotStates.STATE_REPAIRING) {
			onFire = false;
			health += Time.deltaTime * healRate;
			if (health > maxHealth) {
				health = maxHealth;
				currentState = RobotStates.STATE_FINDBOX;
				PlaySingleSoundFx (finishRepairSound);
			}
		}

		if (health <= 0) 
			Explode();
	
		if (grounded && !fellInPit) {
			SearchForTarget ();
		} else if (fellInPit && onFire) {
			// make the fire shrink too
			fireInstance.transform.localScale = new Vector3(currentPitfallScale, 1.0f, currentPitfallScale);
		}

		UpdateShadow ();
		UpdateCarriedItemPosition ();
		UpdateRobotBeam ();
	}

	private void UpdateCarriedItemPosition() {
		// check if another robot took this one's carriedItem
		Throwable checkCarry = GetComponentInChildren<Throwable> ();
		if (checkCarry == null)
			carriedItem = null;

		if (carriedItem == null) {
			//StopMoving ();
			currentState = oldState;		// FIXME: this line of logic may not quite work
			return;
		}
		
		float robotMoveAngle = Vector3.Angle (Vector3.right, target.position - transform.position);
		if (robotMoveAngle < 45.0f) {
			carriedItem.transform.localPosition = Vector3.right * carryItemDistance;
		} else if (robotMoveAngle < 90.0f) {
			carriedItem.transform.localPosition = Vector3.up * carryItemDistance;
		} else if (robotMoveAngle < 180.0f) {
			carriedItem.transform.localPosition = Vector3.left * carryItemDistance;
		} else if (robotMoveAngle < 360.0f) {
			carriedItem.transform.localPosition = Vector3.down * carryItemDistance;
		}
	}

	public void SetState(RobotStates newState) {
		currentState = newState;
		StopMoving ();
	}

	public void SetStateRandom() {
		switch (Random.Range (0, 3)) {
			case 0:
				currentState = RobotStates.STATE_FINDBOX;
				break;
			case 1:
				currentState = RobotStates.STATE_SUICIDAL;
				break;
			case 2:
				currentState = RobotStates.STATE_HOMICIDAL;
				break;
		}
		StopMoving ();
	}

	void SearchForTarget() {

		if (currentState == RobotStates.STATE_REPAIRING) {
			StopMoving ();
			return;
		}
			
		if (target != null) {
			FollowPath ();
			return;
		}
	
		// SEARCH for a box to pickup, not just a random box. (meaning do a box2d sweep, then path to a random valid cell if nothing is found) 
		switch (currentState) {
			case RobotStates.STATE_FINDBOX:
				target = GameManager.instance.GetRandomBoxTarget();
				break;
			case RobotStates.STATE_DELIVERING:
				target = GameManager.instance.GetRandomDeliveryTarget ();
				break;
			case RobotStates.STATE_SUICIDAL:
				// change the robot visual
				target = GameManager.instance.GetRandomHazardTarget ();
				break;
			case RobotStates.STATE_HOMICIDAL:
				target = GameManager.instance.GetRandomRobotTarget ();
				// target a random robot to deliver (as if a box), and again, and again
				break;
			case RobotStates.STATE_ONFIRE: 
				// TODO: run around like a crazy person
				break;
		}

		if (target == null) {
			//SetStateRandom ();
			StopMoving();
		} else {	// prevent others from attempting to grab this target (only affects boxes and robots, not delivery points or hazards, see GameManager)
			Throwable targetObj = target.gameObject.GetComponent<Throwable> ();
			if (targetObj != null)
				targetObj.SetClaimant (gameObject);
		}
	}

	void UpdatePath(bool freshStart) {
		if (freshStart || (target.position - targetLastKnownPosition).sqrMagnitude > pathUpdateMoveThreshold) {
			PathRequestManager.RequestPath (transform.position, target.position, OnPathFound);
			targetLastKnownPosition = target.position;
		}
	}

	void FollowPath() {
		UpdatePath (path == null);
		if (path == null)
			return;

		if (transform.position == currentWaypoint) {
			targetIndex++;
			if (targetIndex >= path.Length) {
				StopMoving ();
				return;
			}
			currentWaypoint = path [targetIndex];
		}

		float percentSpeed = 1.0f;
		float sqrRange = (path [path.Length - 1] - transform.position).sqrMagnitude;
		if (sqrRange < sqrTargetSlowdownDistance) {
			percentSpeed = Mathf.Clamp01 (Mathf.Sqrt (sqrRange) / slowdownDistance);
			if (percentSpeed < stoppingThreshold) {
				StopMoving ();
				return;
			}
		}
			
		transform.position = Vector3.MoveTowards (transform.position, currentWaypoint, speed * percentSpeed * Time.deltaTime);
	}

	void StopMoving() {
		path = null;
		targetIndex = 0;
		if (target != null) {
			Throwable targetObj = target.gameObject.GetComponent<Throwable> ();
			if (targetObj != null)
				targetObj.SetClaimant (null);	// isClaimed also becomes false if the claimant dies
			target = null;
		}
	}

	protected override void HitCollision2D(Collision2D collision) {
		if (carriedItem == null) {
			if (currentState == RobotStates.STATE_FINDBOX && collision.collider.tag == "Box") {
				StopMoving ();
				Box hitBox = collision.collider.gameObject.GetComponent<Box> ();
				hitBox.transform.SetParent (transform);
				hitBox.GetComponent<Rigidbody2D> ().constraints = RigidbodyConstraints2D.FreezeRotation;
				carriedItem = hitBox;
				oldState = currentState;
				currentState = RobotStates.STATE_DELIVERING;
				// TODO: once the carriedItem has hit the intended target collider/trigger, 
				// unparent it and change back to previousState (saved whenever changing to DELIVERING)
				// FIXME: ensure that robots stealing boxes doesn't cause multiple parenting (doubtful)
				// but more importantly that the stolen-from robot's carriedItem becomes null and it state returns to oldState (as if delivered)
			} else if (currentState == RobotStates.STATE_HOMICIDAL && collision.collider.tag == "Robot") {
				Robot hitRobot = collision.collider.gameObject.GetComponent<Robot> ();
				hitRobot.transform.SetParent (transform);
				hitRobot.GetComponent<Rigidbody2D> ().constraints = RigidbodyConstraints2D.FreezeRotation;
				carriedItem = hitRobot;
				oldState = currentState;
				currentState = Random.Range (0, 2) == 0 ? RobotStates.STATE_DELIVERING : RobotStates.STATE_SUICIDAL;
			}
			// This GRABBING robot instantiates the beam and parents it to the carriedItem.
			// It then becomes the responsibility of the carriedItem to check if its a child of a robot to decide whether to remove its robotBeam
			if (carriedItem != null)
				carriedItem.ActivateRobotBeam(Instantiate<ParticleSystem> (robotBeamPrefab, carriedItem.transform.position, Quaternion.identity, carriedItem.transform));
		}

		if (collision.collider.tag == "Furnace") {
			if (!onFire)
				onFire = true;
		}

		if (collision.collider.tag == "Crusher")
			Explode ();

		if (collision.collider.tag == "Robot") {
			if (!onFire && collision.collider.GetComponent<Robot> ().onFire)
				onFire = true;
		}
	}

	protected override void HitTrigger2D (Collider2D collider) {
		if (collider.tag == "BoxExit") {
			RandomThrow ();
		}

		if (collider.tag == "HealZone" && currentState != RobotStates.STATE_REPAIRING && health < maxHealth) {
			currentState = RobotStates.STATE_REPAIRING;
			PlaySingleSoundFx (repairingSound);
		}
	}

	// Debug Drawing
	public void OnDrawGizmos() {
		if (path != null) {
			for (int i = targetIndex; i < path.Length; i++) {
				Gizmos.color = Color.black;
				Gizmos.DrawCube (path [i], Vector3.one);

				if (i == targetIndex) {
					Gizmos.DrawLine (transform.position, path [i]);
				} else {
					Gizmos.DrawLine (path [i - 1], path [i]);	
				}
			}
		}
	}

	protected override void OnLanding () {
		base.OnLanding ();
		if (currentState != RobotStates.STATE_REPAIRING && !fellInPit && !onFire)		// bugfix for the landing sound overwriting status change sounds
			PlayRandomSoundFx (landingSounds);
	}
}
