using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamPuff : MonoBehaviour {

	public AudioClip[] 		steamPuffSounds;

	private AudioSource 	source;
	private ParticleSystem 	steamPuff;
	private Robot 			robotToExtinguish;

	void Awake () {
		steamPuff = GetComponent<ParticleSystem> ();
		source = GetComponent<AudioSource> ();
		int randomIndex = Random.Range (0, steamPuffSounds.Length);
		source.clip = steamPuffSounds [randomIndex];
		float pitch = Random.Range (0.95f, 1.05f);
		source.pitch = pitch;
		source.Play ();
		robotToExtinguish = GetComponentInParent<Robot> ();
	}
		
	void Update() {
		// shrink with the robot
		transform.localScale = new Vector3(robotToExtinguish.currentPitfallScale, robotToExtinguish.currentPitfallScale, robotToExtinguish.currentPitfallScale);

		if (!source.isPlaying)
			steamPuff.Stop (true, ParticleSystemStopBehavior.StopEmitting);
		if(!steamPuff.isPlaying)
			Destroy(gameObject);
	}
}
