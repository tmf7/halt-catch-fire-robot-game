using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprinklerSystem : MonoBehaviour {

	public AudioClip[] sprinklerSounds;

	private AudioSource source;
	private ParticleSystem[] sprinklerSystem;
	private Animator coolDownAnimator;
	private bool buttonCooldown = false;

	void Start () {
		GameObject sprinklerHandler = GameObject.Find ("SprinklerHandler");
		sprinklerSystem = sprinklerHandler.GetComponentsInChildren<ParticleSystem> ();
		source = GetComponent<AudioSource> ();
		coolDownAnimator = GetComponent<Animator> ();
	}


	void Update() {
		if (HUDManager.instance.playSprinklerSystem && !buttonCooldown) {
			coolDownAnimator.SetTrigger ("Cooldown");
			buttonCooldown = true;

			foreach (ParticleSystem sprinkler in sprinklerSystem)
				sprinkler.Play ();

			int randomIndex = Random.Range (0, sprinklerSounds.Length);
			float pitch = Random.Range (0.95f, 1.05f);
			source.pitch = pitch;
			source.clip = sprinklerSounds [randomIndex];
			source.Play ();
		}

		if (HUDManager.instance.resetSprinklerCooldown) {
			CooldownComplete ();
		}

		// the game is paused, this longer sound is an exception
		// to just letting clips play out
		if (Time.timeScale == 1.0f)
			source.UnPause ();
		else
			source.Pause ();

		if (!sprinklerSystem [0].isPlaying)
			source.Stop ();
	}

	// invoked by cooldown animation event
	public void CooldownComplete() {
		buttonCooldown = false;
		HUDManager.instance.playSprinklerSystem = false;
		HUDManager.instance.resetSprinklerCooldown = false;
	}
}
