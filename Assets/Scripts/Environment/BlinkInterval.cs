using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BlinkInterval : MonoBehaviour {

	public Range blinkTimes = new Range(2.0f, 5.0f);

	private Animator animator;
	private float blinkInterval;
	private float nextBlinkTime;

	void Start () {
		animator = GetComponent<Animator> ();
		blinkInterval = Random.Range (blinkTimes.minimum, blinkTimes.maximum);
		nextBlinkTime = Time.time + blinkInterval;
		if (tag == "Hazard")
			animator.speed = 2.0f;
		else
			animator.speed = 0.25f;
	}
	
	void Update () {
		if (Time.time > nextBlinkTime) {
			animator.SetTrigger ("Blink");
			nextBlinkTime = Time.time + blinkInterval;
		}	
	}
}
