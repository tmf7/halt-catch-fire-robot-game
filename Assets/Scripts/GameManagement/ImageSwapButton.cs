using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSwapButton : MonoBehaviour {

	public Sprite 			onStateSprite;
	public Sprite 			offStateSprite;

	protected bool			isOn = false;

	private Image			swapImage;

	void Start () {
		swapImage = GetComponent<Image> ();
	}
	
	public void ToggleImage() {
		isOn = !isOn;
		swapImage.sprite = isOn ? onStateSprite : offStateSprite;
	}
}
