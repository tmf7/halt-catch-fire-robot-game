using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Robot : Throwable {

	public ParticleSystem 	firePrefab;
	public ParticleSystem 	robotBeamPrefab;
	public AudioClip 		repairingSound;

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
			if (!onFire && value)
				fireInstance = Instantiate<ParticleSystem> (firePrefab, transform.position, Quaternion.identity, transform);
			else if (onFire)
				Destroy (fireInstance.gameObject);
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

	// pathing
	private Vector3[] 	path;
	private int 		targetIndex;
	private Vector3 	currentWaypoint;
	private Vector3		targetLastKnownPosition;
	private float 		sqrTargetSlowdownDistance;
	private const float	pathUpdateMoveThreshold = 0.25f;
	private const float minWaitTime = 0.2f;
	private const float stoppingThreshold = 0.01f;

	// state machine
	private bool 		justReleased;
	private Throwable 	carriedItem;
	private ParticleSystem robotBeam;
	private ParticleSystem fireInstance;

	void Start() {
		currentState = RobotStates.STATE_FINDBOX;
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

			justReleased = false;
			if (whoGrabbed.tag == "Player") {
				SetHeight (grabHeight);
			} else if (whoGrabbed.tag == "Robot" && robotBeam == null) {
				robotBeam = Instantiate<ParticleSystem> (robotBeamPrefab, transform.position, Quaternion.identity, transform);
			} 
			StopMoving ();
		} else if (!grounded && !justReleased) {
		//	Destroy (robotBeam);					//FIXME: this line is a test
			justReleased = true;
			rb2D.velocity = new Vector2 (dropForce.x, dropForce.y);
			Throw (0.0f, -1.0f);
		}

		if (onFire)
			health -= Time.deltaTime * damageRate;

		if (currentState == RobotStates.STATE_REPAIRING) {
			onFire = false;
			health += Time.deltaTime * healRate;
			if (health > maxHealth) {
				health = maxHealth;
				currentState = RobotStates.STATE_FINDBOX;
			}
		}

		if (health <= 0) 
			Explode();

		if (grounded) 
			SearchForTarget ();
		
		UpdateShadow ();
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
			case RobotStates.STATE_SUICIDAL:
				// change the robot visual
				// target a hazard (pit, furnace, crusher) and fall/fire/explode on contact with those
				break;
			case RobotStates.STATE_HOMICIDAL:
				target = GameManager.instance.GetRandomRobotTarget ();
				// target a random robot to deliver (as if a box), and again, and again
				break;
			case RobotStates.STATE_ONFIRE: 
				// run around like a crazy person
				break;
		}

		if (target == null) {
			//SetStateRandom ();
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
		target = null;
		targetIndex = 0;
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (carriedItem == null) {
			if (currentState == RobotStates.STATE_FINDBOX && collision.collider.tag == "Box") { // AND the box is the target box...or just pickup the first box you hit, its all the same
				Box hitBox = collision.collider.gameObject.GetComponent<Box> ();
				hitBox.Explode ();
			//	hitBox.transform.SetParent (transform);
			//	hitBox.GetComponent<Rigidbody2D> ().constraints = RigidbodyConstraints2D.FreezeRotation;
			//	carriedItem = hitBox;
				// then set its transform.position x/y (z=0) relative to the robots current intended movement along path
			} else if (currentState == RobotStates.STATE_HOMICIDAL && collision.collider.tag == "Robot") {
			//	Robot hitRobot = collision.collider.gameObject.GetComponent<Robot> ();
			//	hitRobot.transform.SetParent (transform);
			//	hitRobot.GetComponent<Rigidbody2D> ().constraints = RigidbodyConstraints2D.FreezeRotation;
			//	carriedItem = hitRobot;
			}
		}
		// if the robot has collided with its target (and its a box~)
		// then pick the box up (shift its y a little up)
		// and start moving with the box (give the Box object a posseser, or assign a gameobject to a private variable in Robot, or both)


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
		if (collider.tag == "Finish") {
			RandomThrow ();
		}
		if (collider.tag == "HealZone") {
			currentState = RobotStates.STATE_REPAIRING;
			efxSource.Stop ();
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
		// robot landing stuff
	}
}
