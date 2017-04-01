using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour {

	// NOTE: project settings -> physics, the Shadow layer only collides with the Map layer
	// This object's z-coordinate determines the drop shadow sprite y-offset relative to its user sprite

	private Rigidbody rb;

	void Awake () {
		rb = GetComponent<Rigidbody> ();
		rb.isKinematic = true;
	}

	// for thrown boxes use velocity == Vector3.forward * parentRB.velocity.y
	public void SetVelocity(Vector3 velocity) {	
		rb.velocity = velocity;	
	}

	public void SetHeight (float height) {
		transform.position = Vector3.forward * height;	
	}

	public void Drop() {
		rb.isKinematic = false;
	}

	public void Hang() {
		rb.isKinematic = true;
	}

	// height off the ground that determines the dropShadow sprite y-offset from its user sprite
	public float GetShadowOffset() {
		return transform.position.z;
	}

}
