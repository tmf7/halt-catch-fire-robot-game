using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuicideCircle : MonoBehaviour {

	private CapsuleCollider2D observableArea;
	private SpriteRenderer spriteRenderer;
	private Robot owner;

	void Start () {
		observableArea = GetComponent<CapsuleCollider2D> ();
		spriteRenderer = GetComponent<SpriteRenderer> ();
		owner = GetComponentInParent<Robot> ();
	}

	public void Update () {
		spriteRenderer.enabled = (owner.currentState == Robot.RobotStates.STATE_SUICIDAL && owner.grounded);
	}

	public void OnTriggerStay2D (Collider2D hitTrigger) {
//		if (spriteRenderer.enabled) {
//			Collider2D hit = GameManager.instance.IsTouchingHazard (observableArea);
//			owner.target = (hit == null ? owner.target : hit.transform);
//		}
		owner.target = hitTrigger.transform;
		owner.ignoreHit = true;
	}
}
