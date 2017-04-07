using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour {

	public AudioSource musicSource;
	public static SoundManager instance = null;

	void Awake () {
		if (instance == null)
			instance = this;
		else if (instance != null)
			Destroy (gameObject);

		DontDestroyOnLoad (gameObject);
	}

	/////////////////////////////////////////////
	///////////// MENU OPTIONS///////////////////
	/////////////////////////////////////////////
	/// 
	public void PlayMusic(AudioClip clip) {
		musicSource.clip = clip;
		musicSource.Play ();
	}

	public void PauseMusic() {
		musicSource.Pause ();	
	}

	public void StopMusic() {
		musicSource.Stop ();
	}

	public void MuteMusic(bool mute) {
		musicSource.mute = mute;
	}

	public bool IsMusicPlaying() {
		return musicSource.isPlaying;
	}
		
	public void SetMusicVolume(float volume) {
		musicSource.volume = Mathf.Clamp01(volume);
	}
}
