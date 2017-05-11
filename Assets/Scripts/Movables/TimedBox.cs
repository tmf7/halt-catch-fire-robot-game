using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedBox : MonoBehaviour {

	public float 		implosionDelay = 10.0f;
	public float		midLifeFlashScale = 2.0f;
	public float		lateLifeFlashScale = 5.0f;

	private Animator	animator;
	private Box 		box;
	private float		implosionTime;

	void Awake () {
		animator = GetComponent<Animator> ();
		box = GetComponent<Box> ();
		implosionTime = Time.time + implosionDelay;
	}

	void Update () {
		// step the animator speed over its duration
		// once exceeded the lifetime, implode with point effector (and implosion animation spawn)
		// delivering this box is worth 10

		float fraction = 1.0f - (implosionTime - Time.time) / implosionDelay;
		if (fraction > 0.5f)
			animator.speed = midLifeFlashScale;
		if (fraction > 0.8f)
			animator.speed = lateLifeFlashScale;
		if (Time.time > implosionTime && !box.hasExited)
			box.Explode ();
	}

}
