using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxThrower : MonoBehaviour {

	public Box 			boxPrefab;
	public float 		throwDelay;
	public bool			throwBoxes = true;

	private float 		nextThrowTime;
	private bool		throwOn;

	void Start() {
		nextThrowTime = Time.time + throwDelay;
	}

	void Update () {
		throwOn = throwBoxes && GameManager.instance.boxCount < GameManager.instance.maxBoxes;

		if (throwOn && (Time.time > nextThrowTime || Time.timeSinceLevelLoad < throwDelay)) {
			nextThrowTime = Time.time + throwDelay;

			Box thrownBox = Instantiate<Box> (boxPrefab, transform.position, Quaternion.identity);
			GameManager.instance.AddBox (thrownBox);
			thrownBox.RandomThrow ();
		}
	}
}
