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
	public AudioClip[]	breakdownShakeSounds;
	public AudioClip	breakdownZapSound;

	public Sprite 		onFireSpeechSprite;
	public Sprite 		homicidalSpeechSprite;
	public Sprite 		suidicalSpeechSprite;
	public Image		currentSpeech;

	public Transform 	target;
	public float 		speed = 2.0f;
	public float 		slowdownDistance = 2.0f;
	public float 		grabHeight = 10.0f;
	public float		robotScreamTolerance = 16.0f;
	public float		emotionalDistressRate = 0.1f;		// FIXME/TODO: 10 seconds to insanity (tie this to a difficulty slider on the pause menu)
	public float		freakoutThreshold = 0.8f;
	public float 		freakoutShakeDuration = 0.5f;
	public float		emotionalStability = 0.0f;

	[HideInInspector]
	public float 		health = 100;
	[HideInInspector]
	public bool 		grabbedByPlayer = false;
	[HideInInspector]
	public bool 		lockedByPlayer = false;
	[HideInInspector]
	public float 		spawnTime;

	[HideInInspector]
	public bool onFire {
		get { 
			return fireInstance != null;
		} set { 
			if (!onFire && value) {
				DropItem ();
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
		STATE_HOMICIDAL
	};
	public RobotStates	currentState = RobotStates.STATE_FINDBOX;
	private bool isDelivering = false;

	// pathing
	private LineRenderer 		line;
	private Grid				grid;
	private PathFinding 		pathFinder;
	private List<GridNode> 		drawnPath;
	private Vector3[] 			path;
	private int 				targetIndex;
	private int					slowdownIndex;				// account for unusually curvy paths
	private int					oldDrawnPathCount;			// used for updating the drawnPath color
	private Vector3 			currentWaypoint;
	private Vector3				targetLastKnownPosition;
	private float 				sqrTargetSlowdownDistance;
	private float 				carryItemDistance;
	private const float			pathUpdateMoveThreshold = 0.25f;
	private const float 		minWaitTime = 0.2f;
	private const float 		stoppingThreshold = 0.01f;
	private bool 				waitingForPathRequestResults;
	private List<Vector3[]> 	subPaths;
	private int 				numSubPathsToProcess;

	// state machine
	private float				stateSpeedMultiplier = 1.0f;
	private bool 				justReleased;
	private Throwable 			carriedItem;
	private GameObject 			fireInstance;
	private CircleCollider2D 	circleCollider;
	private Animator			animator;
	private ParticleSystem		shockParticles;
	private IEnumerator			freakoutCoroutine = null;

	void Start() {
		drawnPath = new List<GridNode> ();
		subPaths = new List<Vector3[]> ();
		line = GetComponent<LineRenderer> ();
		line.enabled = false;
		animator = GetComponent<Animator> ();
		circleCollider = GetComponent<CircleCollider2D> ();

		// Throwable has grabbed the first particle system child
		// this needs the next one
		ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem> ();
		foreach (ParticleSystem system in allParticles) {
			if (system.name == "ShockParticles") {
				shockParticles = system;
				break;
			}
		}

		// InfoCanvas initialization
		currentSpeech = GetComponentInChildren<Image> ();
		currentSpeech.enabled = false;
		Text[] robotNamePlate = GetComponentsInChildren<Text> ();
		name = RobotNames.Instance.TryGetSurvivorName();
		robotNamePlate[0].text = name;
		robotNamePlate[1].text = name;

		sqrTargetSlowdownDistance = slowdownDistance * slowdownDistance;
		grid = GameObject.FindObjectOfType<Grid> ();
		pathFinder = GameObject.FindObjectOfType<PathFinding> ();
		spawnTime = Time.time;
	}

	void Update() {
		UpdateEmotionalState ();
		UpdateVisuals ();

		if (!CheckGrabbedStatus ()) {
			UpdatePathLine ();

			if (!grounded && !justReleased) {
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
		}

		if (onFire && HUDManager.instance.playSprinklerSystem)
			onFire = false;

		// FIXME: version 2.0 wont have special states to FIND nearest robots or hazards (maybe)
		// and won't have a repair zone at all
//		currentState == RobotStates.STATE_REPAIRING;
//		currentState = RobotStates.STATE_FINDBOX;
//		HUDManager.instance.RobotRepairComplete ();
	
		SearchForTarget ();
		UpdateShadow ();
		UpdateCarriedItem ();
	}

	private bool CheckGrabbedStatus() {
		if (grabbedByPlayer || lockedByPlayer || isBeingCarried) {
			if (isCarryingItem && grabbedByPlayer) {
				if (carriedItem is Robot)
					PlayRandomSoundFx (robotReliefSounds);
				DropItem ();
			}
		
			if (isBeingCarried && (grabbedByPlayer || lockedByPlayer)) {
				PlayRandomSoundFx (robotReliefSounds);
				GetCarrier ().DropItem ();
			}

			if (path != null)
				StopMoving ();
		
			if (grabbedByPlayer) {
				justReleased = false;
				SetHeight (grabHeight);
			}

			if (lockedByPlayer)
				ShowDrawnPath ();

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
		if (!isCarryingItem || path == null || path.Length <= 0)
			return;
		
		Vector3 carryDir = (path [path.Length - 1] - transform.position).normalized;
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
			currentState = RobotStates.STATE_FINDBOX;
			emotionalStability += emotionalDistressRate * Time.deltaTime;

			if (emotionalStability > freakoutThreshold)
				StartCoroutine (Freakout());
		}

		if (emotionalStability >= 1.0f && currentState == RobotStates.STATE_FINDBOX) {
			emotionalStability = 1.0f;
			GoCrazy ();
		}
	}

	// do not have more than one of this coroutine running on a robot at a time
	public IEnumerator Freakout() {
		if (freakoutCoroutine == null)
			freakoutCoroutine = Freakout ();
		else
			yield break;

		RobotStates oldState = currentState;
		float oldStability = emotionalStability;
		var main = shockParticles.main;
		ParticleSystemRenderer psr = shockParticles.GetComponent<ParticleSystemRenderer> ();

		do {
			UIManager.instance.ShakeObject (this.gameObject, true, freakoutShakeDuration);
			if (!shockParticles.isPlaying) { 
				main.loop = true;
				shockParticles.Play(); 
			} else {
				psr.sortingLayerID = spriteRenderer.sortingLayerID;
			}
			if (!efxSource.isPlaying)
				PlayRandomSoundFx (breakdownShakeSounds);
			
			yield return new WaitForSeconds (freakoutShakeDuration);
		} while (currentState == oldState && emotionalStability > oldStability);

		main.loop = false;
		foreach (AudioClip clip in breakdownShakeSounds) {
			if (efxSource.clip == clip) {
				efxSource.Stop ();
				break;
			}
		}
		freakoutCoroutine = null;
	}

	public void ToggleCrazyEmotion() {
		StartCoroutine (Freakout ());
		if (currentState == RobotStates.STATE_FINDBOX)
			GoCrazy ();
		else
			currentState = (currentState == RobotStates.STATE_HOMICIDAL ? RobotStates.STATE_SUICIDAL : RobotStates.STATE_HOMICIDAL);
	}

	private void GoCrazy () {
		PlaySingleSoundFx (breakdownZapSound);
		float flipACoin = Random.Range (0, 2);
		currentState = flipACoin == 0 ? RobotStates.STATE_HOMICIDAL : RobotStates.STATE_SUICIDAL;
	}

	private void UpdateVisuals() {
		switch (currentState) {
			case RobotStates.STATE_FINDBOX:
				spriteRenderer.color = Color.white;
				line.colorGradient = lockedByPlayer ? line.colorGradient 
													: GameManager.instance.blueWaveGradient;
				break;
			case RobotStates.STATE_SUICIDAL:
				spriteRenderer.color = Color.cyan;
				currentSpeech.sprite = suidicalSpeechSprite;
				line.colorGradient = lockedByPlayer ? line.colorGradient 
													: GameManager.instance.greenWaveGradient;
				break;
			case RobotStates.STATE_HOMICIDAL:
				spriteRenderer.color = Color.red;
				currentSpeech.sprite = homicidalSpeechSprite;
				line.colorGradient = lockedByPlayer ? line.colorGradient 
													: GameManager.instance.redWaveGradient;
				break;
		}

		if (onFire)
			currentSpeech.sprite = onFireSpeechSprite;

		currentSpeech.enabled = currentState != RobotStates.STATE_FINDBOX || onFire;
	}

	void SearchForTarget() {
		if (!grounded)
			return;
			
		if (isBeingCarried || fellInPit || !grid.NodeFromWorldPoint(transform.position).walkable || GameManager.instance.levelEnded) {
			StopMoving ();
			return;
		}

		CheckIfTargetLost ();
			
		if (target != null || path != null) {
			FollowPath ();
			return;
		}

		// FIXME: only get a box/delivery target if not given an explicit path from the player
		// TODO: also run around like a crazy person if onFire == true

		switch (currentState) {
			case RobotStates.STATE_FINDBOX:
				stateSpeedMultiplier = 1.0f;
				target = isDelivering ? GameManager.instance.GetClosestDeliveryTarget (this)
									  : GameManager.instance.GetClosestBoxTarget (this);
				break;
			case RobotStates.STATE_SUICIDAL:
				stateSpeedMultiplier = 0.5f;
				target = GameManager.instance.GetClosestHazardTarget (this);
				break;
			case RobotStates.STATE_HOMICIDAL:
				stateSpeedMultiplier = 2.0f;
				target = isDelivering ? GameManager.instance.GetClosestHazardTarget (this)
									  : GameManager.instance.GetClosestRobotTarget (this);
				break;
		}
	}

	public void OnPathFound(Vector3[] newPath, bool pathSuccessful, bool isSubPath) {
		waitingForPathRequestResults = false;
		if (pathSuccessful) {
			if (isSubPath) {
				subPaths.Add (newPath);
				numSubPathsToProcess--;

				if (numSubPathsToProcess <= 0) {
					numSubPathsToProcess = 0;
					path = pathFinder.MergeSubPaths (subPaths);
					subPaths.Clear ();
					if (path.Length > 0)
						InitPath ();
				} else {
					waitingForPathRequestResults = true;
				}

			} else {
				path = newPath;
				InitPath ();
			}
		} else {
			StopMoving ();
		}
	}

	private void InitPath () {
		targetIndex = 0;
		currentWaypoint = path [targetIndex];

		// account for unusually curvy paths
		float distanceFromEnd = 0.0f;
		for (int i = path.Length - 1; i > 0; i--) {
			distanceFromEnd += Vector3.Distance (path [i], path [i - 1]);
			if (distanceFromEnd >= slowdownDistance) {
				slowdownIndex = i;
				break;
			}
		}
	}

	public void ClearUserDefinedPath() {
		oldDrawnPathCount = 0;
		drawnPath.Clear ();
		subPaths.Clear ();
	}
		
	public void TryAddPathPoint (Vector3 worldPosition) {
		GridNode node = grid.NodeFromWorldPoint (worldPosition);
		if (node.walkable && !drawnPath.Contains (node)) {
			if (pathFinder.PathLengthSqr(drawnPath) < GameManager.instance.maxDrawnPathLengthSqr)
				drawnPath.Add (node);				
		}
	}
		
	public bool FinishDrawingPath() {
		bool longEnoughPath = pathFinder.PathLengthSqr (drawnPath) > GameManager.instance.drawnPathLengthThresholdSqr;
		if (longEnoughPath) {
			waitingForPathRequestResults = true;
			numSubPathsToProcess = pathFinder.OptimizeDrawnPath (drawnPath, OnPathFound);
		}
		ClearUserDefinedPath ();
		return longEnoughPath;
	}

	void UpdatePath(bool freshStart) {
		if (!waitingForPathRequestResults && (freshStart || (target != null && (target.position - targetLastKnownPosition).sqrMagnitude > pathUpdateMoveThreshold))) {
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
		if (targetIndex >= slowdownIndex && currentState != RobotStates.STATE_HOMICIDAL) {
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
	}

	private void UpdatePathLine() {
		if (path == null)
			return;

		line.enabled = true;
		line.numPositions = (path.Length - targetIndex) + 1;
		line.SetPosition(0, transform.position);

		for (int i = targetIndex, pos = 1; i < path.Length; i++, pos++)
			line.SetPosition (pos, path [i]);
	}

	private void ShowDrawnPath() {
		if (drawnPath.Count <= 0)
			return;
		
		line.enabled = true;
		if (drawnPath.Count > oldDrawnPathCount)
			line.colorGradient = pathFinder.PathLengthSqr(drawnPath) > GameManager.instance.drawnPathLengthThresholdSqr ? GameManager.instance.silverWaveGradient 
																								   						: GameManager.instance.blackWaveGradient;
		oldDrawnPathCount = drawnPath.Count;
		line.numPositions = drawnPath.Count + 1;
		line.SetPosition(0, transform.position);

		for (int i = 0, pos = 1; i < drawnPath.Count; i++, pos++)
			line.SetPosition (pos, drawnPath [i].worldPosition);		
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

	// stop pathing to a box/robot that this has targeted, but has since been grabbed by another robot (or the player)
	public void CheckIfTargetLost() {
		if (isTargetThrowable) {
			Throwable throwableTarget = target.GetComponent<Throwable> ();
			Robot targeter = throwableTarget.GetTargeter ();
			if (targeter == null || (targeter != null && targeter != this)) {
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
				else if (toGrab.tag == "Robot" && currentState == RobotStates.STATE_HOMICIDAL && !(toGrab as Robot).lockedByPlayer)
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
