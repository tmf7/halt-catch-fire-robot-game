using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Implosion : MonoBehaviour {

	public float 				rotationSpeed = 360.0f;

	private CircleCollider2D[] 	killCircles;
	private Animator			oneShotAnimation;
	private float 				initialRadius;

	void Start () {
		killCircles = GetComponents<CircleCollider2D> ();
		initialRadius = killCircles[0].radius;
		oneShotAnimation = GetComponent<Animator>();
		float angle = Random.Range (0.0f, 360.0f);
		Quaternion rotation = Quaternion.AngleAxis (angle, Vector3.forward);
		transform.rotation = rotation;
	}

	void Update () {
		Quaternion rotation = Quaternion.AngleAxis (rotationSpeed * Time.deltaTime, Vector3.forward);
		transform.rotation *= rotation;

		// playing sensibly fair
		float newRadius = initialRadius * (1.0f - oneShotAnimation.GetCurrentAnimatorStateInfo(0).normalizedTime);
		killCircles[0].radius = newRadius;
		killCircles [1].radius = newRadius;
	}

	void OnTriggerStay2D (Collider2D hitCollider) {
		Robot robot = hitCollider.GetComponent<Robot> ();	
		if (robot != null && !robot.fellInPit)
			robot.fellInPit = true;
	}
}
