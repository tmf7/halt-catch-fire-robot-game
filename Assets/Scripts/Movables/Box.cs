using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Throwable {

	public float 			exitSpeed = 20.0f;
	public float 			exitDelay = 2.0f;

	private bool 			hasExited = false;

	void Update () {
		UpdateShadow ();
	}

	protected override void OnLanding () {
		base.OnLanding ();
		PlayRandomSoundFx (landingSounds);
	}

	// derived-class extension of OnCollisionEnter2D
	// because Throwable implements OnCollisionEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitCollision2D(Collision2D collision) {

		// prevent this box from being targeted again
		if (!hasExited && collision.collider.tag == "BoxExit") {
			hasExited = true;
			GameManager.instance.Remove (this);
		}
	}

	// derived-class extension of OnTriggerEnter2D
	// because Throwable implements OnTriggerEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitTrigger2D (Collider2D hitTrigger) {

		// prevent this box from being targeted again
		if (!hasExited && hitTrigger.tag == "BoxExit") {
			hasExited = true;
			GameManager.instance.Remove (this);
		}
	}

	public void ExitBox() {
		GetComponent<BoxCollider2D> ().enabled = false;
		SetHeight (2.0f * deadlyHeight);
		rb2D.velocity = new Vector2( 0.0f, exitSpeed);
		Throw (rb2D.velocity.y, -1.0f);
		Invoke ("Remove", exitDelay);
	}

	void OnDrawGizmos() {
		if (isTargeted) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawCube (transform.position, Vector3.one);
		}
	}
}
