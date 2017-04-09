using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Throwable {

	public float 			exitSpeed = 10;
	public float 			exitDelay = 1.0f;

	void Update () {
		UpdateShadow ();
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (!grounded && collision.gameObject.layer == Mathf.Log(airStrikeMask.value,2)) {
			Explode ();
		}
	}

	protected override void OnLanding () {
		base.OnLanding ();
		PlayRandomSoundFx (landingSounds);
	}

	protected override void HitTrigger2D (Collider2D collider) {
		if (collider.tag == "Finish") {
			rb2D.velocity = Vector2.up * exitSpeed;
			Throw (rb2D.velocity.y, -1.0f);
			Invoke ("Remove", exitDelay);
		}
	}
}
