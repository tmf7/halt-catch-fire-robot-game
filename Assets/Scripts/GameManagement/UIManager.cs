using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using System;

public class UIManager : MonoBehaviour {

	public static UIManager instance = null;
	public float 			transitionTime = 0.5f;
	public int				storyToTell = 0;
	public const float 		shakeDuration = 3.0f;
	public const float 		shakeSpeed = 20.0f;
	public const float 		shakeIntensity = 0.25f;

	private IEnumerator 	shakeCoroutineInstance = null;
	private GameObject[]	overlayObjects;
	private Animator 		screenFaderAnimator;
	private Image			screenFaderImage;
	private Slider			musicSlider;
	private Slider			sfxSlider;
	private bool			isFading = false;
	private bool			inTransition = false;

	void Awake() {
		if (instance == null) {
			instance = this;
			DontDestroyOnLoad(gameObject);
		} else if (instance != this) {
			Destroy (gameObject);	
		}
	}

	void Start() {
		screenFaderAnimator = GetComponent<Animator> ();
		screenFaderImage = GetComponentInChildren<Image> ();
		screenFaderAnimator.speed = 1.0f / transitionTime;
		instance.musicSlider = GameObject.Find ("MusicSlider").GetComponent<Slider> ();
		instance.sfxSlider = GameObject.Find ("SFxSlider").GetComponent<Slider> ();
		InitScene ();
	}

    void Update() {
		if (Input.GetButtonDown ("Cancel") && !inTransition) { 		// set to escape key in project settings, other simultaneous keys can be added (eg: Pause/Break key)
			if (isSceneMainMenu)
				ToggleOverlay ();
			else
				TogglePause ();
		}
    }

	public bool isSceneMainMenu {
		get { 
			return SceneManager.GetActiveScene ().buildIndex == 0;
		}
	}

	public bool isSceneHighScores {
		get { 
			return SceneManager.GetActiveScene ().buildIndex == 1;
		}
	}

	public void QuitGame() {
		Application.Quit ();
	}
		
    public void ResetLevel() {
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

	public IEnumerator FadeToBlack(bool whiteInstead = false) {
		inTransition = true;
		isFading = true;
		if (whiteInstead)
			screenFaderImage.color = Color.white;
		else
			screenFaderImage.color = Color.black;
		instance.screenFaderAnimator.SetTrigger ("FadeToBlack");

		while (isFading)
			yield return null;
	}

	public IEnumerator FadeToClear() {
		isFading = true;
		instance.screenFaderAnimator.SetTrigger ("FadeToClear");

		while (isFading)
			yield return null;
	}

	public void FadeComplete() {
		instance.isFading = false;
	}

	// invoked by MainMenu button
	public void FadeToHighScores () {
		HighScoreManager.instance.CheckHighScores ();
		instance.StartCoroutine (instance.FadeToLevelCoroutine (1));
	}
		
	// scene 0 in the build must be set to the MainMenu scene
	public void LoadRandomLevel () {
		int buildIndex = Random.Range (2, SceneManager.sceneCountInBuildSettings);
		instance.FadeToLevel (buildIndex);
	}

	public void FadeToLevel (int buildIndex) {
		if (buildIndex == 0 || instance.storyToTell >= 7) {						// MainMenu button or GameOver
			if (HighScoreManager.instance.CheckHighScores())
				instance.StartCoroutine (instance.FadeToLevelCoroutine (1));	// go to HighScores, or
			else
				instance.StartCoroutine (instance.FadeToLevelCoroutine (0));	// return to MainMenu
		} else if (instance.storyToTell < 6) {
			instance.StartCoroutine (instance.FadeToLevelCoroutine (buildIndex));
		} else if (instance.storyToTell < 7) {
			instance.FadeToStory ();
			instance.storyToTell++;
		} 
	}
		
	public IEnumerator FadeToLevelCoroutine (int buildIndex) {
		GameManager.instance.enabled = false;
		yield return instance.StartCoroutine (instance.FadeToBlack ());

		SceneManager.LoadScene (buildIndex);

		yield return instance.StartCoroutine (instance.FadeToClear ());
	}

	public void FadeToGameOver() {
		storyToTell = 6;
		FadeToLevel (0);
	}

	public void FadeToStory() {
		instance.StartCoroutine (instance.FadeToStoryCoroutine ());
	}

	private IEnumerator FadeToStoryCoroutine () {

		yield return instance.StartCoroutine (instance.FadeToBlack ());

		RobotGrabber.instance.gameObject.SetActive (false);
		HUDManager.instance.gameObject.SetActive (false);
		DisableCurrentScene ();
		GameManager.instance.enabled = false;
		Cursor.visible = true;
		TransitionManager.instance.gameObject.SetActive (true);
		TransitionManager.instance.StartIntermission (storyToTell);
		SoundManager.instance.PlayIntermissionMusic ();

		yield return instance.StartCoroutine (instance.FadeToClear ());
	}

	// FIXME(~): hack to prevent scene from continuing to process while intermission plays overtop before the next scene loads
	private void DisableCurrentScene() {
		GameObject[] allSceneObjects = SceneManager.GetActiveScene ().GetRootGameObjects ();
		foreach (GameObject item in allSceneObjects) {
			if (item.tag == "MainCamera")
				continue;

			item.SetActive (false);
		}
	}

	// convenience function to issue multiple shakes without overlap
	public void ShakeObject (GameObject obj, bool loose = false, float duration = shakeDuration, float speed = shakeSpeed, float intensity = shakeIntensity) {
		if (shakeCoroutineInstance != null)
			instance.StopCoroutine (shakeCoroutineInstance);
		shakeCoroutineInstance = instance.ShakeObjectCoroutine (obj, loose, duration, speed, intensity);
		instance.StartCoroutine (shakeCoroutineInstance);
	}

	public IEnumerator ShakeObjectCoroutine (GameObject obj, bool loose, float duration, float speed, float intensity) {
		Vector3 originalPosition = obj.transform.position;
		float elapsed = 0.0f;
		while (elapsed < duration && Time.timeScale == 1.0f) {
			if (obj != null) {
				float damping = Mathf.Clamp01 ((duration - elapsed) / duration);
				float dampedMag = damping * intensity;
				float xOffset = (dampedMag * Mathf.PerlinNoise (Time.time * speed, 0.0f)) - (dampedMag / 2.0f);	
				float yOffset = (dampedMag * Mathf.PerlinNoise (0.0f, Time.time * speed)) - (dampedMag / 2.0f);

				if (loose)
					obj.transform.localPosition = new Vector3 (obj.transform.position.x + xOffset, obj.transform.position.y + yOffset, obj.transform.position.z);
				else
					obj.transform.localPosition = new Vector3 (originalPosition.x + xOffset, originalPosition.y + yOffset, originalPosition.z);
				
				elapsed += Time.deltaTime;
				yield return null;
			} else {
				yield break;
			}
		}
		if (!loose)
			obj.transform.position = originalPosition;
	}

	//this is called only once, and the paramter tell it to be called only after the scene was loaded
	//(otherwise, our Scene Load callback would be called the very first load, and we don't want that)
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static public void CallbackInitialization() {
		//register the callback to be called everytime the scene is loaded
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	static private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1) {
		instance.InitScene();
	}

	private void InitScene() {
		if (Time.timeScale == 0.0f)
			TogglePause();
		if (Robot.isHalted)
			HUDManager.instance.ToggleHaltButton ();
		
		if (isSceneMainMenu) {
			GameManager.instance.enabled = false;
			HUDManager.instance.gameObject.SetActive (false);
			RobotGrabber.instance.gameObject.SetActive (false);
			HighScoreManager.instance.gameObject.SetActive (false);
			TransitionManager.instance.DisableTransitionManager ();
			SoundManager.instance.PlayMenuMusic ();
			Cursor.visible = true;
			ResetGame ();
		} else if (isSceneHighScores) {
			GameManager.instance.enabled = false;
			HUDManager.instance.gameObject.SetActive (false);
			RobotGrabber.instance.gameObject.SetActive (false);
			TransitionManager.instance.DisableTransitionManager ();
			SoundManager.instance.PlayIntermissionMusic ();
			HighScoreManager.instance.gameObject.SetActive (true);
			Cursor.visible = true;
			if (HighScoreManager.instance.isNameInputVisible)
				HighScoreManager.instance.PlayCongratulations ();
		} else {
			HighScoreManager.instance.gameObject.SetActive (false);
			GameManager.instance.enabled = true;
			HUDManager.instance.gameObject.SetActive (true);
			HUDManager.instance.ResetSprinklerCooldown ();
			RobotGrabber.instance.gameObject.SetActive (true);
			RobotNames.Instance.ResetSurvivorNamesUsed ();
			SoundManager.instance.PlayGameMusic ();
			GameManager.instance.InitLevel ();
			TransitionManager.instance.DisableStoryCanvas ();
			TransitionManager.instance.StartInGameDialogue();
			Cursor.visible = false;
			storyToTell++;
		}
		instance.inTransition = false;
		instance.UpdateSoundConfiguration ();
		PauseManager.instance.gameObject.SetActive (false);
		instance.overlayObjects = GameObject.FindGameObjectsWithTag("Overlay");			// Overlay == CreditsCard
		instance.ToggleOverlay ();
	}
		
	private void ResetGame() {
		RobotNames.Instance.ResetNames ();
		GameManager.instance.ResetMaxRobots ();
		HUDManager.instance.ResetLevelStats ();
		HUDManager.instance.ResetGameStats ();
		TransitionManager.instance.ResetTextActivity ();
		storyToTell = 0;
	}
		
	public void TogglePause() {
		Time.timeScale = Time.timeScale == 1.0f ? 0.0f : 1.0f;
		RobotGrabber.instance.enabled = Time.timeScale == 1.0f;	
		Cursor.visible = Time.timeScale == 0.0f; 
		PauseManager.instance.gameObject.SetActive (Time.timeScale == 0.0f);
		HUDManager.instance.TogglePauseButtonImage ();
	}

	public void ToggleOverlay() {
		foreach (GameObject overlayItem in instance.overlayObjects)
			overlayItem.SetActive (!overlayItem.activeSelf);
	}

	private void UpdateSoundConfiguration() {
		if (!isSceneMainMenu) {
			musicSlider.value = SoundManager.instance.musicVolume;
			sfxSlider.value = SoundManager.instance.sfxVolume;
		}
	}
}
