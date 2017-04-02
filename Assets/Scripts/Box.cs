using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Box : MonoBehaviour {

	public LayerMask 		airStrikeMask;
	public float 			groundedDrag = 10.0f;
	public float 			strikeHeight;
	public GameObject 		dropShadowPrefab;
	public GameObject 		explosionPrefab;
	public AudioClip		grabBox1;
	public AudioClip 		grabBox2;
	public AudioClip		grabBox3;

	[HideInInspector]
	public float 			airTime;

	private GameObject		dropShadow;
	private Vector3 		shadowVelocity;
	private BoxCollider2D	shadowBox;
	private ShadowController shadowController;

	private BoxCollider2D	boxCollider;
	private Rigidbody2D 	boxRB;
	private float 			midPointTime;
	private float			strikeTime;
	private float 			strikeHeightSqr;
	private bool 			grounded;


	void Awake() {
		// FIXME: non-zero parent is screwing with the calculations (do not parent it to the box, it amplifies the velocity/movement)
		dropShadow = Instantiate<GameObject> (dropShadowPrefab, transform.position, Quaternion.identity);
		strikeHeightSqr = strikeHeight * strikeHeight;
	}

	void Start () {
		shadowController = GetComponentInChildren<ShadowController> ();
		shadowBox = dropShadow.GetComponent<BoxCollider2D> ();
		boxCollider = GetComponent<BoxCollider2D> ();
		boxRB = GetComponent<Rigidbody2D> ();



//		CalculateShadowTrajectory ();
		shadowController.SetVelocity(Vector3.forward * boxRB.velocity.y);
		shadowController.Drop ();


		strikeTime = Time.time + airTime;
		midPointTime = strikeTime - (airTime * 0.5f);
		grounded = false; 
	}

	void Update() {
		if (grounded)
			return;

		float shadowOffset = shadowController.GetShadowOffset ();
		dropShadow.transform.position = transform.position - Vector3.up * shadowOffset;

//		dropShadow.transform.position += shadowVelocity * Time.deltaTime;
//		airTime -= Time.deltaTime;

//		Vector3 shadowToBox = transform.position - dropShadow.transform.position;
//		float heightSqr = shadowToBox.sqrMagnitude;
		if (Time.time > midPointTime && !shadowBox.IsTouching(boxCollider) && shadowOffset < strikeHeight)
			gameObject.layer = LayerMask.NameToLayer ("DynamicCollision");

		if (Time.time > strikeTime) {
			grounded = true;
			boxRB.gravityScale = 0;
			boxRB.drag = groundedDrag;
			boxRB.velocity = Vector2.zero;
			dropShadow.SetActive(false);
		}
	}

	void OnTriggerEnter2D(Collider2D collider) {
//		print ("SHADOW FOUND");
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (!grounded && collision.gameObject.layer == Mathf.Log(airStrikeMask.value,2)) {
			//Or play the sound here for touching a box. I think this might be better.
			ExplodeBox ();
		}
	}

	void CalculateShadowTrajectory() {
		// predicted landing point
		float xFinal = transform.position.x + (boxRB.velocity.x * airTime);
		float yFinal = transform.position.y + (boxRB.velocity.y * airTime) + (0.5f * Physics2D.gravity.y * airTime * airTime);

		// ground movement of the drop shadow
		Vector3 shadowDir = new Vector3 (xFinal, yFinal) - transform.position;
		float distance = shadowDir.magnitude;
		shadowDir.Normalize ();
		float shadowSpeed = distance / airTime;
		shadowVelocity = new Vector3 (shadowSpeed * shadowDir.x, shadowSpeed * shadowDir.y);
	}

	public void ExplodeBox() {
		SoundManager.instance.RandomizeSFx (grabBox1, grabBox2, grabBox3);
		Instantiate<GameObject> (explosionPrefab, transform.position, Quaternion.identity);
		RemoveBox();
	}

	void RemoveBox() {
		BoxThrower.allBoxes.Remove (this);		// FIXME: dont have BoxThrower manage the list of all boxes
		Destroy(shadowController);				// FIXME: may also need to destroy its script instance (hopefully not)
		Destroy(dropShadow);
		Destroy(gameObject);
		Destroy (this);
	}
}
