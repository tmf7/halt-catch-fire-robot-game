using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxThrower : MonoBehaviour {

	public Box 			boxPrefab;
	public AudioClip[]	throwBoxSounds;
	public float 		throwDelay;

	private float 		nextThrowTime;

	void Start() {
		nextThrowTime = Time.time + throwDelay;
	}

	void Update () {

		if (Time.time > nextThrowTime) {
			nextThrowTime = Time.time + throwDelay;

			Box thrownBox = Instantiate<Box> (boxPrefab, transform.position, Quaternion.identity);
			GameManager.instance.AddBox (thrownBox);
			thrownBox.RandomThrow ();
			SoundManager.instance.PlayRandomSoundFx (throwBoxSounds);
		}
	}
}
