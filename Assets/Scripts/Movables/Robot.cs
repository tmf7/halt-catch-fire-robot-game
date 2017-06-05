using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class Robot : Throwable {

	public GameObject			observationCirclePrefab;
	public GameObject 			dummyTargetPrefab;
	public GameObject			firePrefab;
	public GameObject 			steamPuffPrefab;
	public ParticleSystem 		robotBeamPrefab;

	public AudioClip 			playerGrabbedSound;
	public AudioClip 			robotGrabbedSound;
	public AudioClip			slowThrownSound;

	public AudioClip[]			fastThrownSounds;
	public AudioClip[]			robotReliefSounds;
	public AudioClip[]			breakdownShakeSounds;
	public AudioClip			breakdownZapSound;

	public Sprite 				onFireSpeechSprite;
	public Sprite 				homicidalSpeechSprite;
	public Sprite 				suidicalSpeechSprite;
	public Sprite 				happySpeechSprite;
	public Image				currentSpeech;

	public Transform 			target;
	public float 				speed = 2.0f;
	public float 				slowdownDistance = 2.0f;
	public float 				grabHeight = 10.0f;
	public float				robotScreamTolerance = 16.0f;
	public float				emotionalDistressRate = 0.1f;
	public float				freakoutThreshold = 0.8f;
	public float 				freakoutShakeDuration = 0.5f;
	public float				emotionalStability = 0.0f;
	public float 				displayEmotionDelay = 2.0f;

	[HideInInspector]
	public GameObject			dummyTarget;						// in the event of more robots than available targets
	[HideInInspector]
	public float 				health = 100;
	[HideInInspector]
	public bool 				grabbedByPlayer = false;
	[HideInInspector]
	public bool 				lockedByPlayer = false;
	[HideInInspector]
	public float 				spawnTime;

	[HideInInspector]
	public bool onFire {
		get { 
			return fireInstance != null;
		} set { 
			if (!onFire && value) {
				DropItem ();
				fireInstance = Instantiate<GameObject> (firePrefab, transform.position, Quaternion.identity, transform);
			} else if (onFire && !value) {
				Destroy (fireInstance);
				HUDManager.instance.ExtinguishFire ();
				Instantiate<GameObject> (steamPuffPrefab, transform.position, Quaternion.identity, transform);
			}
		}
	}

	[HideInInspector]
	public bool hasDrawnPath {
		get {
			return drawnPath.Count > 0;
		}
	}

	[HideInInspector]
	public static bool isHalted {
		get { 
			return haltAndCommand == true;
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
	private Dictionary<int, Vector3[]> 	subPaths;
	private List<GridNode> 		drawnPath;
	private Vector3[] 			path;
	private int 				targetIndex;
	private int					slowdownIndex;				// account for unusually curvy paths
	private int					oldDrawnPathCount;			// for updating the drawnPath color
	private Vector3 			currentWaypoint;
	private Vector3				targetLastKnownPosition;
	private float 				currentDrawnPathLength;
	private float 				sqrTargetSlowdownDistance;
	private float 				carryItemDistance;
	private int 				lowestFailedSubPathIndex = int.MaxValue;
	private bool 				waitingForPathRequestResults;

	private static Grid			grid;
	private static PathFinding 	pathFinder;
	private static float 		stoppingThreshold = 0.01f;
	private static float		pathUpdateMoveThreshold = 0.25f;
	private static int 			pathSmoothingInterval = 3;
	private static float		minDrawnPathLength = 2.0f;
	private static float 		maxDrawnPathLength = 30.0f;
	private static bool			haltAndCommand = false;

	// state machine
	private float				stateSpeedMultiplier = 1.0f;
	private bool 				justReleased;
	private bool 				wasCarryingItem;
	private Throwable 			carriedItem;
	private GameObject 			fireInstance;
	private ObservationCircle	observationCircle;
	private CircleCollider2D 	circleCollider;
	private Animator			animator;
	private ParticleSystem		shockParticles;
	private ParticleSystem 		buttonGlow;
	public IEnumerator			freakoutCoroutine = null;
	private float 				displayEmotionTime;
	private Button 				emotionButton;
	private RectTransform		emotionButtonRect;

	void Start() {
		GameObject circleObj = Instantiate<GameObject> (observationCirclePrefab, transform.position, Quaternion.identity);
		observationCircle = circleObj.GetComponent<ObservationCircle>();
		observationCircle.SetOwner (this);
		drawnPath = new List<GridNode> ();
		subPaths = new Dictionary<int, Vector3[]> ();
		line = GetComponent<LineRenderer> ();
		line.enabled = false;
		animator = GetComponent<Animator> ();
		circleCollider = GetComponent<CircleCollider2D> ();

		// Throwable has grabbed the first two particle system children
		// shockparticles is next, followed by buttonGlow
		ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem> ();
		shockParticles = allParticles [2];
		buttonGlow = allParticles [3];

		// InfoCanvas initialization
		emotionButton = GetComponentInChildren<Button>();
		emotionButtonRect = emotionButton.GetComponent<RectTransform> ();
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
		if (Robot.isHalted)
			spawnTime += Time.deltaTime;

		waitingForPathRequestResults = PathRequestManager.PathRequestsRemaining (name) > 0;
		UpdateEmotionalState ();
		UpdatePathLine ();

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
	
		UpdateVisuals ();
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

	private RaycastHit2D[] interloperHit = new RaycastHit2D[1];			// BUGFIX: for UpdateCarriedItem checks
	private void UpdateCarriedItem() {
		if (!isCarryingItem || path == null || path.Length <= 0)
			return;
		
		Vector3 carryDir = (path [path.Length - 1] - transform.position).normalized;
		Vector3 carryPos = transform.position + carryDir * carryItemDistance;
		carriedItem.transform.position = new Vector3(carryPos.x, carryPos.y, 0.0f);

		// BUGFIX: a box will sometimes get pushed between a robot and its carried item, causing the robot to
		// rocket backwards due to the default unity collision correction
		circleCollider.Raycast (carryDir, interloperHit, carryItemDistance, groundedResetMask);
		if (interloperHit[0].collider != null) {
			Throwable hitItem = interloperHit[0].collider.GetComponent<Throwable> ();
			if (hitItem != null && (hitItem is Box) && hitItem != carriedItem) {
				DropItem ();
			}
		}
	}

	// HUDManager's instance CHILD button HaltAndCommandButton invokes this
	public static void ToggleHaltAndCommand () {
		haltAndCommand = !haltAndCommand;
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

			if (!haltAndCommand) {
				float stabilityMod = (emotionalStability > freakoutThreshold ? 1.0f : (float)Random.Range (0, 2));
				emotionalStability += emotionalDistressRate * Time.deltaTime * stabilityMod;
			}

			if (emotionalStability > freakoutThreshold)
				StartCoroutine (Freakout());
		}

		if (emotionalStability >= 1.0f && currentState == RobotStates.STATE_FINDBOX) {
			emotionalStability = 1.0f;
			PickRandomCrazyEmotion ();
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
		ParticleSystemRenderer shockPSR = shockParticles.GetComponent<ParticleSystemRenderer> ();

		do {
			UIManager.instance.ShakeObject (this.gameObject, true, freakoutShakeDuration);
			shockPSR.sortingLayerID = spriteRenderer.sortingLayerID;
			if (!shockParticles.isPlaying) { 
				main.loop = true;
				shockParticles.Play(); 
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
		switch (currentState) {
			case RobotStates.STATE_FINDBOX:
				currentState = RobotStates.STATE_SUICIDAL;
				emotionalStability = 1.0f;
				break;
			case RobotStates.STATE_SUICIDAL:
				currentState = RobotStates.STATE_HOMICIDAL;
				emotionalStability = 1.0f;
				break;
			case RobotStates.STATE_HOMICIDAL:
				currentState = RobotStates.STATE_FINDBOX;
				emotionalStability = 0.0f;
				break;
		}
		PlaySingleSoundFx (breakdownZapSound);
		displayEmotionTime = Time.time + displayEmotionDelay;
		SetTarget (null);
	}

	private void PickRandomCrazyEmotion () {
		StartCoroutine (Freakout ());
		float flipACoin = Random.Range (0, 2);
		currentState = flipACoin == 0 ? RobotStates.STATE_HOMICIDAL : RobotStates.STATE_SUICIDAL;
		PlaySingleSoundFx (breakdownZapSound);
		displayEmotionTime = Time.time + displayEmotionDelay;
		SetTarget (null);
	}

	private void UpdateVisuals() {
		switch (currentState) {
			case RobotStates.STATE_FINDBOX:
				stateSpeedMultiplier = 1.0f;
				spriteRenderer.color = Color.white;
				currentSpeech.sprite = happySpeechSprite;
				line.colorGradient = lockedByPlayer ? line.colorGradient 
													: GameManager.instance.blueWaveGradient;
				break;
			case RobotStates.STATE_SUICIDAL:
				stateSpeedMultiplier = 0.5f;
				spriteRenderer.color = Color.cyan;
				currentSpeech.sprite = suidicalSpeechSprite;
				line.colorGradient = lockedByPlayer ? line.colorGradient 
													: GameManager.instance.greenWaveGradient;				
				break;
			case RobotStates.STATE_HOMICIDAL:
				stateSpeedMultiplier = 1.5f;
				spriteRenderer.color = Color.red;
				currentSpeech.sprite = homicidalSpeechSprite;
				line.colorGradient = lockedByPlayer ? line.colorGradient 
													: GameManager.instance.redWaveGradient;
				break;
		}

		if (onFire && !grabbedByPlayer)
			currentSpeech.sprite = onFireSpeechSprite;

		emotionButton.interactable = grabbedByPlayer && !RobotGrabber.instance.isRobotBeingDragged;
		currentSpeech.enabled = (currentState != RobotStates.STATE_FINDBOX && Time.time < displayEmotionTime) || onFire || emotionButton.interactable;

		// FIXME(~): hardcoded magic number (1.5) determined emperically
		if (emotionButton.interactable) {
			currentSpeech.transform.localScale = 1.5f * Vector3.one;
			if (!buttonGlow.isPlaying)
				buttonGlow.Play ();
		} else {
			currentSpeech.transform.localScale = Vector3.one;
			if (buttonGlow.isPlaying) {
				buttonGlow.Stop ();
				buttonGlow.Clear ();
			}
		}
		observationCircle.UpdateVisuals (grounded && !fellInPit);
	}

	public bool EmotionButtonContainsScreenPoint(Vector3 screenPoint) {
		return RectTransformUtility.RectangleContainsScreenPoint (emotionButtonRect, screenPoint, Camera.main);
	}

	void SearchForTarget() {
		if (!grounded || isBeingCarried || fellInPit || hasDrawnPath || !grid.NodeFromWorldPoint(transform.position).walkable || GameManager.instance.levelEnded || freakoutCoroutine != null) {
			StopMoving ();
			return;
		}

		CheckIfTargetLost ();
			
		if (target != null || path != null) {
			FollowPath ();
			return;
		}

		switch (currentState) {
			case RobotStates.STATE_FINDBOX:
				SetTarget (isDelivering ? GameManager.instance.GetClosestDeliveryTarget (this)
										: GameManager.instance.GetClosestBoxTarget (this));
				break;
			case RobotStates.STATE_HOMICIDAL:
				if (isCarryingBox)
					SetTarget (GameManager.instance.GetClosestDeliveryTarget (this));
				else
					SetTarget (isDelivering ? GameManager.instance.GetRandomHazardTarget ()
											: GameManager.instance.GetRandomRobotTarget (this));
				break;
			case RobotStates.STATE_SUICIDAL:
				if (isCarryingBox)
					SetTarget (GameManager.instance.GetClosestDeliveryTarget (this));
				else
					SetTarget (GameManager.instance.GetClosestHazardTarget (this));
				break;
		}

		if (target == null) {
			Vector3 position = GameManager.instance.GetRandomWorldPointTarget (this);
			if (dummyTarget == null)
				dummyTarget = Instantiate<GameObject> (dummyTargetPrefab, position, Quaternion.identity, GameManager.instance.robotParent);
			else
				dummyTarget.transform.position = position;
			target = dummyTarget.transform;
		}
	}

	public void SetTarget(Transform newTarget) {
		// free the old target (if any)
		if (isTargetThrowable) 
			target.GetComponent<Throwable> ().SetTargeter (null);
		
		target = newTarget;
		if (isTargetThrowable) 
			target.GetComponent<Throwable> ().SetTargeter (this);
	}

	public void OnPathFound(Vector3[] newPath, bool pathSuccessful, int subPathIndex) {
		if (pathSuccessful) {
			if (subPathIndex != PathRequestManager.AUTO_PATH) {	// user-defined sub-path
				if (subPathIndex < lowestFailedSubPathIndex)
					subPaths.Add (subPathIndex, newPath);
			} else {											// regular auto-path
				path = newPath;
				InitPath ();
			}
		} else {
			if (subPathIndex != PathRequestManager.AUTO_PATH && subPathIndex < lowestFailedSubPathIndex) {
				lowestFailedSubPathIndex = subPathIndex;
				PathRequestManager.KillPathRequests (name, subPathIndex + 1);
			} else if (subPathIndex == PathRequestManager.AUTO_PATH) {
				StopMoving ();
			}
		}

		if (subPaths.Count > 0) {
			if (PathRequestManager.PathRequestsRemaining(name) <= 0) {
				RemoveCutoffSubPaths ();
				path = pathFinder.MergeSubPaths (subPaths);
				InitPath ();
			}
		}
	}

	private void RemoveCutoffSubPaths() {
		for (int i = lowestFailedSubPathIndex; i <= subPaths.Count; i++) {
			if (subPaths.ContainsKey (i))
				subPaths.Remove (i);
		}
	}

	private void InitPath () {
		ClearDrawnPath ();
		if (path.Length > 0) {
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
	}

	public void ClearDrawnPath() {
		if (this == null || gameObject == null)		// BUGFIX: for odd null exception using gameObject.name here as a Robot gets destroyed
			return;
		PathRequestManager.KillPathRequests(name);
		lowestFailedSubPathIndex = int.MaxValue;
		currentDrawnPathLength = 0.0f;
		oldDrawnPathCount = 0;
		drawnPath.Clear ();
		subPaths.Clear ();
	}
		
	public void TryAddPathPoint (Vector3 worldPosition) {
		GridNode node = grid.NodeFromWorldPoint (worldPosition);
		if (node.walkable && !drawnPath.Contains (node)) {
			if (currentDrawnPathLength < maxDrawnPathLength) {
				if (drawnPath.Count > 1)
					currentDrawnPathLength += Vector3.Distance(node.worldPosition, drawnPath [drawnPath.Count - 1].worldPosition);
				drawnPath.Add (node);
			}
		}
		if (target != null)
			SetTarget (null);
	}
		
	public bool FinishDrawingPath() {
		bool longEnoughPath = currentDrawnPathLength > minDrawnPathLength;
		if (longEnoughPath)
			OptimizeDrawnPath ();
		else
			ClearDrawnPath ();
		return longEnoughPath;
	}

	// this was originally in the PathFinding class
	// but placing it here reduces the function call overhead a bit
	private void OptimizeDrawnPath() {
		List<Vector3> waypoints = new List<Vector3> ();
		waypoints.Add (drawnPath [0].worldPosition);						// always keep the first position the user wanted
		for (int i = pathSmoothingInterval; i < drawnPath.Count - 1; i+=pathSmoothingInterval)
			waypoints.Add (drawnPath [i].worldPosition);
		waypoints.Add (drawnPath [drawnPath.Count - 1].worldPosition);		// always keep the final position the user wanted

		int subPathIndex = 0;
		for (int i = 1; i < waypoints.Count; i++) {
			PathRequestManager.RequestPath (name, waypoints [i - 1], waypoints [i], OnPathFound, subPathIndex, i == (waypoints.Count - 1));
			subPathIndex++;
		}
	}

	void UpdatePath(bool freshStart) {
		if (!waitingForPathRequestResults && (freshStart || (target != null && (target.position - targetLastKnownPosition).sqrMagnitude > pathUpdateMoveThreshold))) {
			PathRequestManager.RequestPath (name, transform.position, target.position, OnPathFound);
			targetLastKnownPosition = target.position;
		} 
	}

	void FollowPath() {
		UpdatePath (path == null && !hasDrawnPath);
		if (path == null || path.Length == 0)
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

		if (!haltAndCommand)
			transform.position = Vector3.MoveTowards (transform.position, currentWaypoint, speed * stateSpeedMultiplier * percentSpeed * Time.deltaTime);
	}

	void CheckWaypointProximity () {
		if (path == null || path.Length == 0)
			return;
		
		if (circleCollider.OverlapPoint (currentWaypoint)) {
			targetIndex++;
			if (targetIndex >= path.Length) {
				StopMoving ();
				return;
			}
			currentWaypoint = path [targetIndex];
		}
	}

	private void UpdatePathLine() {
		line.enabled = ((lockedByPlayer || waitingForPathRequestResults) && hasDrawnPath) || (path != null && path.Length > 0);
		if (!line.enabled)
			return;

		if (lockedByPlayer || waitingForPathRequestResults) {
			if (drawnPath.Count > oldDrawnPathCount)
				line.colorGradient = currentDrawnPathLength > minDrawnPathLength ? GameManager.instance.silverWaveGradient 
																				 : GameManager.instance.blackWaveGradient;
			oldDrawnPathCount = drawnPath.Count;
			line.positionCount = drawnPath.Count + 1;
			line.SetPosition(0, transform.position);
			for (int i = 0, pos = 1; i < drawnPath.Count; i++, pos++)
				line.SetPosition (pos, drawnPath [i].worldPosition);
			
		} else {
			line.positionCount = (path.Length - targetIndex) + 1;
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
	}

	public bool isCarryingItem {
		get { 
			if ((carriedItem != null && carriedItem.GetCarrier () != this) || (wasCarryingItem && carriedItem == null)) {
				carriedItem = null;
				StopDelivering ();
			}
			return (wasCarryingItem = carriedItem != null);
		}
	}

	public bool isCarryingBox {
		get { 
			return isCarryingItem && (carriedItem is Box);
		}
	}

	public bool isCarryingRobot {
		get { 
			return isCarryingItem && (carriedItem is Robot);
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
				if (toGrab.tag == "Box")
					GrabItem (toGrab);
				else if (toGrab.tag == "Robot" && currentState == RobotStates.STATE_HOMICIDAL && !(toGrab as Robot).lockedByPlayer)
					GrabItem (toGrab);
			}
		}

		if (collision.collider.tag == "Robot" && collision.collider.GetComponent<Robot> ().onFire)
			onFire = true;
	}

	// slide along collisions
	void OnCollisionStay2D (Collision2D collision) {
		if (path != null && path.Length > 0) {
			foreach (ContactPoint2D contact in collision.contacts) {
				Vector3 vel3D = currentWaypoint - transform.position;
				Vector2 velocity = new Vector2 (vel3D.x, vel3D.y);
				Vector2 rightTangent = new Vector2 (contact.normal.y, -contact.normal.x);
				float dot = Vector2.Dot (rightTangent, velocity);
				if (dot < 0.0f)
					dot = -1.0f;
				else if (dot > 0.0f)
					dot = 1.0f;

				// FIXME(~): robots headed dead-into a flat wall will get stuck there (but the player can correct it)
				rb2D.AddForceAtPosition (-500.0f * contact.separation * dot * rightTangent, contact.point, ForceMode2D.Force);
			}
			CheckWaypointProximity ();
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
		if (!fellInPit)		// BUGFIX: for the landing sound overwriting status change sounds
			PlayRandomSoundFx (landingSounds);
	}
}
