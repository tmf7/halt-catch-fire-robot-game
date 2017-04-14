using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour {

	public AudioClip menuMusic;
	public AudioClip gameMusic;
	public AudioClip intermissionSound;
	public AudioClip gameOverMusic;
	public AudioMixer sfxMixer;			// control the volume only

	public static SoundManager instance = null;

	private AudioSource musicSource;
	private float savedSfxAttenuation = 0.0f;
	private float savedMusicVolume = 1.0f;

	void Awake () {
		if (instance == null)
			instance = this;
		else if (instance != null)
			Destroy (gameObject);

		DontDestroyOnLoad (gameObject);

		musicSource = GetComponent<AudioSource> ();
	}

	public void PlayMenuMusic() {
		musicSource.clip = menuMusic;
		musicSource.Play ();
	}

	public void PlayGameMusic() {
		musicSource.clip = gameMusic;
		musicSource.Play ();
	}

	public void PlayIntermissionMusic() {
		musicSource.clip = intermissionSound;
		musicSource.Play ();
	}

	public void PlayGameOverMusic() {
		musicSource.clip = gameOverMusic;
		musicSource.Play ();
	}

	public void ToggleMasterMute() {
		musicSource.mute = !musicSource.mute;
		if (!musicSource.mute) {
			musicSource.volume = savedMusicVolume;
			sfxMixer.SetFloat ("Attenuation", savedSfxAttenuation);
		} else {
			sfxMixer.SetFloat ("Attenuation", -80.0f);
		}
	}

	public bool isMuted {
		get {
			return musicSource.mute;
		}
	}
		
	// min 0, max 1 range set in UI slider
	public void SetMusicVolume(float volume) {
		savedMusicVolume = volume;
		if (!isMuted)
			musicSource.volume = savedMusicVolume;
	}

	// min -80, max 0 range set in UI slider and mixer properties
	public void SetSFxVolume(float attenuation) {
		savedSfxAttenuation = attenuation;
		if (!isMuted)
			sfxMixer.SetFloat ("Attenuation", savedSfxAttenuation);
	}
}
