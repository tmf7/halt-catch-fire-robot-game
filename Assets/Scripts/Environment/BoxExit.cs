using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxExit : MonoBehaviour {

	private LayerMask 		exitMask;
	private BoxCollider2D 	boxCollider;
	private ParticleSystem	exitBlast;

	void Awake () {
		boxCollider = GetComponent<BoxCollider2D> ();
		exitMask = LayerMask.GetMask ("DynamicCollision");
	}

	void Start() {
		exitBlast = GetComponentInChildren<ParticleSystem> ();
	}
	
	void FixedUpdate () {
		Vector2 exitCenter = new Vector2(transform.position.x, transform.position.y) + boxCollider.offset;

		RaycastHit2D[] hits = Physics2D.BoxCastAll(exitCenter, boxCollider.size, 0.0f, Vector3.zero, 1.0f, exitMask);

		foreach (RaycastHit2D hit in hits) {
			Throwable toThrow = hit.collider.GetComponent<Throwable> ();
			if (toThrow != null) {
				if (toThrow.isBeingCarried)
					toThrow.GetCarrier ().DropItem ();
				
				if (toThrow is Box) {
					(toThrow as Box).ExitBox ();
					exitBlast.Play ();
				} else { // its a robot
					(toThrow as Robot).ExitRobot();
				}
			}
		}
	}
}
