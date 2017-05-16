using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class HighScoreManager : MonoBehaviour {

	[Serializable]
	private class HighScores {
		public List<string> names;
		public List<int> scores;

		public HighScores (List<string> _names, List<int> _scores) {
			names = _names;
			scores = _scores;
		}
	}

    public static HighScoreManager 		instance = null;
	public float						repeatWait = 0.5f;
	public float						repeatDelay = 0.1f;
	public int							maxHighScores = 5;
	public int 							upButtonHeld = -1;
	public int							downButtonHeld = -1;

    
	private HighScores 					highScores;
	private Animator 					congratsAnimator;
	private GameObject 					inputObj;
	private Button[] 					nameInputButtons;
    private Text[] 						scoresText;
	private Text[]						inputNameText;
	private char[]						inputName;
    private string 						masterHighScoreFile = "/HaltCatchFire_HighScores.dat";
	private int							highScoreToReplace;
	private float 						nextFireTime;
   
    void Awake() {
		if (instance == null) {
			instance = this;
			DontDestroyOnLoad(gameObject);
			LoadHighScores();
			Init ();
		} else if (instance != this) {
			Destroy (gameObject);	
		}
    }

	void Update () {
		if (upButtonHeld != -1 && Time.time > nextFireTime) {
			IncrementLetter (upButtonHeld);
			nextFireTime = Time.time + repeatDelay;
		}

		if (downButtonHeld != -1 && Time.time > nextFireTime) {
			DecrementLetter (downButtonHeld);
			nextFireTime = Time.time + repeatDelay;
		}
	}
		
	private void Init () {
		congratsAnimator = GetComponent<Animator> ();
		scoresText = GameObject.Find ("HighScores").GetComponentsInChildren<Text> ();
		inputObj = GameObject.Find ("NameInput");
		nameInputButtons = inputObj.GetComponentsInChildren<Button> ();
		inputNameText = inputObj.GetComponentsInChildren<Text> ();
		inputName = new char[3];
		inputName [0] = 'A';
		inputName [1] = 'A';
		inputName [2] = 'A';
		for (int rank = 0; rank < scoresText.Length; rank++)
			scoresText[rank].text = ((rank + 1) + ". " + highScores.names[rank] + "\t\t\t\t" + highScores.scores[rank]);
	}

	public void HoldUpButton (int index) {
		instance.upButtonHeld = index;
		instance.nextFireTime = Time.time + instance.repeatWait;
	}

	public void ReleaseUpButton () {
		instance.upButtonHeld = -1;
	}

	public void HoldDownButton (int index) {
		instance.downButtonHeld = index;
		instance.nextFireTime = Time.time + instance.repeatWait;
	}

	public void ReleaseDownButton () {
		instance.downButtonHeld = -1;
	}

	public bool isNameInputVisible {
		get {
			return inputObj != null && inputObj.activeSelf;
		} 
		set {
			if (inputObj == null)
				return;
			inputObj.SetActive (value);
		}
	}

	public void PlayCongratulations() {
		congratsAnimator.Play ("Congrats", 0, 0.0f);
		SoundManager.instance.PlayCongratsSound ();
	}
		
	// FIXME(~): if rank == maxHighScores EXACTLY then the List capacities grow; if greater, then an EXCEPTION is thrown
	public bool CheckHighScores() {
		int rank = GetCurrentScoreRank ();
		if (rank >= maxHighScores) {
			isNameInputVisible = false;
			return false;
		}
		highScoreToReplace = rank - 1;
		highScores.scores.Insert (highScoreToReplace, HUDManager.instance.totalBoxesCollected);
		highScores.scores.RemoveRange (maxHighScores, highScores.scores.Count - maxHighScores);

		string placeHolder = "Y O U";
		instance.highScores.names.Insert (instance.highScoreToReplace, placeHolder);
		instance.highScores.names.RemoveRange (instance.maxHighScores, instance.highScores.names.Count - maxHighScores);

		isNameInputVisible = true;
		MakeLetterEditable (0);
		for (rank = 0; rank < scoresText.Length; rank++)
			scoresText[rank].text = ((rank + 1) + ". " + highScores.names[rank] + "\t\t\t\t" + highScores.scores[rank]);
		return true;
    }

	// [0-8]: { 1up, 1, 1down, 2up, 2, 2down, 3up, 3, 3down }
	public void MakeLetterEditable (int index) {
		switch (index) {
		case 0:
			instance.nameInputButtons [0].gameObject.SetActive (true);
			instance.nameInputButtons [2].gameObject.SetActive (true);
			instance.nameInputButtons [3].gameObject.SetActive (false);
			instance.nameInputButtons [5].gameObject.SetActive (false);
			instance.nameInputButtons [6].gameObject.SetActive (false);
			instance.nameInputButtons [8].gameObject.SetActive (false);
			break;
		case 1:
			instance.nameInputButtons [0].gameObject.SetActive (false);
			instance.nameInputButtons [2].gameObject.SetActive (false);
			instance.nameInputButtons [3].gameObject.SetActive (true);
			instance.nameInputButtons [5].gameObject.SetActive (true);
			instance.nameInputButtons [6].gameObject.SetActive (false);
			instance.nameInputButtons [8].gameObject.SetActive (false);
			break;
		case 2:
			instance.nameInputButtons [0].gameObject.SetActive (false);
			instance.nameInputButtons [2].gameObject.SetActive (false);
			instance.nameInputButtons [3].gameObject.SetActive (false);
			instance.nameInputButtons [5].gameObject.SetActive (false);
			instance.nameInputButtons [6].gameObject.SetActive (true);
			instance.nameInputButtons [8].gameObject.SetActive (true);
			break;
		}
	}

	public void IncrementLetter (int index) {
		char letter = instance.inputName [index];
		char nextChar;
		if (letter == 'Z')
			nextChar = 'A';
		else
			nextChar = (char)(((int) letter) + 1);

		instance.inputName [index] = nextChar;
		instance.UpdateRankedNameText ();
	}

	public void DecrementLetter (int index) {
		char letter = instance.inputName [index];
		char prevChar;
		if (letter == 'A')
			prevChar = 'Z';
		else
			prevChar = (char)(((int) letter) - 1);
		
		instance.inputName [index] = prevChar;
		instance.UpdateRankedNameText ();
	}

	public void SubmitHighScore () {
		if (isNameInputVisible) {
			string newName = (instance.inputName [0].ToString () + " " + instance.inputName [1].ToString () + " " + instance.inputName [2].ToString ());
			instance.highScores.names [instance.highScoreToReplace] = newName;
			instance.SaveHighScores ();
		}
		UIManager.instance.StartCoroutine (UIManager.instance.FadeToLevelCoroutine (0));	// return to MainMenu
	}

	private void CreateDefaultHighScores() {
		List<string> defaultNames = new List<string> ();
		List<int> defaultScores = new List<int> ();
		for (int i = 0; i < maxHighScores; i++) {
			defaultNames.Add("A A A");
			defaultScores.Add(0);
		}
		highScores = new HighScores (defaultNames, defaultScores);
	}

	// each text element has a drop shadow text element
	private void UpdateRankedNameText() {
		inputNameText[0].text = inputName[0].ToString();
		inputNameText[1].text = inputName[0].ToString();
		inputNameText[2].text = inputName[1].ToString();
		inputNameText[3].text = inputName[1].ToString();
		inputNameText[4].text = inputName[2].ToString();
		inputNameText[5].text = inputName[2].ToString();

		string newName = (inputName [0].ToString () + " " + inputName [1].ToString () + " " + inputName [2].ToString ());
		scoresText[highScoreToReplace].text = ((highScoreToReplace + 1) + ". " + newName + "\t\t\t\t" + highScores.scores [highScoreToReplace]);
	}

	private int GetCurrentScoreRank () {
		int rank = 1;
		foreach (int score in highScores.scores) {
			if (HUDManager.instance.totalBoxesCollected > score)
				return rank;
			else
				rank++;
		}
		return rank;
	}

	private void SaveHighScores () {
		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Create(Application.persistentDataPath + masterHighScoreFile);
		bf.Serialize(file, highScores);
		file.Close();
	}

	// FIXME(~): FileMode.Open simply opens the file to be written into, .Append would seek to the end	
	// IE: garbage data may exists if a different/smaller amount of data is written out
	private void LoadHighScores () {
		if (File.Exists(Application.persistentDataPath + masterHighScoreFile)) {
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + masterHighScoreFile, FileMode.Open);
			highScores = (HighScores)bf.Deserialize(file);														
			file.Close();
		} else {
			CreateDefaultHighScores();
			SaveHighScores ();
		}
	}
}
