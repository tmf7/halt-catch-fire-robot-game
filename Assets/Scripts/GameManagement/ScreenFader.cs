using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenFader : MonoBehaviour {

	public bool fadeComplete = false;

	// FadeToBlack animation event calls this
	public void FadeComplete() {
		fadeComplete = true;
	}
}
