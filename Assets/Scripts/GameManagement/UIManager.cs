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
		if (Input.GetButtonDown ("Cancel")) 		// set to escape key in project settings, other simultaneous keys can be added (eg: Pause/Break key)
			ToggleOverlay ();
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
		
	// FIXME: possibly use instance versions of showOnPause, etc
	public void LoadLevel(int buildIndex) {
		SceneManager.LoadScene (buildIndex);
		showOnPause = GameObject.FindGameObjectsWithTag("ShowOnPause");
		muteImage = GameObject.Find ("MuteImage").GetComponent<Image>();
		GameObject pauseObject = GameObject.Find ("PauseImage");
		if (pauseObject != null)
			pauseImage = pauseObject.GetComponent<Image> ();
		GameManager.instance.InitLevel ();
	}
		
	public void ToggleOverlay() {
		if (instance.pauseImage != null) {
			Time.timeScale = Time.timeScale == 1.0f ? 0.0f : 1.0f;
			instance.pauseImage.sprite = Time.timeScale == 1.0f ? instance.pauseSprite : instance.unpauseSprite;
		}

		foreach (GameObject showItem in instance.showOnPause)
			showItem.SetActive (!showItem.activeSelf);
	}

	public void ToggleMute() {
		SoundManager.instance.ToggleMasterMute();
		instance.muteImage.sprite = SoundManager.instance.isMuted ? instance.muteSprite : instance.unmuteSprite;
	}
}
