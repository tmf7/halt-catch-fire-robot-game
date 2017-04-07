using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crusher : MonoBehaviour {

	public AudioClip smashSound;
	public AudioClip retractSound;

	private AudioSource source;
	private Animator animator;

	void Awake() {
		source = GetComponent<AudioSource> ();
		animator = GetComponent<Animator> ();
	}

	private void EnableCollider() {
		GetComponent<BoxCollider2D> ().enabled = true;
	}

	private void DisableCollider() {
		GetComponent<BoxCollider2D> ().enabled = false;
	}

	private void PlaySmashSound() {
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
}
