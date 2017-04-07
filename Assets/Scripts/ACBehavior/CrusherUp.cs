using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrusherUp: StateMachineBehaviour {

	public AudioClip crusherUpSound;

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (stateInfo.normalizedTime > 0.25f) {
			animator.gameObject.GetComponent<BoxCollider2D> ().enabled = false;
			AudioSource source = animator.GetComponent<AudioSource> ();
			Debug.Log (source.isPlaying);
			if (!source.isPlaying) {
				Debug.Log ("SCHWOOP");
				source.clip = crusherUpSound;
				source.Play ();
			}
		}
	}
}
