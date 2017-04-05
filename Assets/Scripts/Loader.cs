using UnityEngine;
using System.Collections;

public class Loader : MonoBehaviour	{
	public GameObject gameManagerPrefab;
	public GameObject soundManagerPrefab;
	public GameObject robotGrabberPrefab;

	void Awake () {
		if (GameManager.instance == null)
			Instantiate(gameManagerPrefab);
		
		if (SoundManager.instance == null)
			Instantiate(soundManagerPrefab);

		if (RobotGrabber.instance == null)
			Instantiate(robotGrabberPrefab);
	}
}