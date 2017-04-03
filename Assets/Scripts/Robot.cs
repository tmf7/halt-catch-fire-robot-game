using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Robot : Throwable {

	public Transform 	target;
	public float 		speed = 5.0f;
	public float 		targetSlowdownDistance = 10.0f;
	public float 		grabHeight = 1.0f;

	[HideInInspector]
	public bool 		grabbed;

	private Vector3[] 	path;
	private int 		targetIndex;
	private const float	pathUpdateMoveThreshold = 0.5f;
	private const float minPathUpdateTime = 0.2f;
	private const float stoppingThreshold = 0.01f;
	private bool 		justReleased;

	private Animator 	animator;
	private Vector3 	oldMousePosition;
	private float 		mouseSpeed;

	void Start() {
		animator = GetComponent<Animator> ();
		StartCoroutine (UpdatePath ());
	}

	public void OnPathFound(Vector3[] newPath, bool pathSuccessful) {
		if (!grabbed && pathSuccessful) {
			path = newPath;
			targetIndex = 0;
			StopCoroutine ("FollowPath");
			StartCoroutine ("FollowPath");
		}
	}

	void Update() {
		mouseSpeed = (Input.mousePosition - oldMousePosition).magnitude / Time.deltaTime;
		oldMousePosition = Input.mousePosition;

		if (grabbed) {

			justReleased = false;
			target = null;
			StopCoroutine ("FollowPath");
			StopCoroutine (UpdatePath ());
			SetHeight (grabHeight);

		} else if (!grounded && !justReleased) {
			justReleased = true;
			Vector2 mouseDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
			print ("MOUSE DIR: " + mouseDir);
			float throwSpeed = mouseDir.magnitude;
			mouseDir.Normalize();

			rb2D.velocity = new Vector2 (throwSpeed * mouseDir.x, throwSpeed * mouseDir.y);
			Throw (0.0f, -1.0f);
			// throw at a speed relative to the mouse velocity, maybe... the airTime would be ... hmm
			// give it negative air time to avoid trajectory (due to the vertical drop)
		} else if (grounded && !landingResolved) {
			rb2D.transform.rotation = Quaternion.identity;
			rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;
			StopCoroutine (UpdatePath());
			StartCoroutine (UpdatePath ());
		}
		UpdateShadow ();
	}

	bool CheckTarget() {
		if (target != null) {
			return true;
		} else if (BoxThrower.allBoxes.Count > 0) {
			// all-systems-normal, pick a random box
			target = BoxThrower.allBoxes [Random.Range (0, BoxThrower.allBoxes.Count - 1)].transform;
			return true;
		} 
		return false;
	}

	IEnumerator UpdatePath() {
		// prevents large Time.deltaTime values when the game first starts up
		if (Time.timeSinceLevelLoad < 0.3f) {
			yield return new WaitForSeconds (0.3f);
		}

		while (!CheckTarget()) {
			yield return null;
		} 
		PathRequestManager.RequestPath (transform.position, target.position, OnPathFound);

		float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
		Vector3 targetPosOld = target.position;

		while (true) {
			yield return new WaitForSeconds (minPathUpdateTime);
			if (CheckTarget() && (target.position - targetPosOld).sqrMagnitude > sqrMoveThreshold) {
				print ("UPDATING PATH");
				PathRequestManager.RequestPath (transform.position, target.position, OnPathFound);
				targetPosOld = target.position;
			}
		}
	}

	IEnumerator FollowPath() {
		Vector3 currentWaypoint = path [0];
		float sqrTargetSlowdownDistance = targetSlowdownDistance * targetSlowdownDistance;

		while (true) {
			print ("FOLLOWING PATH");
			if (transform.position == currentWaypoint) {
				targetIndex++;
				if (targetIndex >= path.Length) {
					yield break;
				}
				currentWaypoint = path [targetIndex];
			}

			float percentSpeed = 1.0f;
			float sqrRange = (path [path.Length - 1] - transform.position).sqrMagnitude;
			if (sqrRange < sqrTargetSlowdownDistance) {
				percentSpeed = Mathf.Clamp01 (Mathf.Sqrt (sqrRange) / targetSlowdownDistance);
				if (percentSpeed < stoppingThreshold)
					yield break;
			}
				
			Vector3 oldPosition = transform.position;
			transform.position = Vector3.MoveTowards (transform.position, currentWaypoint, speed * percentSpeed * Time.deltaTime);
			if ((transform.position - oldPosition).x < 0.0f)
				animator.SetBool ("WalkLeft", true);
			else
				animator.SetBool ("WalkLeft", false);

			yield return null;
		}
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (collision.collider.tag == "Box") { // AND the box is the target box...or just pickup the first box you hit, its all the same
			print("BOX ACQUIRED");
			Box hit = collision.collider.gameObject.GetComponent<Box> ();
			hit.ExplodeBox ();
		}
		// if the robot has collided with its target (and its a box~)
		// then pick the box up (shift its y a little up)
		// and start moving with the box (give the Box object a posseser, or assign a gameobject to a private variable in Robot, or both)
	}

	// Debug Drawing
	public void OnDrawGizmos() {
		if (path != null) {
			for (int i = targetIndex; i < path.Length; i++) {
				Gizmos.color = Color.black;
				Gizmos.DrawCube (path [i], Vector3.one);

				if (i == targetIndex) {
					Gizmos.DrawLine (transform.position, path [i]);
				} else {
					Gizmos.DrawLine (path [i - 1], path [i]);	
				}
			}
		}
	}
}
