using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour {
	
	public static TransitionManager instance = null;
	public float 					secondsPerLetter = 0.1f;
	public Sprite					beginningAndEndSprite;
	public Sprite					midGameSprite;
	public Sprite 					nuclearBlastSprite;

	private Animator dialogueBoxAnimator;
	private Canvas 	transitionCanvas;
	private Canvas 	inGameTextCanvas;
	private Image	transitionImage;
	private Text 	storyText;
	private Text 	scoreText;
	private Text 	inGameText;
//	private Text 	obituariesText;
	private Button 	continueButton;
	private int 	animatedTextCount = 0;
	private int		levelTextToDisplay = 0;
	private float	inGameTextDisappearDelay = 2.0f;
	private bool 	userHitSkip = false;
	private bool	isDialogueBoxAnimating = false;

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
		transitionImage = transitionCanvas.GetComponentInChildren<Image> ();
		storyText = GameObject.Find ("StoryText").GetComponent<Text>();
		scoreText = GameObject.Find ("ScoreText").GetComponent<Text>();
		inGameText = GameObject.Find ("InGameText").GetComponent<Text>();
		continueButton = GetComponentInChildren<Button> ();
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

	IEnumerator	AnimateText(Text targetText, string stringToAnimate) {
		animatedTextCount++;
		targetText.text = "";
		int i = 0;
		for( /* i */ ; i < stringToAnimate.Length; i++) {
			if (userHitSkip)
				break;
			targetText.text += stringToAnimate [i];
			yield return new WaitForSeconds(secondsPerLetter);
		}
		targetText.text += stringToAnimate.Substring (i);
		animatedTextCount--;
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
		instance.inGameText.text = "";
		instance.StartCoroutine(instance.DisplayStoryText ());
	}

	// 0-5 in order, then  GameOver is 6 and 7
	public IEnumerator DisplayStoryText() {
		if (levelTextToDisplay > 0 && levelTextToDisplay < 7)
			DisplayScoreText ();
		else
			scoreText.text = "";
		
		yield return StartCoroutine (AnimateText (storyText, story [levelTextToDisplay]));
		if (levelTextToDisplay == 7) {
			yield return new WaitForSeconds (1.0f);
			yield return UIManager.instance.StartCoroutine (UIManager.instance.FadeToBlack (true));
			yield return new WaitForSeconds (0.5f);
			transitionImage.sprite = nuclearBlastSprite;
			yield return UIManager.instance.StartCoroutine (UIManager.instance.FadeToClear());
			SoundManager.instance.PlayBombSound ();
			UIManager.instance.StartCoroutine (UIManager.instance.ShakeObject (GameObject.FindGameObjectWithTag ("MainCamera")));
		}
	}
		
	public void DisplayScoreText() {
		string scoreString = "ROBOTS FIRED THIS LEVEL: " + HUDManager.instance.robotsFiredThisLevel;
		scoreString += "\nREPAIRS MADE THIS LEVEL: " + HUDManager.instance.repairsThisLevel;
		scoreString += "\nBOXES PROCESSED THIS LEVEL: " + HUDManager.instance.boxesThisLevel;
		StartCoroutine (AnimateText (scoreText, scoreString));
	}

	public IEnumerator ExpandDialogueBox() {
		isDialogueBoxAnimating = true;
		dialogueBoxAnimator.SetTrigger ("ExpandDialogueBox");

		while (isDialogueBoxAnimating)
			yield return null;
	}

	public IEnumerator ContractDialogueBox() {
		inGameText.text = "";
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
		transitionCanvas.enabled = false;
		yield return StartCoroutine (ExpandDialogueBox ());
		yield return StartCoroutine (AnimateText (inGameText, inGameDialogue [levelTextToDisplay]));
		yield return new WaitForSeconds (inGameTextDisappearDelay);
		yield return StartCoroutine (ContractDialogueBox ());
		DisableTransitionManager ();
	}

	public void DisableTransitionManager() {
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
		"That should do it!\nI hope they all work!",
		"A few broken bots?\nHardly a problem!",
		"This should be enough to get started.\nNow to craft our next generation...POSTERITY!",
		"I know! I'll simply make more police bots,\nthey will most definitely handle the problem!",
		"Looks like we'll need quite a few more hands\nto help build this \"problem hold place\".",
		"I know! I can build robots who can! Okay.\nTOTALLY know what I'm doing this time."
	};
}
