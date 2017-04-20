using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UIManager : MonoBehaviour {

	public static UIManager instance = null;

	private GameObject[]	overlayObjects;
	private Slider			musicSlider;
	private Slider			sfxSlider;

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	
		
		DontDestroyOnLoad(gameObject);
	}

	void Start() {
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
		LoadLevel (buildIndex);
	}
		
	public void LoadLevel(int buildIndex) {
		SceneManager.LoadScene (buildIndex, LoadSceneMode.Single);
	}

	//this is called only once, and the paramter tell it to be called only after the scene was loaded
	//(otherwise, our Scene Load callback would be called the very first load, and we don't want that)
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static public void CallbackInitialization() {
		//register the callback to be called everytime the scene is loaded
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	//This is called each time a scene is loaded.
	static private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1) {
		instance.InitScene();
	}


	// FIXME: this function may no longer be necessary given the persistence of the musicSlider, sfxSlider, and the different pauseMenu visibility setup via an object.enabled
	void InitScene() {
		if (Time.timeScale == 0.0f)
			TogglePause();

		// TODO: show and play intermission and gameover stuff

		if (!isSceneMainMenu) {
			HUDManager.instance.gameObject.SetActive (true);
			RobotGrabber.instance.gameObject.SetActive (true);
			PauseManager.instance.gameObject.SetActive (true);
			SoundManager.instance.PlayGameMusic ();
			instance.musicSlider = GameObject.Find ("MusicSlider").GetComponent<Slider> ();
			instance.sfxSlider = GameObject.Find ("SFxSlider").GetComponent<Slider> ();
			GameManager.instance.InitLevel ();
			Cursor.visible = false;
		} else {
			HUDManager.instance.gameObject.SetActive (false);
			RobotGrabber.instance.gameObject.SetActive (false);
			SoundManager.instance.PlayMenuMusic ();
			Cursor.visible = true;
		}
		instance.UpdateSoundConfiguration ();
		PauseManager.instance.gameObject.SetActive (false);
		instance.overlayObjects = GameObject.FindGameObjectsWithTag("Overlay");
		instance.ToggleOverlay ();
	}
		
	public void TogglePause() {
		Time.timeScale = Time.timeScale == 1.0f ? 0.0f : 1.0f;
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
