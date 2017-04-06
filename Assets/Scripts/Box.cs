using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Throwable {

	public AudioClip[]		boxLandingSounds;

	[HideInInspector]
	public bool 		willExit = false;

	private float 		exitTime;
	private float 		exitDelay = 1.0f;

	void Update () {
		UpdateShadow ();	
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (!grounded && collision.gameObject.layer == Mathf.Log(airStrikeMask.value,2)) {
			Explode ();
		}
	}

	protected override void OnLanding () {
		SoundManager.instance.PlayRandomSoundFx (boxLandingSounds);
	}
}
