using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Implosion : MonoBehaviour {

	public float rotationSpeed = 360.0f;

	void Start () {
		float angle = Random.Range (0.0f, 360.0f);
		Quaternion rotation = Quaternion.AngleAxis (angle, Vector3.forward);
		transform.rotation = rotation;
	}

	void Update () {
		Quaternion rotation = Quaternion.AngleAxis (rotationSpeed * Time.deltaTime, Vector3.forward);
		transform.rotation *= rotation;
	}

	void OnTriggerStay2D (Collider2D hitCollider) {
		Robot robot = hitCollider.GetComponent<Robot> ();	
		if (robot != null && !robot.fellInPit)
			robot.fellInPit = true;
	}
}
