using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrusherSwitch : MonoBehaviour {

	public AudioClip 	switchClickSound;

	private AudioSource source;
	private Animator 	crusherAnimator;
	private bool 		wasPlaying = false;

	void Awake() {
		source = GetComponent<AudioSource> ();
		crusherAnimator = transform.parent.GetComponent<Animator> ();
	}

	private void PlayClickSound() {
		source.clip = switchClickSound;
		source.Play ();
	}
		
	void Update() {
		if (source.isPlaying) {
			wasPlaying = true;
		} else if (wasPlaying) {
			crusherAnimator.SetTrigger ("SmashCrusher");
			wasPlaying = false;
		}
	}
}
