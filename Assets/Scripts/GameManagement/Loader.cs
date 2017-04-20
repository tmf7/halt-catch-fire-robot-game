using UnityEngine;
using System.Collections;

public class Loader : MonoBehaviour	{
	
	public GameObject gameManagerPrefab;
	public GameObject soundManagerPrefab;
	public GameObject UIManagerPrefab;
	public GameObject pauseManagerPrefab;
	public GameObject hudManagerPrefab;
	public GameObject robotGrabberPrefab;

	void Awake () {

		if (GameManager.instance == null)
			Instantiate(gameManagerPrefab);

		if (PauseManager.instance == null)
			Instantiate (pauseManagerPrefab);

		if (HUDManager.instance == null)
			Instantiate (hudManagerPrefab);

		if (RobotGrabber.instance == null)
			Instantiate (robotGrabberPrefab);
		
		if (SoundManager.instance == null)
			Instantiate(soundManagerPrefab);

		if (UIManager.instance == null)
			Instantiate (UIManagerPrefab);
	}
}