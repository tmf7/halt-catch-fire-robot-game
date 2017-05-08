using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crusher : MonoBehaviour {

	public AudioClip smashSound;
	public AudioClip retractSound;

	private Animator animator;
	private AudioSource source;
	private ParticleSystem smashParticles;
	private BoxCollider2D boxCollider;

	void Start() {
		source = GetComponent<AudioSource> ();
		boxCollider = GetComponent<BoxCollider2D> ();
		animator = GetComponent<Animator> ();
		smashParticles = GetComponentInChildren<ParticleSystem> ();
	}

	// activate the crusherShadow collider
	// crusherShadow must be the first child of crusher
	private void EnableCollider() {
		boxCollider.isTrigger = false;
	}

	// de-activate the crusherShadow collider
	// crusherShadow must be the first child of crusher
	private void DisableCollider() {
		boxCollider.isTrigger = true;
	}

	private void PlaySmashSound() {
		smashParticles.Play ();
		source.clip = smashSound;
		source.Play ();
	}

	private void PlayRetractSound() {
		source.clip = retractSound;
		source.Play ();
	}

	private void TriggerRetract() {
		animator.SetTrigger ("RetractCrusher");
	}

	void OnCollisionStay2D(Collision2D collision) {
		Robot hitRobot = collision.collider.GetComponent<Robot> ();
		if (hitRobot != null) {
			hitRobot.howDied = RobotNames.MethodOfDeath.DEATH_BY_CRUSHER;
			hitRobot.Explode ();
		}
	}
}
