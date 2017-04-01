using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : MonoBehaviour {

	public LayerMask 		airStrikeMask;
	public float 			groundedDrag = 10.0f;
	public float 			deadlyHeight;
	public GameObject 		dropShadowPrefab;
	public GameObject 		explosionPrefab;
	
	private GameObject		dropShadow;
	private ShadowController shadowController;
	private Rigidbody2D 	boxRB;
	private float			strikeTime;
	private bool 			grounded;

	void Awake() {
		// FIXME: non-zero parent is screwing with the calculations (do not parent it to the box, it amplifies the velocity/movement)
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
	}

	void Start () {
		boxRB = GetComponent<Rigidbody2D> ();
		shadowController = GetComponentInChildren<ShadowController> ();
		shadowController.SetVelocity(Vector3.forward * boxRB.velocity.y);
		shadowController.SetKinematic (false);
		grounded = false; 
	}

	void Update() {
		if (grounded)
			return;

		float shadowOffset = shadowController.GetShadowOffset ();
		dropShadow.transform.position = transform.position - Vector3.up * shadowOffset;

		if (shadowController.IsFalling()  && shadowOffset < deadlyHeight)	// && !shadowBox.IsTouching(boxCollider)
			gameObject.layer = LayerMask.NameToLayer ("DynamicCollision");

		grounded = shadowController.IsGrounded();
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

	void OnCollisionEnter2D(Collision2D collision) {
		if (!grounded && collision.gameObject.layer == Mathf.Log(airStrikeMask.value,2)) {
			ExplodeBox ();
		}
	}
/*
	void CalculateShadowTrajectory() {
		// predicted landing point
		float xFinal = transform.position.x + (boxRB.velocity.x * airTime);
		float yFinal = transform.position.y + (boxRB.velocity.y * airTime) + (0.5f * Physics2D.gravity.y * airTime * airTime);

		// ground movement of the drop shadow
		Vector3 shadowDir = new Vector3 (xFinal, yFinal) - transform.position;
		float distance = shadowDir.magnitude;
		shadowDir.Normalize ();
		float shadowSpeed = distance / airTime;
		shadowVelocity = new Vector3 (shadowSpeed * shadowDir.x, shadowSpeed * shadowDir.y);
	}
*/
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
