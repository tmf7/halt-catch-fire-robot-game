using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoxThrower : MonoBehaviour {

	public Box 			boxPrefab;
	public float 		throwDelay;

	private List<Box> 	allBoxes;		// TODO: move this to a singleton GameManager
	private float 		nextThrowTime;

	void Start() {
		nextThrowTime = Time.time + throwDelay;
		allBoxes = new List<Box>();
	}

	// 1) all transforms start equivalent to this boxThrower
	// 2) the 3d rb is given a purely z-velocity
	// 3) the 2d rb is given an equivalent muzzle velocity at an angle (such that it will hit a specific x,y position on the map)
	// 4) 2d collision is disabled until the 3d rb reaches a set height (then the 2d collision box can hit robots and walls, etc) [in Box.cs]
	// 5) 2d rb gravity.scale is zeroed as soon as the 3d rb makes contact (so the box doesn't slide to the bottom of the screen) [in Box.cs]

	void Update () {
		if (Time.time > nextThrowTime) {
			nextThrowTime = Time.time + throwDelay;

			float throwSpeed = Random.Range (8.0f, 12.0f);
			float throwAngle = Random.Range (30.0f * Mathf.Deg2Rad, 150.0f * Mathf.Deg2Rad);
			float airTime = Random.Range (0.5f, 2.0f);

			// FIXME: non-zero parent is screwing with the calculations
			Box thrownBox = Instantiate<Box>(boxPrefab, transform.position, Quaternion.identity);
			allBoxes.Add (thrownBox);
			thrownBox.airTime = airTime;

			Rigidbody2D boxRB;
			boxRB = thrownBox.GetComponent<Rigidbody2D> ();
			boxRB.velocity = new Vector2(throwSpeed * Mathf.Cos(throwAngle), throwSpeed * Mathf.Sin(throwAngle));
		}
	}
}
