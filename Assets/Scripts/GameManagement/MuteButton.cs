using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuteButton : ImageSwapButton {

	void Update () {
		if (SoundManager.instance.isMuted && !isOn)
			ToggleImage();
	}
}
