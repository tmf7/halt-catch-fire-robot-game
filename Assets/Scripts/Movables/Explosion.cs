using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Explosion : MonoBehaviour {

	public AudioClip[] explosionSounds;

	private AudioSource source;

	void Awake () {
		source = GetComponent<AudioSource> ();
		int randomIndex = Random.Range (0, explosionSounds.Length);
		source.clip = explosionSounds [randomIndex];
		source.Play ();
	}

	// called from an animation event in the explosion animation
	private void DestroyExplosion() {
		Destroy(gameObject);
	}
}
