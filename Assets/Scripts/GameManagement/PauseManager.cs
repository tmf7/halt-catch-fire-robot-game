using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour {

	// cross-scene persistent object for the in-game pause menu's visibility
	// handled by the UIManager
	public static PauseManager 	instance = null;

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
	}
}
