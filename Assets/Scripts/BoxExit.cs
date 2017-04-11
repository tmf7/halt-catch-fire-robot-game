﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxExit : MonoBehaviour {

	private LayerMask 		exitMask;
	private BoxCollider2D 	boxCollider;

	void Awake () {
		boxCollider = GetComponent<BoxCollider2D> ();
		exitMask = LayerMask.GetMask ("DynamicCollision");
	}
	
	void FixedUpdate () {
		Vector2 exitCenter = new Vector2(transform.position.x, transform.position.y) + boxCollider.offset;

		// FIXME: possibly make the distance 0.0f instead of 1.0f
		RaycastHit2D[] hits = Physics2D.BoxCastAll(exitCenter, boxCollider.size, 0.0f, Vector3.zero, 1.0f, exitMask);

		foreach (RaycastHit2D hit in hits) {
			Throwable toThrow = hit.collider.GetComponent<Throwable> ();
			if (toThrow != null) {
				if (toThrow.isBeingCarried)
					toThrow.GetCarrier ().DropItem ();
				
				if (toThrow is Box) {
					(toThrow as Box).ExitBox ();
				} else { // its a robot
					(toThrow as Robot).ExitRobot();
				}
			}
		}
	}
}
