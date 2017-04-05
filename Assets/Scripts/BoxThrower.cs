using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class Range {
	public float minimum;
	public float maximum;

	public Range (float min, float max) {
		minimum = min;
		maximum = max;
	}
}

public class BoxThrower : MonoBehaviour {

	public Box 			boxPrefab;
	public AudioClip[]	throwBoxSounds;
	public float 		throwDelay;
	public Range 		throwSpeeds = new Range(8.0f, 12.0f);
	public Range 		throwAnglesDeg = new Range (30.0f, 150.0f);
	public Range		airTimes = new Range(0.5f, 2.0f);

	private float 		nextThrowTime;

	void Start() {
		nextThrowTime = Time.time + throwDelay;
	}

	void Update () {

		if (Time.time > nextThrowTime) {
			nextThrowTime = Time.time + throwDelay;

			// FIXME: non-zero parent is screwing with the calculations, create a globally visible parent for this and the dropShadow
			Box thrownBox = Instantiate<Box> (boxPrefab, transform.position, Quaternion.identity);
			GameManager.instance.AddBox (thrownBox);

			Rigidbody2D boxRB;
			boxRB = thrownBox.GetComponent<Rigidbody2D> ();
			float throwSpeed = Random.Range (throwSpeeds.minimum, throwSpeeds.maximum);
			float throwAngle = Random.Range (throwAnglesDeg.minimum * Mathf.Deg2Rad, throwAnglesDeg.maximum * Mathf.Deg2Rad);
			float airTime = Random.Range (airTimes.minimum, airTimes.maximum);
			boxRB.velocity = new Vector2 (throwSpeed * Mathf.Cos (throwAngle), throwSpeed * Mathf.Sin (throwAngle));
			thrownBox.SetHeight (2.0f * thrownBox.deadlyHeight);
			thrownBox.Throw (boxRB.velocity.y, airTime);
			SoundManager.instance.PlayRandomSoundFx (throwBoxSounds);
		}
	}
}
