using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour {

	public AudioSource musicSource;
	public AudioSource globalSFxSource;

	public AudioClip selectSound;
	public AudioClip clickSound;
	public AudioClip levelEndSound;
	public AudioClip bigExplosionSound;

	public AudioClip menuMusic;
	public AudioClip gameMusic;
	public AudioClip intermissionSound;
	public AudioClip gameOverMusic;

	public AudioMixer sfxMixer;			// control the volume only

	public static SoundManager instance = null;

	private float savedSfxAttenuation = 0.0f;
	private float savedMusicVolume = 1.0f;

	void Awake () {
		if (instance == null)
			instance = this;
		else if (instance != null)
			Destroy (gameObject);

		DontDestroyOnLoad (gameObject);
	}

	public void PlaySelectSound() {
		instance.globalSFxSource.clip = selectSound;
		instance.globalSFxSource.Play ();
	}

	public void PlayClickSound() {
		instance.globalSFxSource.clip = clickSound;
		instance.globalSFxSource.Play ();
	}

	public void PlayBombSound() {
		instance.musicSource.Stop ();
		instance.globalSFxSource.clip = bigExplosionSound;
		instance.globalSFxSource.Play ();
	}

	public void PlayLevelEndSound() {
		instance.musicSource.Stop ();
		instance.globalSFxSource.clip = levelEndSound;
		instance.globalSFxSource.Play ();
	}

	public void PlayMenuMusic() {
		instance.musicSource.clip = menuMusic;
		instance.musicSource.Play ();
	}

	public void PlayGameMusic() {
		instance.musicSource.clip = gameMusic;
		instance.musicSource.Play ();
	}

	public void PlayIntermissionMusic() {
		instance.musicSource.clip = intermissionSound;
		instance.musicSource.Play ();
	}

	public void PlayGameOverMusic() {
		instance.musicSource.clip = gameOverMusic;
		instance.musicSource.Play ();
	}

	public void ToggleMasterMute() {
		instance.musicSource.mute = !instance.musicSource.mute;
		if (!instance.musicSource.mute) {
			instance.musicSource.volume = instance.savedMusicVolume;
			instance.sfxMixer.SetFloat ("Attenuation", instance.savedSfxAttenuation);
		} else {
			instance.sfxMixer.SetFloat ("Attenuation", -80.0f);
		}
	}

	public bool isMuted {
		get {
			return instance.musicSource.mute;
		}
	}
		
	public float musicVolume {
		get { 
			return instance.savedMusicVolume;
		}
	}

	public float sfxVolume {
		get { 
			return instance.savedSfxAttenuation;
		}
	}
			
	// min 0, max 1 range set in UI slider
	public void SetMusicVolume(float volume) {
		instance.savedMusicVolume = volume;
		if (!isMuted)
			instance.musicSource.volume = instance.savedMusicVolume;
	}

	// min -80, max 0 range set in UI slider and mixer properties
	public void SetSFxVolume(float attenuation) {
		instance.savedSfxAttenuation = attenuation;
		if (!isMuted)
			instance.sfxMixer.SetFloat ("Attenuation", instance.savedSfxAttenuation);
	}
}
