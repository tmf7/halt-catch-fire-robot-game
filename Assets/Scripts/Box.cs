using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Throwable {

	public AudioClip[]	boxLandingSounds;
	public float 		exitSpeed = 10;
	public float 		exitDelay = 1.0f;

	[HideInInspector]
	public bool 		willExit = false;

	private float 		exitTime;

	void Update () {
		UpdateShadow ();
		if (willExit && Time.time > exitTime)
			Remove ();
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (!grounded && collision.gameObject.layer == Mathf.Log(airStrikeMask.value,2)) {
			Explode ();
		}
	}

	protected override void OnLanding () {
		SoundManager.instance.PlayRandomSoundFx (boxLandingSounds);
	}

	protected override void HitTrigger2D (Collider2D collider) {
		if (collider.tag == "Finish") {
			willExit = true;
			exitTime = Time.time + exitDelay;
			rb2D.velocity = Vector2.up * exitSpeed;
			Throw (rb2D.velocity.y, -1.0f);
		}
	}
}
