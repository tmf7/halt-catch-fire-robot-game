using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrusherDown : StateMachineBehaviour {

	public AudioClip crusherDownSound;

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (stateInfo.normalizedTime > 0.75f) {
			animator.gameObject.GetComponent<Collider2D> ().enabled = true;
			AudioSource source = animator.GetComponent<AudioSource> ();
			if (!source.isPlaying) {
				source.clip = crusherDownSound;
				source.Play ();
			}
		}
	}
}
