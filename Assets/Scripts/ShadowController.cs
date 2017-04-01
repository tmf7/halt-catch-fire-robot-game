using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour {

	// NOTE: project settings -> physics, the Shadow layer only collides with the Map layer
	// This object's z-coordinate determines the drop shadow sprite y-offset relative to its user sprite

	private Rigidbody rb;
	private BoxCollider boxCollider;
	private bool isGrounded;

	void Awake () { 
		rb = GetComponent<Rigidbody> ();
		boxCollider = GetComponent<BoxCollider> ();
		rb.isKinematic = true;

		// move the box origin/center to slightly above the plane so it can properly collide with the plane
		rb.transform.position += Vector3.forward * boxCollider.size.z * 0.5f;
	}

	// for thrown boxes use velocity == Vector3.forward * parentRB.velocity.y
	public void SetVelocity(Vector3 velocity) {	
		rb.velocity = velocity;	
	}

	public void SetHeight (float height) {
		transform.position = Vector3.forward * height;	
	}

	public void SetKinematic(bool isKinematic) {
		rb.isKinematic = isKinematic;
	}

	public bool IsKinematic() {
		return rb.isKinematic;
	}

	public bool IsFalling () {
		return rb.velocity.z < 0.0f;
	}

	public bool IsGrounded() {
		return isGrounded;
	}

	// height off the ground that determines the dropShadow sprite y-offset from its user sprite
	public float GetShadowOffset() {
		return rb.transform.position.z - boxCollider.size.z * 0.5f;
	}

	void OnCollisionEnter(Collision collision) {
		isGrounded = true;
	}

	void OnCollisionExit(Collision collision) {
		isGrounded = false;
	}

}
