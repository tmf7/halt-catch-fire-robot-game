using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Throwable {

	public float 			exitSpeed = 20.0f;
	public float 			exitDelay = 2.0f;

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
		// box collision stuff
	}

	// derived-class extension of OnTriggerEnter2D
	// because Throwable implements OnTriggerEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitTrigger2D (Collider2D hitTrigger) {
		// box trigger stuff
	}

	public void ExitBox() {
		GetComponent<BoxCollider2D> ().enabled = false;
		SetHeight (2.0f * deadlyHeight);
		rb2D.velocity = new Vector2( 0.0f, exitSpeed);
		Throw (rb2D.velocity.y, -1.0f);
		Invoke ("Remove", exitDelay);
	}
}
