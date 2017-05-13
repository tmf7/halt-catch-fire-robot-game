using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotDoor : MonoBehaviour {

	public AudioClip 	doorSlideSound;
	public Robot 		robotPrefab;
	public SlimeRobot 	slimeRobotPrefab;
	public float 		spawnDelay = 0.5f;

	[HideInInspector]
	public bool			isClosed = true;
	[HideInInspector]
	public bool 		spawnEnabled = true;

	private AudioSource source;
	private Animator	animator;
	private Canvas 		spawnCanvas;
	private Text[] 		robotCostText;
	private bool		spawnSlimeBot = false;

	void Awake() {
		source = GetComponent<AudioSource> ();
		animator = GetComponent<Animator> ();
		spawnCanvas = GetComponentInChildren<Canvas> ();
		robotCostText = GetComponentsInChildren<Text> ();
	}

	void Update () {
		spawnCanvas.enabled = isClosed;
		if (spawnCanvas.enabled) {
			string buildCost = GameManager.instance.robotBuildCost.ToString();
			robotCostText [0].text = buildCost;
			robotCostText [1].text = buildCost;
		}

		animator.enabled = !Robot.isHalted;
	}

	public IEnumerator SpawnSlimeBot() {
		spawnSlimeBot = true;
		GameManager.instance.StopAllRobots();
		SoundManager.instance.PlayLevelEndSound ();

		while (SoundManager.instance.globalSFxSource.isPlaying)
			yield return null;

		TriggerDoorOpen ();
	}

	// RobotDoorOpen animation event triggers this co-routine
	IEnumerator SpawnRobots() {
		if (!spawnSlimeBot) {
			while (Robot.isHalted)
				yield return null;

			while (spawnEnabled && (GameManager.instance.robotCount < GameManager.instance.maxRobots) && (GameManager.instance.robotCount < HUDManager.instance.robotsRemaining)) {
				if (Robot.isHalted) {
					yield return new WaitForSeconds (spawnDelay);
					continue;
				}
				
				Robot spawnedRobot = Instantiate<Robot> (robotPrefab, animator.transform.position, Quaternion.identity);
				GameManager.instance.AddRobot (spawnedRobot);
				yield return new WaitForSeconds (spawnDelay);
			}
		} else {
			Instantiate<SlimeRobot> (slimeRobotPrefab, animator.transform.position, Quaternion.identity);
			yield return new WaitForSeconds (spawnDelay);
		}
		TriggerDoorClose ();
	}

	// button attached to this gameObject invokes this
	public void SpawnOneRobot() {
		int buildCost = GameManager.instance.robotBuildCost;
		if (HUDManager.instance.boxesRemaining >= buildCost && !GameManager.instance.levelEnded) {
			HUDManager.instance.SpendBoxes (buildCost);
			GameManager.instance.IncrementMaxRobots ();
			TriggerDoorOpen ();
		}
	}
		
	// GameManager invokes this on all doors at level beginning
	public void TriggerDoorOpen() {
		isClosed = false;
		animator.SetTrigger ("OpenDoor");
	}

	private void TriggerDoorClose() {
		animator.SetTrigger ("CloseDoor");
	}

	public void SetDoorClosed() {
		isClosed = true;
	}

	private void PlaySlideSound() {
		source.clip = doorSlideSound;
		source.Play ();
	}
}
