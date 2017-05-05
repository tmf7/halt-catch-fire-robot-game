using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObservationCircle : MonoBehaviour {

	public Color 				homicideSearchColor;
	public Color 				suicideSearchColor;
	public float 				yOffset = -0.45f;

	private SpriteRenderer 		spriteRenderer;
	private Robot 				owner;

	void Start () {
		spriteRenderer = GetComponent<SpriteRenderer> ();
		transform.SetParent(GameManager.instance.robotParent);
	}

	public void SetOwner(Robot _owner) {
		owner = _owner;
	}

	public void Update () {
		if (owner == null)
			Destroy (gameObject);
	}

	public void UpdateVisuals () {
		transform.position = owner.transform.position + (Vector3.up * yOffset);
		spriteRenderer.enabled = (owner.currentState != Robot.RobotStates.STATE_FINDBOX && owner.grounded);
		if (spriteRenderer.enabled ) {
			bool homicidal = owner.currentState == Robot.RobotStates.STATE_HOMICIDAL;
			spriteRenderer.color =  homicidal ? homicideSearchColor
											  : suicideSearchColor;
		}
	}

	void OnTriggerEnter2D (Collider2D hit) {
		if (owner == null || hit == null || !spriteRenderer.enabled || hit.name == owner.name)
			return;
		
		if ((owner.currentState == Robot.RobotStates.STATE_HOMICIDAL && hit.gameObject.GetComponent<Robot> () != null)
			|| (owner.currentState == Robot.RobotStates.STATE_SUICIDAL && GameManager.instance.IsHazard(hit.transform))) { 
			if (owner.isCarryingBox) 
				owner.DropItem ();
			owner.target = hit.transform;
		}
	}

	void OnCollisionEnter2D (Collision2D hit) {
		if (owner == null || hit == null || !spriteRenderer.enabled || hit.gameObject.name == owner.name)
			return;

		if ((owner.currentState == Robot.RobotStates.STATE_HOMICIDAL && hit.gameObject.GetComponent<Robot> () != null)
			|| (owner.currentState == Robot.RobotStates.STATE_SUICIDAL && GameManager.instance.IsHazard(hit.transform))) { 
			if (owner.isCarryingBox) 
				owner.DropItem ();
			owner.target = hit.transform;
		}
	}
}
