using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Throwable : MonoBehaviour {

	public LayerMask 			airStrikeMask;
	public LayerMask			groundedResetMask;
	public float 				groundedDrag = 10.0f;
	public float 				deadlyHeight;
	public GameObject 			dropShadowPrefab;

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
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
		shadowController = GetComponentInChildren<ShadowController> ();
		spriteRenderer = GetComponent<SpriteRenderer> ();
		rb2D = GetComponent<Rigidbody2D> ();
	}

	public void SetShadowParent(Transform parent) {
		dropShadow.transform.SetParent (parent);
	}

	// TODO: use negative airTime to ignore trajectory
	public void Throw(float verticalSpeed, float airTime) {
		// specific to the box being thrown from the box thrower
		// the grabbed and tossed robot will behave differently (ie set an held at a specific height, then dropped straight down, or tossed in some random direction, horizontally NOT FULL PARABOLIC)

		shadowController.SetVelocity(Vector3.forward * verticalSpeed);
		shadowController.SetKinematic (false);
		shadowController.SetTrajectory (airTime);
	}

	public void SetHeight(float height) {
		shadowController.SetHeight (height);
	}

	public bool grounded {
		get {
			return shadowController.grounded;
		}
	}

	// must be defined by inherited classes Robot and Box
	protected abstract void OnLanding ();

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
			gameObject.layer = (int)Mathf.Log (groundedResetMask, 2);
			spriteRenderer.sortingLayerName = "Units";
			rb2D.transform.rotation = Quaternion.identity;
			rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;
			rb2D.gravityScale = 0.0f;
			rb2D.drag = groundedDrag;
			rb2D.velocity = Vector2.zero;
			dropShadow.SetActive(false);
			OnLanding ();
		}
	}
}
