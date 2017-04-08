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
	/*
		var velocity = landingPuff.velocityOverLifetime;
		velocity.enabled = true;
		velocity.space = ParticleSystemSimulationSpace.Local;

		AnimationCurve curve = new AnimationCurve();
		curve.AddKey(0.0f, -rb2D.velocity.x);
		curve.AddKey(1.0f, 0.0f);
		velocity.x = new ParticleSystem.MinMaxCurve(0.5f, curve);
		curve.RemoveKey (0);
		curve.RemoveKey (0);
		curve.AddKey(0.0f, -rb2D.velocity.y);
		curve.AddKey(1.0f, 0.0f);
		velocity.y = new ParticleSystem.MinMaxCurve(0.5f, curve);


		landingPuff.Play ();
	*/
		base.OnLanding ();
	}

	protected override void HitTrigger2D (Collider2D collider) {
		if (collider.tag == "Finish") {
			rb2D.velocity = Vector2.up * exitSpeed;
			Throw (rb2D.velocity.y, -1.0f);
			Invoke ("Remove", exitDelay);
		}
	}
}
