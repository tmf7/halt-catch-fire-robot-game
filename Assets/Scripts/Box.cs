using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : MonoBehaviour {

	public float 			strikeHeight;
	public GameObject 		dropShadowPrefab;
	public GameObject 		explosionPrefab;
	
	[HideInInspector]
	public float 			airTime;

	private GameObject		dropShadow;
	private Vector3 		shadowVelocity;
	private Rigidbody2D 	boxRB;
	private float			strikeTime;
	private float 			strikeHeightSqr;
	private bool 			grounded;

	void Awake() {
		// FIXME: non-zero parent is screwing with the calculations (do not parent it to the box, it amplifies the velocity/movement)
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
		strikeHeightSqr = strikeHeight * strikeHeight;
	}

	void Start () {
		boxRB = GetComponent<Rigidbody2D> ();
		CalculateShadowTrajectory ();
		strikeTime = Time.time + airTime;
		grounded = false; 
	}

	void Update() {
		if (grounded)
			return;

		dropShadow.transform.position += shadowVelocity * Time.deltaTime;
		airTime -= Time.deltaTime;

		Vector3 shadowToBox = transform.position - dropShadow.transform.position;
		float heightSqr = shadowToBox.sqrMagnitude;
		if (boxRB.velocity.y <= 0.0f && heightSqr < strikeHeightSqr)
			gameObject.layer = LayerMask.NameToLayer ("Collision");

		if (Time.time > strikeTime) {
			grounded = true;
			boxRB.gravityScale = 0;
			boxRB.velocity = Vector2.zero;
		//	boxRB.isKinematic = true;
			dropShadow.SetActive(false);
		}
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (!grounded) {
			Instantiate<GameObject> (explosionPrefab, transform.position, Quaternion.identity);
			RemoveBox();
		}
	}

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

	void RemoveBox() {
		Destroy(dropShadow);
		Destroy(gameObject);
		Destroy (this);
	}
}
