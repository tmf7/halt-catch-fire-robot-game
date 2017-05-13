using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class OneShotParticles : MonoBehaviour {

	public float particleDuration = 3.0f;
	public AudioClip[] loopParticleSounds;

	private AudioSource source;
	private float destroyTime;

	// FIXME(~): this works for more than particle systems
	void Awake () {
		source = GetComponent<AudioSource> ();
		int randomIndex = Random.Range (0, loopParticleSounds.Length);
		float pitch = Random.Range (0.95f, 1.05f);
		source.pitch = pitch;
		source.clip = loopParticleSounds [randomIndex];
		source.Play ();
		destroyTime = Time.time + particleDuration;
	}

	void Update () {
		if (Robot.isHalted)
			destroyTime += Time.deltaTime;

		if (Time.time > destroyTime)
			Destroy (gameObject);
	}
}
