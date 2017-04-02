using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwable : MonoBehaviour {

	public LayerMask 			airStrikeMask;
	public LayerMask			groundStrikeMask;
	public float 				groundedDrag = 10.0f;
	public float 				deadlyHeight;
	public GameObject 			dropShadowPrefab;

	protected GameObject		dropShadow;
	protected ShadowController 	shadowController;
	protected Rigidbody2D 		rb2D;
	protected bool 				grounded;
	protected float 			oldShadowOffset;

	void Awake() {
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
	}

	void Start () {
		rb2D = GetComponent<Rigidbody2D> ();
		shadowController = GetComponentInChildren<ShadowController> ();
	}

	// TODO: use negative airTime... to ignore parbolic flight... maybe
	public void Throw(float speed, float airTime) {
		// specific to the box being thrown from the box thrower
		// the grabbed and tossed robot will behave differently (ie set an held at a specific height, then dropped straight down, or tossed in some random direction, horizontally NOT FULL PARABOLIC)

		shadowController.SetVelocity(Vector3.forward * speed);
		shadowController.SetKinematic (false);
		shadowController.SetTrajectory (airTime);		// TODO: check for negative or zero airTime, maybe
		grounded = false; 
	}

	void Update() {
		grounded = shadowController.grounded;
		if (!grounded) {
			rb2D.gravityScale = 1.0f;
			rb2D.drag = 0.0f;
			dropShadow.SetActive(true);

			float shadowOffset = shadowController.GetShadowOffset ();

			// set the shadow position
			dropShadow.transform.position = transform.position - Vector3.up * shadowOffset;

			// coming in for a landing
			if (shadowOffset < oldShadowOffset && shadowOffset < deadlyHeight)
				gameObject.layer = (int)Mathf.Log (groundStrikeMask, 2);

			oldShadowOffset = shadowOffset;
		} else {
			rb2D.gravityScale = 0.0f;
			rb2D.drag = groundedDrag;
			rb2D.velocity = Vector2.zero;
			dropShadow.SetActive(false);
			print ("HIT");
		}
	}
}
