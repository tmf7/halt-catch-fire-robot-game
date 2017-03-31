using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : MonoBehaviour {

	public GameObject 		dropShadowPrefab;
	
	[HideInInspector]
	public float 			airTime;

	private GameObject		dropShadow;
	private Vector3 		shadowVelocity;
	private BoxCollider2D 	boxCollider;
	private Rigidbody2D 	boxRB;
	private float			strikeTime;
	private bool 			grounded;

	void Awake() {
		// FIXME: non-zero parent is screwing with the calculations (do not parent it to the box, it amplifies the velocity/movement)
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
	}

	void Start () {
		boxCollider = GetComponent<BoxCollider2D> ();
		boxCollider.enabled = false;
		boxRB = GetComponent<Rigidbody2D> ();

		// predicted landing point
		float xFinal = transform.position.x + (boxRB.velocity.x * airTime);
		float yFinal = transform.position.y + (boxRB.velocity.y * airTime) + (0.5f * Physics2D.gravity.y * airTime * airTime);

		// ground movement of the drop shadow
		Vector3 shadowDir = new Vector3 (xFinal, yFinal, 0.0f) - transform.position;
		float distance = shadowDir.magnitude;
		shadowDir.Normalize ();
		float shadowSpeed = distance / airTime;
		shadowVelocity = new Vector3 (shadowSpeed * shadowDir.x, shadowSpeed * shadowDir.y, 0.0f);

		strikeTime = Time.time + airTime;
		grounded = false; 
	}

	void Update() {
		if (grounded)
			return;

		dropShadow.transform.position += shadowVelocity * Time.deltaTime;

		if (Time.time > strikeTime) {
			grounded = true;
			boxRB.gravityScale = 0;
			boxRB.velocity = Vector2.zero;
			boxRB.isKinematic = true;
		}
	}

	// FIXME: they start off touching, only enable on the return trip
	void OnTriggerEnter2D(Collider2D collider) {
		if (collider.tag == "Shadow")
			dropShadow.SetActive(false);//boxCollider.enabled = true;
	}
}
