using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxThrower : MonoBehaviour {

	public GameObject boxSprite;	// the prefab to throw

	private Rigidbody2D boxRB2D;

	void Awake() {
		boxRB2D = boxSprite.GetComponent<Rigidbody2D> ();

	}

	void Start () {
		// 1) the parent transform of the box starts at ground (affects both 
		// 2) the 3d rb is given a purely z-velocity
		// 3) the 2d rb is given an equivalent muzzle velocity at an angle (such that it will hit a specific x,y position on the map)
		// 4) 2d collision is disabled until the 3d rb reaches a set height (then the 2d collision box can hit robots and walls, etc)
		// 5) 2d rb gravity.scale is zeroed as soon as the 3d rb makes contact (so the box doesn't slide to the bottom of the screen)
	}
	
	// Update is called once per frame
	void Update () {
		//		boxRB2D.gravityScale = 0;  set this once the 3d rb reaches a certain height
		
	}
}
