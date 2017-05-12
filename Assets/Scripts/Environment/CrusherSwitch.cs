using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrusherSwitch : MonoBehaviour {

	public AudioClip 	switchClickSound;
	public float 		startPercent = 0.0f;

	private AudioSource source;
	private Animator 	crusherAnimator;
	private Animator	switchAnimator;

	void Awake() {
		source = GetComponent<AudioSource> ();
		crusherAnimator = transform.parent.GetComponent<Animator> ();
		switchAnimator = GetComponent<Animator> ();
		switchAnimator.Play ("SwitchOn", 0, startPercent);
	}

	void Update () {
		switchAnimator.enabled = !Robot.isHalted;
		crusherAnimator.enabled = !Robot.isHalted;
	}

	private void PlayClickSound() {
		source.clip = switchClickSound;
		source.Play ();
		crusherAnimator.SetTrigger ("SmashCrusher");
	}
}
