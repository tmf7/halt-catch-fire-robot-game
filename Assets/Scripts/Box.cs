using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Throwable {

	public GameObject 		explosionPrefab;
	public AudioClip[]		boxLandingSounds;

	void Update () {
		UpdateShadow ();	
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (!grounded && collision.gameObject.layer == Mathf.Log(airStrikeMask.value,2)) {
			ExplodeBox ();
		}
	}

	public void ExplodeBox() {
		Instantiate<GameObject> (explosionPrefab, transform.position, Quaternion.identity);
		RemoveBox();
	}

	void RemoveBox() {
		GameManager.instance.RemoveBox (this);
		Destroy(shadowController);				// FIXME: may also need to destroy its script instance (hopefully not)
		Destroy(dropShadow);
		Destroy(gameObject);
		Destroy (this);
	}

	protected override void OnLanding () {
		SoundManager.instance.PlayRandomSoundFx (boxLandingSounds);
	}
}
