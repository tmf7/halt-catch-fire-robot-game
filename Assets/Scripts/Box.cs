using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Throwable {

	public float 			exitSpeed = 20.0f;
	public float 			exitDelay = 2.0f;

	void Update () {
		UpdateShadow ();
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (!grounded)
			HitWall ();
	}

	protected override void OnLanding () {
		base.OnLanding ();
		PlayRandomSoundFx (landingSounds);
	}

	protected override void HitTrigger2D (Collider2D collider) {
		if (collider.tag == "Finish") {
			GetComponent<BoxCollider2D> ().enabled = false;
			SetHeight (2.0f * deadlyHeight);
			rb2D.velocity = new Vector2( 0.0f, exitSpeed);
			Throw (rb2D.velocity.y, -1.0f);
			Invoke ("Remove", exitDelay);
		}
	}

	void OnDrawGizmos() {
		if (isClaimed) {
			Gizmos.color = Color.green;
			Gizmos.DrawCube (transform.position, Vector3.one);
		}
	}
}
