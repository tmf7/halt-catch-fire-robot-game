using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprinklerSystem : MonoBehaviour {

	public AudioClip[] sprinklerSounds;

	private AudioSource source;
	private ParticleSystem[] sprinklerSystem;
	private Animator coolDownAnimator;
	private int coolDownStateNameHash;
	private bool buttonCooldown = false;

	void Start () {
		GameObject sprinklerHandler = GameObject.Find ("SprinklerHandler");

		sprinklerSystem = sprinklerHandler.GetComponentsInChildren<ParticleSystem> ();
		source = GetComponent<AudioSource> ();
		coolDownAnimator = GetComponent<Animator> ();
		coolDownStateNameHash = coolDownAnimator.GetCurrentAnimatorStateInfo (0).fullPathHash;
	}

	void Update() {
		if (HUDManager.instance.playSprinklerSystem && !buttonCooldown) {
			buttonCooldown = true;

			foreach (ParticleSystem sprinkler in sprinklerSystem)
				sprinkler.Play ();

			int randomIndex = Random.Range (0, sprinklerSounds.Length);
			float pitch = Random.Range (0.95f, 1.05f);
			source.pitch = pitch;
			source.clip = sprinklerSounds [randomIndex];
			source.Play ();

			coolDownAnimator.Play (coolDownStateNameHash, 0, 0.0f);
		}

		if (HUDManager.instance.resetSprinklerCooldown) {
			CooldownComplete ();
		}

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
