using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSwapButton : MonoBehaviour {

	public Sprite 			onStateSprite;
	public Sprite 			offStateSprite;

	private Image			swapImage;
	private bool			isOn = false;

	void Start () {
		swapImage = GetComponent<Image> ();
	}
	
	public void ToggleImage() {
		isOn = !isOn;
		swapImage.sprite = isOn ? onStateSprite : offStateSprite;
	}
}
