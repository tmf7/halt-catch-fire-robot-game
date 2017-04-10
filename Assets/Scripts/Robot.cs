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
	private RobotStates	currentState;
	private RobotStates	oldState;

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
		SetState (RobotStates.STATE_FINDBOX);
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
		UpdateCarriedItemPosition ();		// FIXME: putting this before SearchForTarget call causes a null target.position check in UpdateCarriedItemPosition
		CheckIfCarried ();
	}

	private bool CheckGrabbedByPlayer() {
		// TODO: another robot can set the grabbed boolean
		// picking up a grabbed robot keeps the two attached and drops/ungrabs all 
		// picking up a grabbing robot makes it drop its carried item
		if (grabbedByPlayer) {
			DropItem ();
			StopMoving ();
			justReleased = false;
			if (whoGrabbed.tag == "Player") 
				SetHeight (grabHeight);
			else if (whoGrabbed.tag == "Robot" && robotBeam == null) 
				PlaySingleSoundFx (robotGrabbedSound);	// distressed robot better indicates it's a victim
			return true;
		}
		return false;
	}

	private void UpdateCarriedItemPosition() {
		if (!isCarryingItem || target == null)
			return;
		
		float robotMoveAngle = Vector3.Angle (Vector3.right, target.position - transform.position);
		if (robotMoveAngle <= 45.0f) {
			carriedItem.transform.localPosition = Vector3.right * carryItemDistance;
		} else if (robotMoveAngle <= 90.0f) {
			carriedItem.transform.localPosition = Vector3.up * carryItemDistance;
		} else if (robotMoveAngle <= 180.0f) {
			carriedItem.transform.localPosition = Vector3.left * carryItemDistance;
		} else if (robotMoveAngle < 360.0f) {
			carriedItem.transform.localPosition = Vector3.down * carryItemDistance;
		}
	}

	public RobotStates GetState() {
		return currentState;
	}

	public void SetState(RobotStates newState) {
		oldState = currentState;
		currentState = newState;
		StopMoving ();
	}

	public void RevertState() {
		currentState = oldState;
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
			StopMoving ();
		} else {	
			ClaimObject (target.gameObject); 
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
		if (target != null) {
			UnclaimObject (target.gameObject);
			target = null;
		}
	}

	// prevent others from attempting to grab this target 
	// only affects boxes and robots, not delivery points or hazards, see GameManager
	// FIXME: this may be an issue for targeting robots with box/robot children
	private void ClaimObject(GameObject obj) {
		Throwable targetObj = obj.GetComponent<Throwable> ();
		if (targetObj != null)
			targetObj.SetClaimant (gameObject);
	}

	// isClaimed also becomes false if the claimant dies (see Throwable.isClaimed)
	// FIXME: this may be an issue for targeting robots with box/robot children
	public void UnclaimObject(GameObject obj) {
		Throwable targetObj = obj.GetComponent<Throwable> ();
		if (targetObj != null)
			targetObj.SetClaimant (null);	
	}

	public bool isCarryingItem {
		get { 
			Throwable checkCarry = GetComponentInChildren<Throwable> ();
			if (checkCarry == null)
				carriedItem = null;
			return carriedItem != null;
		}
	}

	public void DropItem() {
		if (isCarryingItem) {
			UnclaimObject (carriedItem.gameObject);
			carriedItem.transform.SetParent (null);
			carriedItem = null;
		}
		RevertState ();
	}

	private void GrabItem(Throwable item, RobotStates newState) {
		StopMoving ();
		item.transform.SetParent (transform);
		item.GetComponent<Rigidbody2D> ().constraints = RigidbodyConstraints2D.FreezeRotation;
		item.SetKinematic (true);
		carriedItem = item;
		ClaimObject (item.gameObject);
		SetState (newState);
		SearchForTarget ();
	}

	protected override void HitCollision2D(Collision2D collision) {
		if (carriedItem == null) {
			if (currentState == RobotStates.STATE_FINDBOX && collision.collider.tag == "Box") {
				GrabItem (collision.collider.gameObject.GetComponent<Box> (), RobotStates.STATE_DELIVERING);
				// TODO: once the carriedItem has hit the intended target collider/trigger, 
				// unparent it and change back to previousState (saved whenever changing to DELIVERING)
				// FIXME: ensure that robots stealing boxes doesn't cause multiple parenting (doubtful)
				// but more importantly that the stolen-from robot's carriedItem becomes null and it state returns to oldState (as if delivered)
			} else if (currentState == RobotStates.STATE_HOMICIDAL && collision.collider.tag == "Robot") {
				RobotStates newState = Random.Range (0, 2) == 0 ? RobotStates.STATE_DELIVERING : RobotStates.STATE_SUICIDAL;
				GrabItem(collision.collider.gameObject.GetComponent<Robot> (), newState);
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
		/*
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
		*/
	}

	protected override void OnLanding () {
		base.OnLanding ();
		if (currentState != RobotStates.STATE_REPAIRING && !fellInPit && !onFire)		// bugfix for the landing sound overwriting status change sounds
			PlayRandomSoundFx (landingSounds);
	}
}
