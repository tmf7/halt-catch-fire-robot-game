using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedBox : MonoBehaviour {

	public float 		implosionDelay = 5.0f;
	public float		midLifeFlashScale = 2.0f;
	public float		lateLifeFlashScale = 5.0f;

	private Animator	animator;
	private Box 		box;
	private Rigidbody2D boxRB;
	private float		implosionTime;
	private float		speedThreshold;

	void Awake () {
		animator = GetComponent<Animator> ();
		box = GetComponent<Box> ();
		boxRB = box.GetComponent<Rigidbody2D> ();
		speedThreshold = box.exitSpeed * 0.5f;
		implosionTime = Time.time + implosionDelay;
	}

	void Update () {
		if (Robot.isHalted)
			implosionTime += Time.deltaTime;

		float fraction = 1.0f - (implosionTime - Time.time) / implosionDelay;
		if (fraction > 0.5f)
			animator.speed = midLifeFlashScale;
		if (fraction > 0.8f)
			animator.speed = lateLifeFlashScale;
		if (Time.time > implosionTime && boxRB.velocity.y < speedThreshold)
			box.Explode ();
	}

}
