using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoxThrower : MonoBehaviour {

	public Box 			boxPrefab;
	public TimedBox		timedBoxPrefab;
	public SlowBox		slowBoxPrefab;
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

			Box thrownBox = null;
			float diceRoll = Random.Range (0.0f, 1.0f);

			if (diceRoll > slowBoxChanceRange.minimum && diceRoll <= slowBoxChanceRange.maximum)
				thrownBox = Instantiate<SlowBox> (slowBoxPrefab, transform.position, Quaternion.identity);
			else if (diceRoll > timedBoxChanceRange.minimum && diceRoll <= timedBoxChanceRange.maximum)
				thrownBox = Instantiate<TimedBox> (timedBoxPrefab, transform.position, Quaternion.identity);
			else
				thrownBox = Instantiate<Box> (boxPrefab, transform.position, Quaternion.identity);
			
			GameManager.instance.AddBox (thrownBox);
			thrownBox.RandomThrow ();
		}
	}
}
