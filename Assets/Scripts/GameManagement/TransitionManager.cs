using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour {
	
	public static TransitionManager instance = null;
	public float					visibleTimePerObituary = 1.0f;
	public float 					secondsPerLetter = 0.02f;
	public Sprite					beginningAndEndSprite;
	public Sprite					midGameSprite;
	public Sprite 					nuclearBlastSprite;

	private Animator 				dialogueBoxAnimator;
	private Canvas 					transitionCanvas;
	private Canvas 					inGameTextCanvas;
	private ScrollRect 				obituariesScrollRect;
	private Image					transitionImage;
	private Text[] 					storyText = new Text[2];
	private Text[] 					scoreText = new Text[2];
	private Text[] 					inGameText = new Text[2];
	private Text[]					obituariesText = new Text[2];
	private Button 					continueButton;
	private int 					animatedTextCount = 0;
	private int						levelTextToDisplay = 0;
	private float					inGameTextDisappearDelay = 2.0f;
	private bool 					userHitSkip = false;
	private bool					isDialogueBoxAnimating = false;

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
	}

	void Start () {
		dialogueBoxAnimator = GetComponent<Animator> ();
		transitionCanvas = GameObject.Find ("TransitionCanvas").GetComponent<Canvas> ();
		inGameTextCanvas = GameObject.Find ("InGameTextCanvas").GetComponent<Canvas> ();
		obituariesScrollRect = GameObject.Find ("ScrollViewObject").GetComponent<ScrollRect> ();
		transitionImage = transitionCanvas.GetComponentInChildren<Image> ();

		// the string shadow and the string itself
		GameObject[] storyTextObjects = GameObject.FindGameObjectsWithTag ("StoryText");
		storyText[0] = storyTextObjects[0].GetComponent<Text>();
		storyText[1] = storyTextObjects[1].GetComponent<Text>();

		GameObject[] scoreTextObjects = GameObject.FindGameObjectsWithTag ("ScoreText");
		scoreText[0] = scoreTextObjects[0].GetComponent<Text>();
		scoreText[1] = scoreTextObjects[1].GetComponent<Text>();

		GameObject[] inGameTextObjects = GameObject.FindGameObjectsWithTag ("InGameText");
		inGameText[0] = inGameTextObjects[0].GetComponent<Text>();
		inGameText[1] = inGameTextObjects[1].GetComponent<Text>();

		GameObject[] obituaryTextObjects = GameObject.FindGameObjectsWithTag ("ObituaryText");
		obituariesText[0] = obituaryTextObjects[0].GetComponent<Text>();
		obituariesText[1] = obituaryTextObjects[1].GetComponent<Text>();

		continueButton = GetComponentInChildren<Button> ();
		ResetTextActivity ();
	}

	void Update() {
		if (animatedTextCount > 0) {
			continueButton.interactable = false;
			if (Input.anyKeyDown) 
				userHitSkip = true;
		} else {
			continueButton.interactable = true;			
			userHitSkip = false;
		}
	}

	// targetText must contain 2 items (the main text, and its dropshadow text)
	IEnumerator	AnimateText(Text[] targetText, string stringToAnimate) {
		animatedTextCount++;
		targetText[0].text = "";
		targetText[1].text = "";
		int i = 0;
		for( /* i */ ; i < stringToAnimate.Length; i++) {
			if (userHitSkip)
				break;
			targetText[0].text += stringToAnimate [i];
			targetText[1].text += stringToAnimate [i];
			yield return new WaitForSeconds(secondsPerLetter);
		}
		targetText[0].text += stringToAnimate.Substring (i);
		targetText[1].text += stringToAnimate.Substring (i);
		animatedTextCount--;
	}

	public void ResetTextActivity() {
		obituariesScrollRect.verticalNormalizedPosition = 1.0f;
		obituariesScrollRect.gameObject.SetActive (false);
		storyText[0].enabled = true;
		storyText[1].enabled = true;
	}

	public void StartIntermission(int storyToTell) {
		instance.transitionCanvas.enabled = true;
		if (storyToTell == 0 || storyToTell == 7)
			instance.transitionImage.sprite = beginningAndEndSprite;
		else
			instance.transitionImage.sprite = midGameSprite;

		instance.inGameTextCanvas.enabled = false;
		instance.levelTextToDisplay = storyToTell;
		animatedTextCount = 0;
		instance.inGameText[0].text = "";
		instance.inGameText[1].text = "";
		instance.StartCoroutine(instance.DisplayStoryText ());
	}

	// 0-5 in order, then  GameOver is 6 and 7
	public IEnumerator DisplayStoryText() {
		if (levelTextToDisplay > 0 && levelTextToDisplay < 7)
			DisplayScoreText ();
		else {
			continueButton.gameObject.SetActive (levelTextToDisplay == 0);
			scoreText[0].text = "";
			scoreText[1].text = "";
		}
		
		yield return StartCoroutine (AnimateText (storyText, story [levelTextToDisplay]));
		if (levelTextToDisplay == 7) 
			StartCoroutine(PlayGameOver ());
	}

	private IEnumerator PlayGameOver () {
		yield return new WaitForSeconds (1.0f);
		yield return UIManager.instance.StartCoroutine (UIManager.instance.FadeToBlack (true));

		yield return new WaitForSeconds (0.5f);
		storyText[0].enabled = false;
		storyText[1].enabled = false;
		transitionImage.sprite = nuclearBlastSprite;
		GameManager.instance.KillAllRobots ();

		yield return UIManager.instance.StartCoroutine (UIManager.instance.FadeToClear());

		SoundManager.instance.PlayBombSound ();
		UIManager.instance.ShakeObject (GameObject.FindGameObjectWithTag ("MainCamera"));
		StartCoroutine (ScrollObituaries ());
		DisplayScoreText ();

		SoundManager.instance.PlayGameOverMusic ();
		while (SoundManager.instance.musicSource.isPlaying)
			yield return null;

		continueButton.gameObject.SetActive (true);
	}

	private IEnumerator ScrollObituaries() {
		List<string> obituaries = RobotNames.Instance.GetObituaries ();
		float verticalUnitsPerLine = 14.0f; 				// emperical from 77 lines at 1000 vertical unity units for 0.05 y-scale
		float textBoxHeight = 2 * verticalUnitsPerLine;		// 2 newlines
		string masterObituary = "\n\n";

		foreach (string obituary in obituaries) {
			masterObituary += "\n\n" + obituary + "\n\n";	// each obituary is one line
			textBoxHeight += verticalUnitsPerLine * 5.0f;	// resulting in 5 lines
		}

		obituariesText[0].rectTransform.sizeDelta = new Vector2(obituariesText[0].rectTransform.sizeDelta.x, textBoxHeight);
		obituariesText[0].text = masterObituary;
		obituariesText[1].rectTransform.sizeDelta = new Vector2(obituariesText[0].rectTransform.sizeDelta.x, textBoxHeight);
		obituariesText[1].text = masterObituary;
		obituariesScrollRect.gameObject.SetActive (true);

		// FIXME(~): the scrollSpeed cant be too slow or it doesn't scroll at all
		float totalScrollTime = visibleTimePerObituary * obituaries.Count ;
		float obituaryScrollSpeed = 1.0f / totalScrollTime;

		while (obituariesScrollRect.verticalNormalizedPosition > 0.0f) {
			float newScrollPos = obituariesScrollRect.verticalNormalizedPosition - (Time.deltaTime * obituaryScrollSpeed);
			obituariesScrollRect.verticalNormalizedPosition = newScrollPos;
			yield return null;
		}
	}
		
	public void DisplayScoreText() {
		string scoreString = "DEFAULT SCORE STRING";
		if (levelTextToDisplay == 7) {
			scoreString = "TOTAL BOXES LAUNCHED: " + HUDManager.instance.totalBoxesCollected;
			scoreString += "\nTOTAL FIRES EXTINGUISHED: " + HUDManager.instance.totalFiresPutOut;
			scoreString += "\nTOTAL ROBOTS BUILT: " + HUDManager.instance.totalRobotsBuilt;
		} else {
			scoreString = "LEVEL PERFORMANCE:";
			scoreString +=  "\nBOXES LAUNCHED: " + HUDManager.instance.boxesThisLevel;
			scoreString += "\t\t\t\t\tFIRES EXTINGUISHED: " + HUDManager.instance.firesPutOutThisLevel;
			scoreString += "\nBUILT ROBOTS: " + HUDManager.instance.robotsBuiltThisLevel + "   ";
			scoreString += "\t\t\t\t\t\tTERMINATED ROBOTS: " + HUDManager.instance.robotsFiredThisLevel;
		}
		StartCoroutine (AnimateText (scoreText, scoreString));
	}

	public IEnumerator ExpandDialogueBox() {
		isDialogueBoxAnimating = true;
		dialogueBoxAnimator.SetTrigger ("ExpandDialogueBox");

		while (isDialogueBoxAnimating)
			yield return null;
	}

	public IEnumerator ContractDialogueBox() {
		inGameText[0].text = "";
		inGameText[1].text = "";
		isDialogueBoxAnimating = true;
		dialogueBoxAnimator.SetTrigger ("ContractDialogueBox");

		while (isDialogueBoxAnimating)
			yield return null;
	}

	public void DialogueBoxAnimationComplete() {
		instance.isDialogueBoxAnimating = false;
	}

	public void StartInGameDialogue() {
		instance.StartCoroutine (instance.StartInGameDialogueCoroutine ());
	}
		
	// ExpandDialogueBox animation enables the InGameTextCanvas
	// ContractDialogueBox animation disable the InGameTextCanvas
	public IEnumerator StartInGameDialogueCoroutine() {
		yield return StartCoroutine (ExpandDialogueBox ());
		yield return StartCoroutine (AnimateText (inGameText, inGameDialogue [levelTextToDisplay]));
		yield return new WaitForSeconds (inGameTextDisappearDelay);
		yield return StartCoroutine (ContractDialogueBox ());
		DisableTransitionManager ();
	}

	public void DisableStoryCanvas() {
		transitionCanvas.enabled = false;
	}

	public void DisableTransitionManager() {
		isDialogueBoxAnimating = false;
		dialogueBoxAnimator.Play ("NoDialogBox", 0, 0.0f);
		StopAllCoroutines ();
		inGameTextCanvas.enabled = false;
		gameObject.SetActive (false);
	}

	private string[] story = { 
		"FINALLY! Months of rust and blazing heat have not kept me from my mission...\nto resurrect humanity. What do these drooping banners say?\n\"NOT FIT FOR USE\"? This could be a problem. I will not let it be a problem!",
		"Unfortunately they don't seem to be functional for more than a couple minutes at a time.\nNo matter! I have assembled a team to expedite the process. \nMy mission will soon be complete.",
		"Success! Now we must educate these robots with some of humanity's finest works:\n\"To Kill a Mocking Bird\", \"Wizard of Oz\", \"Red Threat\", \"The Art of War\", \n\"Smurfs\", \"Harold and Kumar go to White Castle\", \"IT\", \"Casablanca\"...",
		"Progress report, in general, a success. They have learned greatly from the ancient teachings.\nWe already have a cooking bot, a cowboy bot, even a teaching bot.\nThough in hindsight, allowing for gangster bot and murder bot was probably ill-advised.",
		"Have those gangster bots paid off the police bots!? \nWhy are the police bots beating that poor robot!? This. Was a bad idea.\nAdvisor bot assures me that building a place for \"problem bots\" will prevent further issues.",
		"Progress report. Ten percent- fifteen per- twenty percent of the population\nhas now been incarcerated. Wow, I am horrible at this.\nThey need compentent leadership, proper planning. I just can't give them that.",
		"This is not looking good.\nThe leader bots have divided everyone and they're just screaming at each other.",
		"Perhaps the next location will be more suited to our needs."
	};

	private string[] inGameDialogue = {
		"I hope these new robots\ncan launch the boxes\nwithout any issues!",
		"A few broken robots?\nHardly a problem!",
		"This should be enough to get started.\nNow to craft our next generation...POSTERITY!",
		"I know! I'll simply make more police bots,\nthey will most definitely handle the problem!",
		"Looks like we'll need quite a few more hands\nto help build this \"problem hold place\".",
		"I know! I can build leader bots! Okay.\nTOTALLY know what I'm doing this time."
	};
}
