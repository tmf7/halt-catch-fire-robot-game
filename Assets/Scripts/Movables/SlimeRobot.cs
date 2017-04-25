using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeRobot : MonoBehaviour {

	public float moveSpeed = 2.0f;

	private Transform target;

	void Start() {
		target = GameObject.Find ("SlimeBotTarget").transform;
		StartCoroutine (MoveToTarget ());
	}

	public IEnumerator MoveToTarget() {

		while (transform.position != target.position) {		
			transform.position = Vector3.MoveTowards (transform.position, target.position, moveSpeed * Time.deltaTime);
			yield return null;
		}

		if (HUDManager.instance.allRobotsFired)
			UIManager.instance.FadeToGameOver ();
		else
			UIManager.instance.FadeToStory ();
	}
}
