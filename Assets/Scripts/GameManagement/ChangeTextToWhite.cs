using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChangeTextToWhite : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	private Text childText;

	void Start () {
        childText = GetComponentInChildren<Text>();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        childText.color = Color.white;
    }


    public void OnPointerExit(PointerEventData eventData) {
        childText.color = Color.black;
    }
}
