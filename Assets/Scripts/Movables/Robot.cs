using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class Robot : Throwable {

	public Sprite slimeBotSprite;
	public AnimatorOverrideController 	slimeBotController;

	public ParticleSystem firePrefab;
	public ParticleSystem robotBeamPrefab;

	public AudioClip 	repairingSound;
	public AudioClip	catchFireSound;
	public AudioClip 	playerGrabbedSound;
	public AudioClip 	robotGrabbedSound;
	public AudioClip	slowThrownSound;

	public AudioClip[]	fastThrownSounds;
	public AudioClip[]	robotReliefSounds;
	public AudioClip	exitRepairZapSound;

	public Transform 	target;
	public float 		speed = 2.0f;
	public float 		slowdownDistance = 2.0f;
	public float 		grabHeight = 10.0f;
	public float		damageRate = 9.0f;
	public float		healRate = 10.0f;
	public float		maxHealth = 100.0f;
	public float		robotScreamTolerance = 16.0f;

	public float		findBoxChance = 0.8f;
	public float		homicideChance = 0.1f;
//	public float		suicideChance = 0.1f;		// the remaining probability

	[HideInInspector]
	public float 		health = 100;
	[HideInInspector]
	public bool 		grabbedByPlayer = false;
	[HideInInspector]
	public Text 		nameTag;

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
	public RobotStates oldState;

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
	private bool 				justReleased;
	private Throwable 			carriedItem;
	private ParticleSystem 		fireInstance;
	private CircleCollider2D 	circleCollider;
	private Animator			animator;
	private Sprite 				eggBotSprite;
	private RuntimeAnimatorController eggBotController;

	void Start() {
		animator = GetComponent<Animator> ();
		eggBotController = animator.runtimeAnimatorController;
		eggBotSprite = spriteRenderer.sprite;
		circleCollider = GetComponent<CircleCollider2D> ();
		nameTag = GetComponentInChildren<Text> ();
		nameTag.text = RobotNames.Instance.GetUnusedName();
		sqrTargetSlowdownDistance = slowdownDistance * slowdownDistance;
		SetState (RobotStates.STATE_FINDBOX);
	}

	void Update() {
		if (!CheckGrabbedStatus () && !grounded && !justReleased) {
			justReleased = true;
			SetHeight (grabHeight);
			rb2D.drag = 0.0f;
			rb2D.velocity = new Vector2 (dropForce.x, dropForce.y);
			Throw (0.0f, -1.0f);
			if (dropForce.sqrMagnitude > robotScreamTolerance)
				PlayRandomSoundFx (fastThrownSounds);
			else
				PlaySingleSoundFx (slowThrownSound);
		}

		if (onFire)
			health -= Time.deltaTime * damageRate;

		if (currentState == RobotStates.STATE_REPAIRING) {
			onFire = false;
			health += Time.deltaTime * healRate;
			if (health > maxHealth) {
				health = maxHealth;
				SetState(RobotStates.STATE_FINDBOX);
			}
		}

		if (health <= 0) 
			Explode();
	
		if (grounded && !fellInPit && !isBeingCarried)
			SearchForTarget ();

		// make the fire shrink too
		if (fellInPit && onFire)
			fireInstance.transform.localScale = new Vector3(currentPitfallScale, 1.0f, currentPitfallScale);

		UpdateShadow ();
		UpdateCarriedItem ();
	}

	private bool CheckGrabbedStatus() {
		if (grabbedByPlayer || isBeingCarried) {
			if (isCarryingItem) {
				if (carriedItem is Robot)
					PlayRandomSoundFx (robotReliefSounds);
				DropItem ();
			}
		
			if (grabbedByPlayer && isBeingCarried) {
				PlayRandomSoundFx (robotReliefSounds);
				GetCarrier ().DropItem ();
			}
		
			if (target != null || path != null)
				StopMoving ();

			if (grabbedByPlayer) {
				justReleased = false;
				SetHeight (grabHeight);
			}
			return true;
		}
		return false;
	}

	// carriedItem must not be null to call this
	private void SetupCarryRange() {
		float carriedRadius = circleCollider.radius;
		float roomForJesus = 0.2f;

		if (carriedItem is Box) {
			float boxRadius = carriedItem.GetComponent<BoxCollider2D> ().bounds.extents.magnitude;
			carriedRadius = boxRadius + roomForJesus;
		} else {	// its a robot
			float robotRadius = carriedItem.GetComponent<CircleCollider2D> ().radius;
			carriedRadius = robotRadius + roomForJesus;
		}
		carryItemDistance = circleCollider.radius + carriedRadius;
	}

	// FIXME: this function may be interfering with releaseing on the conveyor belts from time to time
	// causeing the robot to push into the center of the BoxExit oddly
	private void UpdateCarriedItem() {
		if (!isCarryingItem || target == null)
			return;

		Vector3 carryDir = (target.position - transform.position).normalized;
		Vector3 carryPos = transform.position + carryDir * carryItemDistance;
		carriedItem.transform.position = new Vector3(carryPos.x, carryPos.y, 0.0f);
	}

	public RobotStates GetState() {
		return currentState;
	}
		
	public void SetState(RobotStates newState) {

		// FIXME: the newState will be delivering often, so the sprite shouldn't change for a homicidal bot
		// HOMICIDE, SUICIDE, ON_FIRE, DELIVERING, FIND_BOX, REPAIRING
		if (newState == RobotStates.STATE_HOMICIDAL) {
			animator.runtimeAnimatorController = slimeBotController;
			spriteRenderer.sprite = slimeBotSprite;
		} else {
			animator.runtimeAnimatorController = eggBotController;
			spriteRenderer.sprite = eggBotSprite;
		}
		oldState = currentState;
		currentState = newState;
		StopMoving ();
	}

	// FIXME: if a Homicidal robot is told to DropItem, then it will revert state, which it shouldn't
	// revert state should only occur upon **Item delivery** or **Item stealing** (not player grabbing)
	// if not delivering and grabbed by player...the old state is...what?
	public void StopDelivering() {
		StopMoving ();
		if (!grabbedByPlayer)
			currentState = oldState;
	}

	public RobotStates GetRandomState() {
		float stateWeight = Random.Range(0.0f, 1.0f);
		if (stateWeight < findBoxChance)
			return RobotStates.STATE_FINDBOX;
		else if (stateWeight < (findBoxChance + homicideChance)) 
			return RobotStates.STATE_HOMICIDAL;
		else
			return RobotStates.STATE_SUICIDAL;
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
	
		switch (currentState) {
			case RobotStates.STATE_FINDBOX:
				target = GameManager.instance.GetRandomBoxTarget();
				break;
			case RobotStates.STATE_DELIVERING:
				if (oldState == RobotStates.STATE_FINDBOX)
					target = GameManager.instance.GetRandomDeliveryTarget ();
				else if (oldState == RobotStates.STATE_HOMICIDAL && (carriedItem is Robot))
					target = GameManager.instance.GetRandomHazardTarget ();
				break;
			case RobotStates.STATE_SUICIDAL:
				// TODO: change the robot visual
				target = GameManager.instance.GetRandomHazardTarget ();
				break;
			case RobotStates.STATE_HOMICIDAL:
				target = GameManager.instance.GetRandomRobotTarget ();
				break;
			case RobotStates.STATE_ONFIRE: 
				// TODO: run around like a crazy person
				break;
		}

		if (target == null)
			SetState(GetRandomState());
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

		// animation controller
		Vector3 move = currentWaypoint - transform.position;

		animator.SetFloat ("XDir", Mathf.Clamp01 (Mathf.Abs(move.x)));
		animator.SetFloat ("YDir", Mathf.Clamp(move.y, -1.0f, 1.0f));
		if (move.x < 0)
			spriteRenderer.flipX = true;
		else
			spriteRenderer.flipX = false;	

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
				SetState (GetRandomState());		// FIXME: if a homicidal robot's robot is stolen it should go back to finding boxes, this line came from: if a carried BOX is properly delivered
													// then give the robot a chance to freak out. But then it should stay in one of the broken states until repaired
			}
			return carriedItem != null;
		}
	}

	// helper function for Throwables checking if they've been delivered by their carrier
	public bool CheckHitTarget(string possibleTargetTag) {
		return possibleTargetTag == target.tag;
	}

	public void DropItem() {
		StopDelivering ();
		if (carriedItem != null) {
			carriedItem.SetCarrier (null);
			carriedItem = null;
		}
	}

	private void GrabItem(Throwable item) {
		carriedItem = item;
		carriedItem.SetKinematic (true);
		carriedItem.SetCarrier (this);
		carriedItem.ActivateRobotBeam(Instantiate<ParticleSystem> (robotBeamPrefab, carriedItem.transform.position, Quaternion.identity, carriedItem.transform));
		SetState (RobotStates.STATE_DELIVERING);
		SetupCarryRange ();
		UpdateCarriedItem ();
		SearchForTarget ();
	}

	public void ExitRobot() {
		RandomThrow ();
	}

	// derived-class extension of OnCollisionEnter2D
	// because Throwable implements OnCollisionEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitCollision2D(Collision2D collision) {
		if (!isCarryingItem && !isBeingCarried) {

			Throwable toGrab = collision.gameObject.GetComponent<Throwable> ();
			if (toGrab != null && toGrab.grounded) {
				if (toGrab.tag == "Box" && currentState == RobotStates.STATE_FINDBOX)
					GrabItem (toGrab);
				else if (toGrab.tag == "Robot" && currentState == RobotStates.STATE_HOMICIDAL)
					GrabItem (toGrab);
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

	void OnCollisionStay2D(Collision2D collision) {
		if (collision.collider.tag == "Crusher")
			Explode ();
	}

	// derived-class extension of OnTriggerEnter2D
	// because Throwable implements OnTriggerEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitTrigger2D (Collider2D hitTrigger) {
		if (hitTrigger.tag == "HealZone" && currentState != RobotStates.STATE_REPAIRING) {
			if (currentState != RobotStates.STATE_FINDBOX)
				health = 50.0f;
			if (health < maxHealth) {
				currentState = RobotStates.STATE_REPAIRING;
				PlaySingleSoundFx (repairingSound);
			}
		}
			
		if (hitTrigger.tag == "Electric" && efxSource.clip != exitRepairZapSound)
			PlaySingleSoundFx (exitRepairZapSound);
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
