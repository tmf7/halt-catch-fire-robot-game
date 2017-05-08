using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObservationCircle : MonoBehaviour {

	public Color 				homicideSearchColor;
	public Color 				suicideSearchColor;
	public float 				yOffset = -0.45f;

	private SpriteRenderer 		spriteRenderer;
	private Robot 				owner;

	void Awake () {
		spriteRenderer = GetComponent<SpriteRenderer> ();
		spriteRenderer.enabled = false;
		transform.SetParent(GameManager.instance.robotParent);
	}

	public void SetOwner(Robot _owner) {
		owner = _owner;
	}

	public void Update () {
		if (owner == null)
			Destroy (gameObject);
	}

	public void UpdateVisuals (bool show) {
		transform.position = owner.transform.position + (Vector3.up * yOffset);
		spriteRenderer.enabled = (owner.currentState != Robot.RobotStates.STATE_FINDBOX && show);
		if (spriteRenderer.enabled) {
			bool homicidal = owner.currentState == Robot.RobotStates.STATE_HOMICIDAL;
			spriteRenderer.color =  homicidal ? homicideSearchColor
											  : suicideSearchColor;
		}
	}

	void OnTriggerStay2D (Collider2D hit) {
		if (owner == null || hit == null || !spriteRenderer.enabled || hit.name == owner.name)
			return;
		
		if ((owner.currentState == Robot.RobotStates.STATE_HOMICIDAL && !owner.isCarryingRobot && hit.gameObject.GetComponent<Robot> () != null)
			|| (owner.currentState == Robot.RobotStates.STATE_SUICIDAL && GameManager.instance.IsHazard(hit.transform))) { 
			if (owner.isCarryingBox) 
				owner.DropItem ();
			owner.SetTarget(hit.transform);
		}
	}
}
