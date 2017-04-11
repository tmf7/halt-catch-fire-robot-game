using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crusher : MonoBehaviour {

	public AudioClip smashSound;
	public AudioClip retractSound;

	private AudioSource source;
	private Animator animator;
	private ParticleSystem smashParticles;

	void Start() {
		source = GetComponent<AudioSource> ();
		animator = GetComponent<Animator> ();
		smashParticles = GetComponentInChildren<ParticleSystem> ();
	}

	// activate the crusherShadow collider
	// crusherShadow must be the first child of crusher
	private void EnableCollider() {
		GetComponentInChildren<BoxCollider2D> ().enabled = true;
	}

	// de-activate the crusherShadow collider
	// crusherShadow must be the first child of crusher
	private void DisableCollider() {
		GetComponentInChildren<BoxCollider2D> ().enabled = false;
	}

	private void PlaySmashSound() {
		smashParticles.Play ();
		source.clip = smashSound;
		source.Play ();
	}

	private void PlayRetractSount() {
		source.clip = retractSound;
		source.Play ();
	}

	private void TriggerRetract() {
		animator.SetTrigger ("RetractCrusher");
	}

	void OnCollisionEnter2D(Collision2D collision) {
		Robot hitRobot = collision.collider.GetComponent<Robot> ();
		if (hitRobot != null)
			hitRobot.Explode ();
	}
}
