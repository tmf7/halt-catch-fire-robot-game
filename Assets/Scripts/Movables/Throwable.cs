using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

[Serializable]
public class Range {
	public float minimum;
	public float maximum;

	public Range (float min, float max) {
		minimum = min;
		maximum = max;
	}
}

public abstract class Throwable : MonoBehaviour {

	public AudioClip[]			landingSounds;

	public LayerMask			groundedResetMask;
	public float 				groundedDrag = 10.0f;
	public float 				deadlyHeight = 0.125f;
	public GameObject 			dropShadowPrefab;
	public GameObject 			explosionPrefab;
	public AnimationCurve 		throwThrottleCurve;
	public Range 				throwSpeeds = new Range(8.0f, 12.0f);
	public Range				airTimes = new Range(0.5f, 2.0f);
	public int 					throwSectorCount = 10;
	public float				smallestPitfallScale = 0.1f;
	public float				pitfallRate = 0.9f;
	public float 				currentPitfallScale = 1.0f;		// transform.localScale

	[HideInInspector] 
	public Vector3				dropForce;

	protected ParticleSystem	landingParticles;
	protected ParticleSystem 	robotBeam;
	protected AudioSource 		efxSource;
	protected GameObject		dropShadow;
	protected SpriteRenderer	spriteRenderer;
	protected Rigidbody2D 		rb2D;
	protected Vector3 			oldPosition;
	protected bool 				landingResolved = true;

	private ShadowController 	shadowController;
	private Robot				whoIsCarrying;
	private Robot				whoHasTargeted;
	private int 				previousSectorIndex = 0;

	private static List<Range>	  throwSectors;
	private static readonly float maxThrowThrottleFactor = 0.0625f;
	private static readonly float thresholdAngle = 180.0f * Mathf.Deg2Rad;

	void Awake() {
		efxSource = GetComponent<AudioSource> ();
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
		dropShadow.SetActive (false);
		shadowController = GetComponentInChildren<ShadowController> ();
		spriteRenderer = GetComponent<SpriteRenderer> ();
		rb2D = GetComponent<Rigidbody2D> ();
		landingParticles = GetComponentInChildren<ParticleSystem> ();		// the landing particle system must be the first child of the throwable gameobject for this to work

		if (Throwable.throwSectors == null) {
			Throwable.throwSectors = new List<Range> ();
			float throwSectorAngle = 359.9375f / throwSectorCount;
			for (int i = 0; i < throwSectorCount; i++) {
				float currentMinAngle = i * throwSectorAngle;
				Throwable.throwSectors.Add (new Range (currentMinAngle, currentMinAngle + throwSectorAngle));
			}
		}
		oldPosition = transform.position;
	}

	void LateUpdate() {
		oldPosition = transform.position;
	}

	public void SetShadowParent(Transform parent) {
		dropShadow.transform.SetParent (parent);
	}
		
	// negative airTime to ignores trajectory of a parabola
	public void Throw(float verticalSpeed, float airTime) {
		shadowController.SetVelocity(Vector3.forward * verticalSpeed);
		shadowController.SetKinematic (false);
		shadowController.SetTrajectory (airTime);
	}

	public void RandomThrow() {
		float airTime = Random.Range (airTimes.minimum, airTimes.maximum);
		float throwSpeed = Random.Range (throwSpeeds.minimum, throwSpeeds.maximum);

		// dont throw in the same general direction twice in a row
		int sectorIndex = 0;
		while ((sectorIndex = Random.Range(0, throwSectorCount)) == previousSectorIndex)
			;
		
		previousSectorIndex = sectorIndex;
		float throwAngle = Random.Range (Throwable.throwSectors[sectorIndex].minimum, Throwable.throwSectors[sectorIndex].maximum);
		throwAngle *= Mathf.Deg2Rad;
		Vector2 throwDir = new Vector2 (Mathf.Cos (throwAngle), Mathf.Sin (throwAngle));

		// don't give the throwable more downward speed than it needs
		if (throwAngle > thresholdAngle) {
			float curveTime = Mathf.InverseLerp(Mathf.PI, 2 * Mathf.PI, throwAngle);		// 0 to 1
			float lerpFactor = throwThrottleCurve.Evaluate (curveTime);						// 1 to 0 to 1
			float throttleFactor = Mathf.Lerp(maxThrowThrottleFactor, 1.0f, lerpFactor); 	// 1 to 0.0625 to 1
			throwSpeed *= throttleFactor;			
		}

		rb2D.velocity = throwSpeed * throwDir;
		SetHeight (2.0f * deadlyHeight);
		Throw (rb2D.velocity.y, airTime);
	}

	public void SetHeight(float height) {
		shadowController.SetHeight (height);
	}
		
	public bool grounded {
		get {
			return shadowController.grounded;
		}
	}
		
	public bool isFalling {
		get { 
			return shadowController.isFalling;
		}
	}
		
	public bool fellInPit {
		get { 
			return currentPitfallScale < 1.0f;
		}
	}

	public bool isBeingCarried {
		get {
			return whoIsCarrying != null;
		} 
	}

	public bool isTargeted {
		get { 
			return whoHasTargeted != null;
		}
	}

	public void SetTargeter(Robot newTargeter) {
		whoHasTargeted = newTargeter;
	}

	public Robot GetTargeter() {
		return whoHasTargeted;
	}

	public void SetCarrier(Robot newCarrier) {
		if (newCarrier != null && this is Robot) {

			// only scream the first time its grabbed by a homicidal robot
			if (whoIsCarrying == null)
				PlaySingleSoundFx ((this as Robot).robotGrabbedSound);
			
			if ((this as Robot).isCarryingItem)
				(this as Robot).DropItem ();
		}
		whoHasTargeted = newCarrier;
		whoIsCarrying = newCarrier;
	}

	public Robot GetCarrier() {
		return whoIsCarrying;
	}

	protected void UpdateRobotBeam() {
		if (robotBeam != null) {
			if (!isBeingCarried) {
				SetKinematic (false);
				Destroy (robotBeam.gameObject);
			}
		}
	}

	public void ActivateRobotBeam(ParticleSystem _robotBeam) {
		if (robotBeam != null)
			Destroy (robotBeam);
		robotBeam = _robotBeam;
	}
		
	public void SetKinematic(bool isKinematic) {
		shadowController.SetKinematic (isKinematic);
		rb2D.isKinematic = isKinematic;
		rb2D.transform.rotation = Quaternion.identity;
	}


	protected void UpdateShadow() {
		if (!grounded) {
			landingResolved = false;
			rb2D.gravityScale = 1.0f;
			rb2D.drag = 0.0f;
			dropShadow.SetActive(true);
			gameObject.layer = LayerMask.NameToLayer ("Flying");
			spriteRenderer.sortingLayerName = "Flying";

			float shadowOffset = shadowController.GetShadowOffset ();

			// set the shadow position
			dropShadow.transform.position = transform.position + Vector3.down * shadowOffset;

			// coming in for a landing
			if (isFalling && shadowOffset < deadlyHeight)
				gameObject.layer = (int)Mathf.Log (groundedResetMask, 2);

		} else if (!landingResolved) {
			landingResolved = true;
			OnLanding ();
		}
		UpdateRobotBeam ();
	}

	IEnumerator FallingDownPit() {
		while (currentPitfallScale > smallestPitfallScale) {
			currentPitfallScale *= pitfallRate;
			transform.localScale = new Vector3 (currentPitfallScale, currentPitfallScale);
			efxSource.volume = currentPitfallScale;
			yield return null;
		}

		if (this is Robot)
			(this as Robot).howDied = RobotNames.MethodOfDeath.DEATH_BY_PIT;

		Remove();
	}

	// stop the rb2D and apply a force to the rb3D so it lands quicker
	public void HitWall() {
		shadowController.SuddenDrop ();
	}

	public void Explode() {
		Instantiate<GameObject> (explosionPrefab, transform.position, Quaternion.identity);
		Remove();
	}

	public void Remove() {
		if (this is Robot) {
			(this as Robot).DropItem ();
			RobotNames.Instance.AddRobotSurvivalTime(name, Time.time - (this as Robot).spawnTime, true, (this as Robot).howDied);
			PathRequestManager.KillPathRequests (name);
		}
		
		GameManager.instance.Remove (this);
		Destroy(shadowController);
		Destroy(dropShadow);
		Destroy(gameObject);
	}

	void OnTriggerEnter2D(Collider2D hitTrigger) {
		if (hitTrigger.tag == "Pit" && !fellInPit)
			StartCoroutine ("FallingDownPit");

		// tell the carrier to drop this
		if (isBeingCarried && whoIsCarrying.CheckHitTarget(hitTrigger.tag))
				whoIsCarrying.DropItem();

		HitTrigger2D (hitTrigger);
	}

	void OnCollisionEnter2D(Collision2D collision)  {
		if (!grounded)
			HitWall ();

		// tell the carrier to drop this
		if (isBeingCarried && whoIsCarrying.CheckHitTarget(collision.collider.tag))
			whoIsCarrying.DropItem();

		HitCollision2D (collision);
	}

	void OnCollisionStay2D (Collision2D collision) {
		foreach (ContactPoint2D contact in collision.contacts) {
			Vector2 tangent = new Vector2 (contact.normal.y, -contact.normal.x);
			rb2D.AddForceAtPosition (-2.0f * contact.separation * tangent, contact.point, ForceMode2D.Force);
		}
	}

	public void PlaySingleSoundFx (AudioClip clip) {
		float pitch = Random.Range (0.95f, 1.05f);
		efxSource.pitch = pitch;
		efxSource.clip = clip;
		efxSource.Play ();
	}

	public void PlayRandomSoundFx(params AudioClip [] clips) {
		int randomIndex = Random.Range (0, clips.Length);
		float pitch = Random.Range (0.95f, 1.05f);
		efxSource.pitch = pitch;
		efxSource.clip = clips [randomIndex];
		efxSource.Play ();
	}

	// these must be defined by inherited classes Robot and Box
	protected virtual void OnLanding () {
		gameObject.layer = (int)Mathf.Log (groundedResetMask, 2);
		spriteRenderer.sortingLayerName = "Units";
		rb2D.transform.rotation = Quaternion.identity;
		rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;
		rb2D.gravityScale = 0.0f;
		rb2D.drag = groundedDrag;
		rb2D.velocity = Vector2.zero;
		dropShadow.SetActive(false);
		landingParticles.Play ();
	}

	protected abstract void HitTrigger2D (Collider2D collider);
	protected abstract void HitCollision2D(Collision2D collision);
}
