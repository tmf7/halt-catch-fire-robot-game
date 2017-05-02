using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Smoke : MonoBehaviour {

	public AudioClip 		catchFireSound;
	public float			burnToDeathDelay = 10.0f;
	public float 			timeToDie;
	public Range 			emissionRange = new Range (0.0f, 5.0f);

	private AudioSource 	source;
	private ParticleSystem 	smoke;
	private Robot 			robotToBurn;

	void Awake () {
		smoke = GetComponent<ParticleSystem> ();
		source = GetComponent<AudioSource> ();
		source.clip = catchFireSound;
		float pitch = Random.Range (0.95f, 1.05f);
		source.pitch = pitch;
		source.Play ();
		robotToBurn = GetComponentInParent<Robot> ();
		timeToDie = Time.time + burnToDeathDelay;
	}

	void Update() {
		// increase the amount of smoke
		float lerpFactor = 1.0f - ((timeToDie - Time.time) / burnToDeathDelay);
		if (lerpFactor > 0.5f) {
			var em = smoke.emission;
			em.rateOverTime = Mathf.RoundToInt (Mathf.Lerp (emissionRange.minimum, emissionRange.maximum, lerpFactor));
		}

		// shrink with the robot
		transform.localScale = new Vector3(robotToBurn.currentPitfallScale,  robotToBurn.currentPitfallScale, robotToBurn.currentPitfallScale);

		if (Time.time > timeToDie) {
			robotToBurn.howDied = RobotNames.MethodOfDeath.DEATH_BY_FIRE;
			robotToBurn.Explode ();
		}
	}
}
