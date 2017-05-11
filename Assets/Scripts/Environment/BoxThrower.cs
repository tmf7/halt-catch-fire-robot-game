using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoxThrower : MonoBehaviour {

	public GameObject	boxPrefab;
	public GameObject	timedBoxPrefab;
	public GameObject	slowBoxPrefab;
	public float 		throwDelay;
	public Range 		slowBoxChanceRange = new Range (0.8f, 0.9f);
	public Range 		timedBoxChanceRange = new Range (0.9f, 1.0f);
	public bool 		throwBoxes = true;

	private float 		nextThrowTime;
	private bool		throwOn;

	void Start() {
		nextThrowTime = Time.time + throwDelay;
	}

	void Update () {
		throwOn = throwBoxes && GameManager.instance.boxCount < GameManager.instance.maxBoxes;

		if (throwOn && (Time.time > nextThrowTime || Time.timeSinceLevelLoad < throwDelay)) {
			nextThrowTime = Time.time + throwDelay;

			GameObject boxObject = null;
			float diceRoll = Random.Range (0.0f, 1.0f);

			if (diceRoll > slowBoxChanceRange.minimum && diceRoll <= slowBoxChanceRange.maximum)
				boxObject = Instantiate<GameObject> (slowBoxPrefab, transform.position, Quaternion.identity);
			else if (diceRoll > timedBoxChanceRange.minimum && diceRoll <= timedBoxChanceRange.maximum)
				boxObject = Instantiate<GameObject> (timedBoxPrefab, transform.position, Quaternion.identity);
			else
				boxObject = Instantiate<GameObject> (boxPrefab, transform.position, Quaternion.identity);

			Box thrownBox = boxObject.GetComponent<Box> ();
			GameManager.instance.AddBox (thrownBox);
			thrownBox.RandomThrow ();
		}
	}
}
