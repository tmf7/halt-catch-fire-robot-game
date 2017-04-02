using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour {

	public float speed = 0.05f;
	public bool isStopped = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		if (! isStopped) {
			transform.Translate (speed * Time.deltaTime, 0, 0);
		}
	}

	void OnMouseDown() {
	
		Debug.Log ("Clicking block");
//		isStopped = true;

		if (!isStopped) {
			isStopped = true;
		} else if (isStopped) {
			isStopped = false;
		}
	}
}