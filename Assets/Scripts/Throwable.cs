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

	public AudioSource 			efxSource;
	public AudioClip[]			thrownSounds;
	public AudioClip[]			landingSounds;
	public AudioClip[]			explodeSounds;

	public LayerMask 			airStrikeMask;
	public LayerMask			groundedResetMask;
	public float 				groundedDrag = 10.0f;
	public float 				deadlyHeight;
	public GameObject 			dropShadowPrefab;
	public GameObject 			explosionPrefab;
	public Range 				throwSpeeds = new Range(8.0f, 12.0f);
	public Range 				throwAnglesDeg = new Range (30.0f, 150.0f);
	public Range				airTimes = new Range(0.5f, 2.0f);

	[HideInInspector] 
	public Vector3				dropForce;
	[HideInInspector]
	public bool 				grabbed = false;

	protected GameObject		dropShadow;
	protected ShadowController 	shadowController;
	protected SpriteRenderer	spriteRenderer;
	protected Rigidbody2D 		rb2D;
	protected float 			oldShadowOffset = 0.0f;
	protected bool 				landingResolved;

	void Awake() {
		efxSource = GetComponent<AudioSource> ();
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
		shadowController = GetComponentInChildren<ShadowController> ();
		spriteRenderer = GetComponent<SpriteRenderer> ();
		rb2D = GetComponent<Rigidbody2D> ();
	}

	public void SetShadowParent(Transform parent) {
		dropShadow.transform.SetParent (parent);
	}

	// negative airTime to ignores trajectory of a parabola
	public void Throw(float verticalSpeed, float airTime) {
		shadowController.SetVelocity(Vector3.forward * verticalSpeed);
		shadowController.SetKinematic (false);
		shadowController.SetTrajectory (airTime);
		PlayRandomSoundFx (thrownSounds);
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
			if (shadowOffset < oldShadowOffset && shadowOffset < deadlyHeight)
				gameObject.layer = (int)Mathf.Log (groundedResetMask, 2);

			oldShadowOffset = shadowOffset;
		} else if (!landingResolved) {
			landingResolved = true;
			OnLanding ();
		}
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
		HitTrigger2D (collider);
	}

	protected void PlaySingleSoundFx (AudioClip clip) {
		efxSource.clip = clip;
		efxSource.Play ();
	}

	protected void PlayRandomSoundFx(params AudioClip [] clips) {
		int randomIndex = Random.Range (0, clips.Length - 1);
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
		PlayRandomSoundFx (landingSounds);
	}

	protected abstract void HitTrigger2D (Collider2D collider);
}
