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
	public float 				deadlyHeight;
	public GameObject 			dropShadowPrefab;
	public GameObject 			explosionPrefab;
	public Range 				throwSpeeds = new Range(8.0f, 12.0f);
	public Range 				throwAnglesDeg = new Range (30.0f, 150.0f);
	public Range				airTimes = new Range(0.5f, 2.0f);
	public float				smallestPitfallScale = 0.1f;
	public float				pitfallRate = 0.9f;

	[HideInInspector]
	public bool isClaimed {		// already grabbed or will be grabbed
		get {
			return whoClaimed != null;
		} 
	}  			
		
	[HideInInspector] 
	public Vector3				dropForce;
	[HideInInspector]
	public bool 				grabbed = false;

	protected ParticleSystem	landingParticles;
	protected AudioSource 		efxSource;
	protected GameObject		dropShadow;
	protected SpriteRenderer	spriteRenderer;
	protected Rigidbody2D 		rb2D;
	protected bool 				landingResolved;
	protected float 			currentPitfallScale = 1.0f;		// transform.localScale

	private ShadowController 	shadowController;
	private GameObject			whoClaimed;

	void Awake() {
		efxSource = GetComponent<AudioSource> ();
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
		shadowController = GetComponentInChildren<ShadowController> ();
		spriteRenderer = GetComponent<SpriteRenderer> ();
		rb2D = GetComponent<Rigidbody2D> ();
		landingParticles = GetComponentInChildren<ParticleSystem> ();		// the landing particle system must be the first child of the throwable gameobject for this to work
	}

	public void SetClaimant(GameObject newClaimant) {
		whoClaimed = newClaimant;
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
		float throwSpeed = Random.Range (throwSpeeds.minimum, throwSpeeds.maximum);
		float throwAngle = Random.Range (throwAnglesDeg.minimum * Mathf.Deg2Rad, throwAnglesDeg.maximum * Mathf.Deg2Rad);
		float airTime = Random.Range (airTimes.minimum, airTimes.maximum);
		rb2D.velocity = new Vector2 (throwSpeed * Mathf.Cos (throwAngle), throwSpeed * Mathf.Sin (throwAngle));
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
			dropShadow.transform.position = transform.position - Vector3.up * shadowOffset;

			// coming in for a landing
			// possibly FIXME: I moved the isFalling logic to the shadowController class and made the shadowController a private member of Throwable
			if (isFalling && shadowOffset < deadlyHeight)
				gameObject.layer = (int)Mathf.Log (groundedResetMask, 2);
			
		} else if (!landingResolved) {
			landingResolved = true;
			OnLanding ();
		}
	}

	IEnumerator FallingDownPit() {
		while (currentPitfallScale > smallestPitfallScale) {
			currentPitfallScale *= pitfallRate;
			transform.localScale = new Vector3 (currentPitfallScale, currentPitfallScale);
			efxSource.volume = currentPitfallScale;
			yield return null;
		}
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

	protected void Remove() {
		GameManager.instance.Remove (this);
		Destroy(shadowController);				// FIXME: may also need to destroy its script instance (hopefully not)
		Destroy(dropShadow);
		Destroy(gameObject);
		Destroy (this);
	}

	void OnTriggerEnter2D(Collider2D collider) {
		if (collider.tag == "Pit" && !fellInPit)
			StartCoroutine ("FallingDownPit");
		HitTrigger2D (collider);
	}

	public void PlaySingleSoundFx (AudioClip clip) {
		efxSource.clip = clip;
		efxSource.Play ();
	}

	public void PlayRandomSoundFx(params AudioClip [] clips) {
		int randomIndex = Random.Range (0, clips.Length);
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
}
