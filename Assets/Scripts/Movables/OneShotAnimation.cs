using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class OneShotAnimation : MonoBehaviour {

	public AudioClip[] oneShotSounds;

	private AudioSource source;

	void Awake () {
		source = GetComponent<AudioSource> ();
		int randomIndex = Random.Range (0, oneShotSounds.Length);
		float pitch = Random.Range (0.95f, 1.05f);
		source.pitch = pitch;
		source.clip = oneShotSounds [randomIndex];
		source.Play ();
	}

	// called from an animation event in the attached animation
	private void DestroyObject() {
		Destroy(gameObject);
	}
}
