using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamPuff : MonoBehaviour {

	public AudioClip[] steamPuffSounds;

	private AudioSource source;
	private ParticleSystem steamPuff;

	void Awake () {
		steamPuff = GetComponent<ParticleSystem> ();
		source = GetComponent<AudioSource> ();
		int randomIndex = Random.Range (0, steamPuffSounds.Length);
		source.clip = steamPuffSounds [randomIndex];
		source.Play ();
	}


	void Update() {
		if (!source.isPlaying)
			steamPuff.Stop (true, ParticleSystemStopBehavior.StopEmitting);
		if(!steamPuff.isPlaying)
			Destroy(gameObject);
	}
}
