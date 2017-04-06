using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Robot : Throwable {

	public ParticleSystem 	firePrefab;
	public ParticleSystem 	robotBeamPrefab;
	public AudioClip	robotLandingSound;
	public Transform 	target;
	public float 		speed = 2.0f;
	public float 		slowdownDistance = 2.0f;
	public float 		grabHeight = 10.0f;
	public float		damageRate = 9.0f;
	public float		healRate = 10.0f;
	public float		maxHealth = 100.0f;

	[HideInInspector]
	public GameObject	whoGrabbed;
	[HideInInspector]
	public float 		health = 100;
	[HideInInspector]
	public bool 		onHealing = false;
	[HideInInspector]
	public ParticleSystem fireReference;


	[HideInInspector]
	public bool onFire {
		get { 
			return fireReference != null;
		} set { 
			if (value)
				fireReference = Instantiate (firePrefab, transform.position, Quaternion.identity, transform);
			else
				Destroy (fireReference);
		}
	}


	public enum RobotStates {
		STATE_NORMAL,
		STATE_SUICIDAL,
		STATE_HOMICIDAL,
		STATE_ONFIRE,
		STATE_REPAIRING
	};

	private Animator 	animator;
	private Vector3[] 	path;
	private int 		targetIndex;
	private const float	pathUpdateMoveThreshold = 0.25f;
	private const float minWaitTime = 0.2f;
	private const float stoppingThreshold = 0.01f;
	private bool 		justReleased;
	private Throwable 	carriedItem;					// FIXME: make sure this starts null because of C#
	private ParticleSystem robotBeam;
	private RobotStates	currentState;

	void Start() {
		animator = GetComponent<Animator> ();
		currentState = RobotStates.STATE_NORMAL;
		StartCoroutine ("UpdatePath");
	}

	public void OnPathFound(Vector3[] newPath, bool pathSuccessful) {
		if (grounded && pathSuccessful) {
			path = newPath;
			targetIndex = 0;
			StopCoroutine ("FollowPath");
			StartCoroutine ("FollowPath");
		}
	}

	void Update() {
		if (grabbed) {
			// TODO: another robot can set the grabbed boolean
			// picking up a grabbed robot keeps the two attached and drops/ungrabs all 

			justReleased = false;
			if (whoGrabbed.tag == "Player") {
				SetHeight (grabHeight);
			}
			if (robotBeam == null) {
				robotBeam = Instantiate<ParticleSystem> (robotBeamPrefab, transform.position, Quaternion.identity, transform);
			} 
			target = null;
		} else if (!grounded && !justReleased) {
			Destroy (robotBeam);					//FIXME: this line is a test
			justReleased = true;
			rb2D.velocity = new Vector2(dropForce.x, dropForce.y);
			Throw (0.0f, -1.0f);
		} 

		if (onFire)
			health -= Time.deltaTime * damageRate;

		if (onHealing) {
			onFire = false;

			currentState = RobotStates.STATE_REPAIRING;
			health += Time.deltaTime * healRate;
			if (health > maxHealth) {
				health = maxHealth;
				onHealing = false;
			}
		}

		if (health <= 0) 
			Explode();
		
		UpdateShadow ();
	}

	public void SetState(RobotStates newState) {
		currentState = newState;
		target = null;				// allows SearchForTarget to start fresh
	}

	public void SetStateRandom() {
		switch (Random.Range (0, 3)) {
			case 0:
				currentState = RobotStates.STATE_NORMAL;
				break;
			case 1:
				currentState = RobotStates.STATE_SUICIDAL;
				break;
			case 2:
				currentState = RobotStates.STATE_HOMICIDAL;
				break;
		}
		target = null;			// allows SearchForTarget to start fresh
	}

	bool SearchForTarget() {
		if (target != null || currentState == RobotStates.STATE_REPAIRING)
			return true;
	
		// SEARCH for a box to pickup, not just a random box. (meaning do a box2d sweep, then path to a random valid cell if nothing is found) 
		switch (currentState) {
			case RobotStates.STATE_NORMAL:
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
		return target != null;
	}

	IEnumerator UpdatePath() {
		// prevents large Time.deltaTime values when the game first starts up
		if (Time.timeSinceLevelLoad < minWaitTime) {
			yield return new WaitForSeconds (minWaitTime);
		}

		while (!SearchForTarget ()) {
			yield return null;
		}

		Vector3 targetPosOld = target.position;

		while (true) {
			yield return new WaitForSeconds (minWaitTime);
			if (grounded && SearchForTarget() && (target.position - targetPosOld).sqrMagnitude > pathUpdateMoveThreshold) {
				PathRequestManager.RequestPath (transform.position, target.position, OnPathFound);
				targetPosOld = target.position;
			}
		}
	}

	IEnumerator FollowPath() {
		Vector3 currentWaypoint = path [0];
		float sqrTargetSlowdownDistance = slowdownDistance * slowdownDistance;
		Vector3 oldPosition = transform.position;

		while (true) {
			if (!grounded)
				yield return null;

			if (transform.position == currentWaypoint) {
				targetIndex++;
				if (targetIndex >= path.Length) {
					// path = null;
					//target = null;
					yield break;
				}
				currentWaypoint = path [targetIndex];
			}

			float percentSpeed = 1.0f;
			float sqrRange = (path [path.Length - 1] - transform.position).sqrMagnitude;
			if (sqrRange < sqrTargetSlowdownDistance) {
				percentSpeed = Mathf.Clamp01 (Mathf.Sqrt (sqrRange) / slowdownDistance);
				if (percentSpeed < stoppingThreshold) {
					//target = null;
					yield break;
				}
			}
				
			transform.position = Vector3.MoveTowards (transform.position, currentWaypoint, speed * percentSpeed * Time.deltaTime);
			if (transform.position.x - oldPosition.x < 0.0f)
				animator.SetBool ("WalkLeft", true);
			else
				animator.SetBool ("WalkLeft", false);

			oldPosition = transform.position;

			yield return null;
		}
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (collision.collider.tag == "Box") { // AND the box is the target box...or just pickup the first box you hit, its all the same
			Box hitBox = collision.collider.gameObject.GetComponent<Box> ();
			hitBox.Explode ();
		}
		// if the robot has collided with its target (and its a box~)
		// then pick the box up (shift its y a little up)
		// and start moving with the box (give the Box object a posseser, or assign a gameobject to a private variable in Robot, or both)


		if (collision.collider.tag == "Furnace") {
			if (!onFire)
				onFire = true;
		}

		if (collision.collider.tag == "Robot") {
			if (!onFire && collision.collider.GetComponent<Robot> ().onFire)
				onFire = true;
		}
	}

	void OnTriggerEnter2D(Collider2D collider) {
		if (collider.tag == "HealZone")
			onHealing = true;
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
		SoundManager.instance.PlayRandomSoundFx (robotLandingSound);
	}
}
