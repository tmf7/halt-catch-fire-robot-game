using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Throwable {

	public GameObject 		explosionPrefab;

/*
	public LayerMask 		airStrikeMask;
	public LayerMask		groundStrikeMask;
	public float 			groundedDrag = 10.0f;
	public float 			deadlyHeight;
	public GameObject 		dropShadowPrefab;


	private GameObject		dropShadow;
	private ShadowController shadowController;
	private Rigidbody2D 	boxRB;
	private bool 			grounded;
	private float 			oldShadowOffset;

	void Awake() {
		// FIXME: non-zero parent is screwing with the calculations (do not parent it to the box, it amplifies the velocity/movement)
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
	}

	void Start () {
		boxRB = GetComponent<Rigidbody2D> ();
		shadowController = GetComponentInChildren<ShadowController> ();
		shadowController.SetVelocity(Vector3.forward * boxRB.velocity.y);
		shadowController.SetKinematic (false);
		shadowController.SetTrajectory (airTime);
		grounded = false; 
	}

	void Update() {
		if (grounded)
			return;

		float shadowOffset = shadowController.GetShadowOffset ();
		if (shadowOffset > 0.0f) {
			dropShadow.transform.position = transform.position - Vector3.up * shadowOffset;
		}
			
		// coming in for a landing
		if (shadowOffset < oldShadowOffset && shadowOffset < deadlyHeight)
			gameObject.layer = (int)Mathf.Log (groundStrikeMask, 2);
		
		oldShadowOffset = shadowOffset;
		grounded = shadowController.grounded;

		if (grounded) {
			boxRB.gravityScale = 0;
			boxRB.drag = groundedDrag;
			boxRB.velocity = Vector2.zero;
			dropShadow.SetActive(false);
			print ("HIT");
		}
	}

	void OnTriggerEnter2D(Collider2D collider) {
//		print ("SHADOW FOUND");
	}
*/

	void Update () {
		UpdateShadow ();	
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (!grounded && collision.gameObject.layer == Mathf.Log(airStrikeMask.value,2)) {
			ExplodeBox ();
		}
	}

	public void ExplodeBox() {
		Instantiate<GameObject> (explosionPrefab, transform.position, Quaternion.identity);
		RemoveBox();
	}

	void RemoveBox() {
		BoxThrower.allBoxes.Remove (this);		// FIXME: dont have BoxThrower manage the list of all boxes
		Destroy(shadowController);				// FIXME: may also need to destroy its script instance (hopefully not)
		Destroy(dropShadow);
		Destroy(gameObject);
		Destroy (this);
	}
}
