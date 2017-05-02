using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class Robot : Throwable {

	public GameObject	firePrefab;
	public GameObject 	steamPuffPrefab;
	public ParticleSystem robotBeamPrefab;

	public AudioClip 	repairingSound;			// TODO: loop-play this as long the slider bar is being held
	public AudioClip 	playerGrabbedSound;
	public AudioClip 	robotGrabbedSound;
	public AudioClip	slowThrownSound;

	public AudioClip[]	fastThrownSounds;
	public AudioClip[]	robotReliefSounds;
	public AudioClip	exitRepairZapSound;		// TODO: play this (and shake the robot, and use a particle effect) as a robot breaks

	public Sprite 		onFireSpeechSprite;
	public Sprite 		homicidalSpeechSprite;
	public Sprite 		suidicalSpeechSprite;

	public Transform 	target;
	public float 		speed = 2.0f;
	public float 		slowdownDistance = 2.0f;
	public float 		grabHeight = 10.0f;
	public float		robotScreamTolerance = 16.0f;
	public float		emotionalDistressRate = 0.1f;		// FIXME/TODO: 10 seconds to insanity (tie this to a difficulty slider on the pause menu)
	public float		emotionalBreakdownDuration = 3.0f;
	public float		emotionalStability = 0.0f;

	[HideInInspector]
	public float 		health = 100;
	[HideInInspector]
	public bool 		grabbedByPlayer = false;
	[HideInInspector]
	public float 		spawnTime;

	[HideInInspector]
	public bool onFire {
		get { 
			return fireInstance != null;
		} set { 
			if (!onFire && value) {
				DropItem ();
				currentState = RobotStates.STATE_ONFIRE;
				fireInstance = Instantiate<GameObject> (firePrefab, transform.position, Quaternion.identity, transform);
			} else if (onFire) {
				Destroy (fireInstance);
				HUDManager.instance.ExtinguishFire ();
				Instantiate<GameObject> (steamPuffPrefab, transform.position, Quaternion.identity, transform);
			}
		}
	}

	[HideInInspector]
	public RobotNames.MethodOfDeath howDied = RobotNames.MethodOfDeath.SURVIVED;
		
	public enum RobotStates {
		STATE_FINDBOX,
		STATE_SUICIDAL,
		STATE_HOMICIDAL,
		STATE_ONFIRE
	};
	public RobotStates	currentState = RobotStates.STATE_FINDBOX;
	private bool isDelivering = false;

	// pathing
	private LineRenderer 		line;
	private Grid				grid;
	private Vector3[] 			path;
	private int 				targetIndex;
	private Vector3 			currentWaypoint;
	private Vector3				targetLastKnownPosition;
	private float 				sqrTargetSlowdownDistance;
	private float 				carryItemDistance;
	private const float			pathUpdateMoveThreshold = 0.25f;
	private const float 		minWaitTime = 0.2f;
	private const float 		stoppingThreshold = 0.01f;
	private bool 				waitingForPathRequestResults;

	// state machine
	private float				stateSpeedMultiplier = 1.0f;
	private bool 				justReleased;
	private bool				freakingOut;
	private Throwable 			carriedItem;
	private GameObject 			fireInstance;
	private CircleCollider2D 	circleCollider;
	private Animator			animator;
	private Image				currentSpeech;

	void Start() {
		line = GetComponent<LineRenderer> ();
		line.enabled = false;
		animator = GetComponent<Animator> ();
		circleCollider = GetComponent<CircleCollider2D> ();

		// InfoCanvas initialization
		currentSpeech = GetComponentInChildren<Image> ();
		currentSpeech.enabled = false;
		Text[] robotNamePlate = GetComponentsInChildren<Text> ();
		name = RobotNames.Instance.TryGetSurvivorName();
		robotNamePlate[0].text = name;
		robotNamePlate[1].text = name;

		sqrTargetSlowdownDistance = slowdownDistance * slowdownDistance;
		grid = GameObject.FindObjectOfType<Grid> ();
		spawnTime = Time.time;
	}

	void Update() {
		UpdateEmotionalState ();

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

		if (onFire && HUDManager.instance.playSprinklerSystem)
			onFire = false;

		// FIXME: version 2.0 wont have special states to FIND nearest robots or hazards (maybe)
		// and won't have a repair zone at all
//		currentState == RobotStates.STATE_REPAIRING;
//		currentState = RobotStates.STATE_FINDBOX;
//		HUDManager.instance.RobotRepairComplete ();
	
		if (grounded && !fellInPit && !isBeingCarried)
			SearchForTarget ();

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
		
			if (target != null || path != null) {
				StopMoving ();
			}

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
		
	public void StartDelivering() {
		isDelivering = true;
		StopMoving ();
	}

	public void StopDelivering() {
		isDelivering = false;
		StopMoving ();
	}

	public void UpdateEmotionalState() {
		if (emotionalStability < 1.0f) {
			emotionalStability += emotionalDistressRate * Time.deltaTime;

			float timeRemaining = (1.0f - emotionalStability) / emotionalDistressRate;
			if (timeRemaining < emotionalBreakdownDuration && timeRemaining > 0.0f && !freakingOut) {
				UIManager.instance.ShakeObject (this.gameObject, timeRemaining);
				// TODO: begin looping (child) electric shock particle effect
			}
			freakingOut = timeRemaining < emotionalBreakdownDuration;
				
			if (emotionalStability >= 1.0f) {
				emotionalStability = 1.0f;
				// TODO: stop playing electric shock particle effect
				float flipACoin = Random.Range (0, 2);
				currentState = flipACoin == 0 ? RobotStates.STATE_HOMICIDAL : RobotStates.STATE_SUICIDAL;
			}
		}
	}

	void SearchForTarget() {

		if (isBeingCarried || !grid.NodeFromWorldPoint(transform.position).walkable || GameManager.instance.levelEnded) {
			StopMoving ();
			return;
		}

		CheckIfTargetLost ();
			
		if (target != null) {
			FollowPath ();
			return;
		}

		switch (currentState) {
			case RobotStates.STATE_FINDBOX:
				spriteRenderer.color = Color.white;
				line.colorGradient = GameManager.instance.blueWaveGradient;
				stateSpeedMultiplier = 1.0f;
				target = isDelivering ? GameManager.instance.GetClosestDeliveryTarget (this)
									  : GameManager.instance.GetClosestBoxTarget (this);
				break;
			case RobotStates.STATE_SUICIDAL:
				spriteRenderer.color = Color.cyan;
				currentSpeech.sprite = suidicalSpeechSprite;
				line.colorGradient = GameManager.instance.greenWaveGradient;
				stateSpeedMultiplier = 0.5f;
				target = GameManager.instance.GetClosestHazardTarget (this);
				break;
			case RobotStates.STATE_HOMICIDAL:
				spriteRenderer.color = Color.red;
				currentSpeech.sprite = homicidalSpeechSprite;
				line.colorGradient = GameManager.instance.redWaveGradient;
				stateSpeedMultiplier = 2.0f;
				target = isDelivering ? GameManager.instance.GetClosestHazardTarget (this)
									  : GameManager.instance.GetClosestRobotTarget (this);
				break;
			case RobotStates.STATE_ONFIRE: 
				StopMoving();
				// TODO: onFire overwrites currentState fully (no deliveries, no homicide, no suicide, different targets though)
				// TODO: run around like a crazy person
				currentSpeech.sprite = onFireSpeechSprite;
				break;
		}
		currentSpeech.enabled = currentState != RobotStates.STATE_FINDBOX;
	}

	public void OnPathFound(Vector3[] newPath, bool pathSuccessful) {
		if (pathSuccessful) {
			path = newPath;
			targetIndex = 0;
			currentWaypoint = path [targetIndex];
		} else {
			StopMoving ();
		}
		waitingForPathRequestResults = false;
	}

	void UpdatePath(bool freshStart) {
		if (!waitingForPathRequestResults && (freshStart || (target.position - targetLastKnownPosition).sqrMagnitude > pathUpdateMoveThreshold)) {
			waitingForPathRequestResults = true;
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
		if (currentState != RobotStates.STATE_HOMICIDAL) {
			float sqrRange = (path [path.Length - 1] - transform.position).sqrMagnitude;
			if (sqrRange < sqrTargetSlowdownDistance) {
				percentSpeed = Mathf.Clamp01 (Mathf.Sqrt (sqrRange) / slowdownDistance);
				if (percentSpeed < stoppingThreshold) {
					StopMoving ();
					return;
				}
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

		transform.position = Vector3.MoveTowards (transform.position, currentWaypoint, speed * stateSpeedMultiplier * percentSpeed * Time.deltaTime);
		UpdatePathLine ();
	}

	private void UpdatePathLine() {
		line.enabled = true;

		if (path != null) {
			line.numPositions = (path.Length - targetIndex) + 1;
			line.SetPosition(0, transform.position);

			for (int i = targetIndex, pos = 1; i < path.Length; i++, pos++)
					line.SetPosition (pos, path [i]);
		}
	}

	public void StopMoving() {
		if (isTargetThrowable) 
			target.GetComponent<Throwable> ().SetTargeter (null);
	
		path = null;
		targetIndex = 0;
		target = null;
		if (line != null)
			line.enabled = false;
	}

	public bool isCarryingItem {
		get { 
			if (carriedItem != null && carriedItem.GetCarrier () != this) {
				carriedItem = null;
				StopDelivering ();
			}
			return carriedItem != null;
		}
	}

	public bool isTargetThrowable {
		get { 
			return target != null && target.GetComponent<Throwable> () != null;	
		}
	}

	// stop pathing to a box/robot that this has targeted, but has since been grabbed by another robot
	// FIXME(?): homicidal robots still target robots grabbed by the player
	public void CheckIfTargetLost() {
		if (isTargetThrowable) {
			Robot targeter = target.GetComponent<Throwable> ().GetTargeter ();
			if (targeter != null && targeter != this) {
				StopMoving ();
			}
		}
	}

	// helper function for Throwables checking if they've been delivered by their carrier
	public bool CheckHitTarget(string possibleTargetTag) {
		if (target != null && possibleTargetTag == target.tag && target.tag == "BoxExit")
			RobotNames.Instance.AddRobotBoxDelivery (name);
		
		return target == null || possibleTargetTag == target.tag;
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
		StartDelivering ();
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

		if (collision.collider.tag == "Crusher") {
			howDied = RobotNames.MethodOfDeath.DEATH_BY_CRUSHER;
			Explode ();
		}

		if (collision.collider.tag == "Robot") {
			if (!onFire && collision.collider.GetComponent<Robot> ().onFire)
				onFire = true;
		}
	}

	void OnCollisionStay2D(Collision2D collision) {
		if (collision.collider.tag == "Crusher"){
			howDied = RobotNames.MethodOfDeath.DEATH_BY_CRUSHER;
			Explode ();
		}
	}

	// derived-class extension of OnTriggerEnter2D
	// because Throwable implements OnTriggerEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitTrigger2D (Collider2D hitTrigger) {
		// robot trigger stuff here
	}

	// Debug Drawing
	public void OnDrawGizmos() {
		if (isTargeted) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawCube (transform.position, Vector3.one);
		}
			
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
		if (!fellInPit)		// bugfix for the landing sound overwriting status change sounds
			PlayRandomSoundFx (landingSounds);
	}
}
