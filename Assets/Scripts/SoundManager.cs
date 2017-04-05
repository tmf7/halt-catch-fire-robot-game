using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour {

	public AudioSource efxSource;
	public AudioSource musicSource;
	public static SoundManager instance = null;

	public float lowPitchRange = 0.95f;
	public float highPitchRange = 1.05f;

	void Awake () {
		if (instance == null)
			instance = this;
		else if (instance != null)
			Destroy (gameObject);

		DontDestroyOnLoad (gameObject);
	}

	public void PlaySingleSoundFx (AudioClip clip) {
		efxSource.clip = clip;
		efxSource.Play ();
	}

	public void PlayRandomSoundFx(params AudioClip [] clips) {
		int randomIndex = Random.Range (0, clips.Length - 1);
		float randomPitch = Random.Range (lowPitchRange, highPitchRange);

		efxSource.pitch = randomPitch;
		efxSource.clip = clips [randomIndex];
		efxSource.Play ();
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

	public void SetFxVolume(float volume) {
		efxSource.volume = Mathf.Clamp01(volume);
	}

	public void MuteSoundFx(bool mute) {
		efxSource.mute = mute;
	}

	public void PauseAllSound() {
		musicSource.Pause ();
		efxSource.Pause ();
	}
	
	public void StopAllSound() {
		musicSource.Stop ();
		efxSource.Stop ();
	}
}
