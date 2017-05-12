using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Throwable {

	public AudioClip[]		exitSounds;
	public float 			exitSpeed = 50.0f;
	public float 			exitDelay = 2.0f;

	[HideInInspector]
	public bool 			hasExited = false;

	public enum BoxTypes {
		BOXTYPE_NORMAL,
		BOXTYPE_TIMED,
		BOXTYPE_SLOW
	};

	private TrailRenderer	trail;
	private BoxTypes		boxType;
	private int				worth;

	private static int		normalBoxWorth = 1;
	private static int		timeBoxWorth = 3;
	private static int		slowBoxWorth = 5;

	void Start () {
		trail = GetComponent<TrailRenderer> ();
		trail.enabled = false;
		DetectBoxType ();
	}

	void Update () {
		UpdateShadow ();
	}
		
	private void DetectBoxType () {
		TimedBox timeType = GetComponent<TimedBox> ();
		SlowBox slowType = GetComponent<SlowBox> ();
		if (timeType != null)
			SetBoxType (BoxTypes.BOXTYPE_TIMED);
		else if (slowType != null)
			SetBoxType (BoxTypes.BOXTYPE_SLOW);
		else
			SetBoxType (BoxTypes.BOXTYPE_NORMAL);
	}

	private void SetBoxType (BoxTypes type) {
		boxType = type;
		switch (type) {
			case BoxTypes.BOXTYPE_NORMAL:
				worth = normalBoxWorth;
				break;
			case BoxTypes.BOXTYPE_TIMED:
				worth = timeBoxWorth;
				break;
			case BoxTypes.BOXTYPE_SLOW:
				worth = slowBoxWorth;
				break;
			default:
				type = BoxTypes.BOXTYPE_NORMAL;
				worth = normalBoxWorth;
				break;
		}
	}

	protected override void OnLanding () {
		base.OnLanding ();
		PlayRandomSoundFx (landingSounds);
	}

	// derived-class extension of OnCollisionEnter2D
	// because Throwable implements OnCollisionEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitCollision2D(Collision2D collision) {
		// box collision stuff
	}

	// derived-class extension of OnTriggerEnter2D
	// because Throwable implements OnTriggerEnter2D,
	// which prevents derived classes from directly using it
	protected override void HitTrigger2D (Collider2D hitTrigger) {

		// prevent this box from being targeted again
		if (!hasExited && hitTrigger.tag == "BoxExit")
			hasExited = true;
	}

	public BoxTypes GetBoxType() {
		return boxType;
	}

	public void ExitBox() {
		HUDManager.instance.CollectBox (worth);
		GetComponent<BoxCollider2D> ().enabled = false;
		SetHeight (2.0f * deadlyHeight);
		rb2D.velocity = new Vector2( 0.0f, exitSpeed);
		Throw (rb2D.velocity.y, -1.0f);
		trail.enabled = true;
		PlayRandomSoundFx (exitSounds);
		Invoke ("Remove", exitDelay);
	}

	// Debug drawing
	void OnDrawGizmos() {
		if (isTargeted) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawCube (transform.position, Vector3.one);
		}
	}
}
