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
	private RobotStates	currentState;

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
		SetState (RobotStates.STATE_FINDBOX);
		sqrTargetSlowdownDistance = slowdownDistance * slowdownDistance;
	}

	void Update() {

		if (Input.GetKey (KeyCode.Space))
			SetStateRandom ();

		if (!CheckGrabbedByPlayer () && !grounded && !justReleased) {
			justReleased = true;
			SetHeight (grabHeight);
			rb2D.drag = 0.0f;
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
		UpdateCarriedItem ();
	}

	private bool CheckGrabbedByPlayer() {
		if (grabbedByPlayer) {
			if (isCarryingItem)
				DropItem ();
			else
				StopMoving ();
			
			justReleased = false;
			SetHeight (grabHeight);
				
			return true;
		}
		return false;
	}

	// carriedItem must not be null to call this
	private void SetupCarryRange() {
		float carriedWidth = circleCollider.radius;
		float carriedHeight = circleCollider.radius;
		float roomForJesus = 0.2f;

		if (carriedItem is Box) {
			Vector3 boxExtents = carriedItem.GetComponent<BoxCollider2D> ().bounds.extents;
			carriedWidth = boxExtents.x + roomForJesus;
			carriedHeight = boxExtents.y + roomForJesus;
		} else {	// its a robot
			float robotRadius = carriedItem.GetComponent<CircleCollider2D> ().radius;
			carriedWidth = robotRadius + roomForJesus;
			carriedHeight = robotRadius + roomForJesus;
		}
		carryItemDistance = circleCollider.radius + (carriedWidth > carriedHeight ? carriedWidth : carriedWidth);
	}

	// FIXME: this function may be interfering with releaseing on the conveyor belts from time to time
	// causeing the robot to push into the center of the BoxExit oddly
	private void UpdateCarriedItem() {
		if (!isCarryingItem || target == null)
			return;

		Vector3 carryDir = (target.position - transform.position).normalized;
		carriedItem.transform.position = transform.position + carryDir * carryItemDistance;
	}

	public RobotStates GetState() {
		return currentState;
	}

	public void SetState(RobotStates newState) {
		currentState = newState;
		StopMoving ();
	}

	public void SetStateRandom() {
		switch (Random.Range (0, 3)) {
			case 0:
				SetState(RobotStates.STATE_FINDBOX);
				break;
			case 1:
				SetState(RobotStates.STATE_SUICIDAL);
				break;
			case 2:
				SetState(currentState = RobotStates.STATE_HOMICIDAL);
				break;
		}
	}

	void SearchForTarget() {

		if (currentState == RobotStates.STATE_REPAIRING || isBeingCarried) {
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

		if (target == null)
			SetStateRandom ();
	}

	public void OnPathFound(Vector3[] newPath, bool pathSuccessful) {
		if (pathSuccessful) {
			path = newPath;
			targetIndex = 0;
			currentWaypoint = path [targetIndex];
		} else {
			StopMoving ();
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

	public void StopMoving() {
		path = null;
		targetIndex = 0;
		target = null;
	}

	public bool isCarryingItem {
		get { 
			if (carriedItem != null && carriedItem.GetCarrier () != this) {
				carriedItem = null;
				SetState (RobotStates.STATE_FINDBOX);
			}
			return carriedItem != null;
		}
	}

	// helper function for Throwables checking if they've been delivered by their carrier
	public bool CheckHitTarget(string possibleTargetTag) {
		return possibleTargetTag == target.tag || possibleTargetTag == target.parent.tag;
	}

	public void DropItem() {
		SetState (RobotStates.STATE_FINDBOX);
		if (carriedItem != null) {
			carriedItem.SetCarrier (null);
			carriedItem = null;
		}
	}

	private void GrabItem(Throwable item, RobotStates newState) {
		item.SetKinematic (true);
		carriedItem = item;
		carriedItem.SetCarrier (this);
		carriedItem.ActivateRobotBeam(Instantiate<ParticleSystem> (robotBeamPrefab, carriedItem.transform.position, Quaternion.identity, carriedItem.transform));
		SetState (newState);
		SetupCarryRange ();
		UpdateCarriedItem ();
		SearchForTarget ();
	}

	// derived-class extension of OnCollisionEnter2D
	// because Throwable implements OnCollisionEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitCollision2D(Collision2D collision) {
		if (!isCarryingItem) {
			if (currentState == RobotStates.STATE_FINDBOX && collision.collider.tag == "Box") {
				GrabItem (collision.gameObject.GetComponent<Box> (), RobotStates.STATE_DELIVERING);
				// TODO: once the carriedItem has hit the intended target collider/trigger, 
				// unparent it and change back to previousState (saved whenever changing to DELIVERING)
				// FIXME: ensure that robots stealing boxes doesn't cause multiple parenting (doubtful)
				// but more importantly that the stolen-from robot's carriedItem becomes null and it state returns to oldState (as if delivered)
			} else if (currentState == RobotStates.STATE_HOMICIDAL && collision.collider.tag == "Robot") {
				RobotStates newState = Random.Range (0, 2) == 0 ? RobotStates.STATE_DELIVERING : RobotStates.STATE_SUICIDAL;
				GrabItem(collision.gameObject.GetComponent<Robot> (), newState);
			}
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

	// derived-class extension of OnTriggerEnter2D
	// because Throwable implements OnTriggerEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitTrigger2D (Collider2D hitTrigger) {
		if (hitTrigger.tag == "BoxExit") {
			RandomThrow ();
		}

		if (hitTrigger.tag == "HealZone" && currentState != RobotStates.STATE_REPAIRING && health < maxHealth) {
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
