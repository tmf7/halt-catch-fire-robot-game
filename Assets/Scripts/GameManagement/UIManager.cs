using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UIManager : MonoBehaviour {

	public Sprite 			pauseSprite;
	public Sprite 			unpauseSprite;
	public Sprite 			muteSprite;
	public Sprite 			unmuteSprite;

	public static UIManager instance = null;

	private GameObject[]	showOnPause;
	private Image			muteImage;
	private Image			pauseImage;
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
		showOnPause = GameObject.FindGameObjectsWithTag("ShowOnPause");
		muteImage = GameObject.Find ("MuteImage").GetComponent<Image>();
		ToggleOverlay ();
	}

    void Update() {
		if (Input.GetButtonDown ("Cancel")) { 		// set to escape key in project settings, other simultaneous keys can be added (eg: Pause/Break key)
			ToggleOverlay ();
			TogglePause ();
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
		if (buildIndex == 0)
			SoundManager.instance.PlayMenuMusic ();
		else
			SoundManager.instance.PlayGameMusic ();

		// TODO: show and play intermission and gameover stuff

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

	void InitScene() {
		Time.timeScale = 1.0f;
		instance.showOnPause = GameObject.FindGameObjectsWithTag("ShowOnPause");
		instance.muteImage = GameObject.Find ("MuteImage").GetComponent<Image>();
		GameObject pauseObject = GameObject.Find ("PauseImage");
		if (pauseObject != null)
			instance.pauseImage = pauseObject.GetComponent<Image> ();
		
		GameManager.instance.InitLevel ();
		if (SceneManager.GetActiveScene ().buildIndex > 0) {		// MainMenu is buildIndex 0
			instance.musicSlider = GameObject.Find ("MusicSlider").GetComponent<Slider> ();
			instance.sfxSlider = GameObject.Find ("SFxSlider").GetComponent<Slider> ();
		}
		instance.UpdateSoundConfiguration ();
		instance.ToggleOverlay ();
	}
		
	public void TogglePause() {
		if (instance.pauseImage == null)
			return;

		Time.timeScale = Time.timeScale == 1.0f ? 0.0f : 1.0f;
		instance.pauseImage.sprite = Time.timeScale == 1.0f ? instance.pauseSprite : instance.unpauseSprite;
	}

	public void ToggleOverlay() {
		foreach (GameObject showItem in instance.showOnPause)
			showItem.SetActive (!showItem.activeSelf);
	}

	public void ToggleMute() {
		SoundManager.instance.ToggleMasterMute();
		instance.muteImage.sprite = SoundManager.instance.isMuted ? instance.muteSprite : instance.unmuteSprite;
	}

	private void UpdateSoundConfiguration() {
		if (SceneManager.GetActiveScene ().buildIndex > 0) {		// MainMenu is buildIndex 0
			musicSlider.value = SoundManager.instance.musicVolume;
			sfxSlider.value = SoundManager.instance.sfxVolume;
		}
		instance.muteImage.sprite = SoundManager.instance.isMuted ? instance.muteSprite : instance.unmuteSprite;
	}
}
