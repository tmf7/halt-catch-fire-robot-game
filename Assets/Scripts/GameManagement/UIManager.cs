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

	private GameObject[]	overlayObjects;
	private Animator 		screenFaderAnimator;
	private Slider			musicSlider;
	private Slider			sfxSlider;
	private Action 			faderCallback;
	private int				levelToLoad;

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	
		
		DontDestroyOnLoad(gameObject);
	}

	void Start() {
		screenFaderAnimator = GetComponent<Animator> ();
		screenFaderAnimator.speed = 1.0f / transitionTime;
		instance.musicSlider = GameObject.Find ("MusicSlider").GetComponent<Slider> ();
		instance.sfxSlider = GameObject.Find ("SFxSlider").GetComponent<Slider> ();
		InitScene ();
	}

    void Update() {
		if (Input.GetButtonDown ("Cancel")) { 		// set to escape key in project settings, other simultaneous keys can be added (eg: Pause/Break key)
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

	public void QuitGame() {
		Application.Quit ();
	}
		
    public void ResetLevel() {
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

	// scene 0 in the build must be set to the MainMenu scene
	public void LoadRandomLevel() {
		int buildIndex = Random.Range (1, SceneManager.sceneCountInBuildSettings);
		FadeToLevel (buildIndex);
	}

	public void FadeToLevel (int buildIndex) {
		instance.levelToLoad = buildIndex;
		GameManager.instance.enabled = false;
		instance.faderCallback = instance.LoadLevel;
		instance.screenFaderAnimator.SetTrigger ("FadeToBlack");
	}

	public void FadeToGameOver() {
		storyToTell = 4;
		FadeToStory ();
	}

	public void FadeToStory() {
		DisableCurrentScene ();
		Cursor.visible = true;
		RobotGrabber.instance.gameObject.SetActive (false);
		GameManager.instance.enabled = false;
		instance.faderCallback = instance.ShowStory;
		instance.screenFaderAnimator.SetTrigger ("FadeToBlack");
	}

	public void SetFaderCallback(Action callback) {
		instance.faderCallback = callback;
	}

	// FadeToBlack/Clear animations invoke this
	public void ExecuteFaderCommand() {
		if (faderCallback != null)
			instance.faderCallback ();
	}

	public void LoadLevel () {
		SceneManager.LoadScene (levelToLoad);
	}
		
	public void ShowStory() {
		HUDManager.instance.gameObject.SetActive (false);
		TransitionManager.instance.gameObject.SetActive (true);
		TransitionManager.instance.StartIntermission (storyToTell++);
		SoundManager.instance.PlayIntermissionMusic ();
		instance.faderCallback = null;
		instance.screenFaderAnimator.SetTrigger ("FadeToClear");
	}

	// HACK: prevents scene from continuing to process while intermission plays overtop before the next scene loads
	public void DisableCurrentScene() {
		GameObject[] allSceneObjects = SceneManager.GetActiveScene ().GetRootGameObjects ();
		foreach (GameObject item in allSceneObjects) {
			if (item.tag == "MainCamera")
				continue;

			item.SetActive (false);
		}
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
		instance.screenFaderAnimator.SetTrigger ("FadeToClear");
	}

	void InitScene() {
		if (Time.timeScale == 0.0f)
			TogglePause();
		
		// TODO: show and play intermission and gameover stuff

		if (!isSceneMainMenu) {
			GameManager.instance.enabled = true;
			HUDManager.instance.gameObject.SetActive (true);
			RobotGrabber.instance.gameObject.SetActive (true);
			SoundManager.instance.PlayGameMusic ();
			GameManager.instance.InitLevel ();
			TransitionManager.instance.StartInGameDialogue();
			Cursor.visible = false;
		} else {
			GameManager.instance.enabled = false;
			HUDManager.instance.gameObject.SetActive (false);
			RobotGrabber.instance.gameObject.SetActive (false);
			TransitionManager.instance.DisableTransitionManager ();
			SoundManager.instance.PlayMenuMusic ();
			Cursor.visible = true;
		}
		instance.UpdateSoundConfiguration ();
		PauseManager.instance.gameObject.SetActive (false);
		instance.overlayObjects = GameObject.FindGameObjectsWithTag("Overlay");			// Overlay == CreditsCard
		instance.ToggleOverlay ();
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
