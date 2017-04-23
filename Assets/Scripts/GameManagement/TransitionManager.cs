using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour {
	
	public static TransitionManager instance = null;
	public float 					secondsPerLetter = 0.1f;

	private Animator dialogueBoxAnimator;
	private Canvas 	transitionCanvas;
	private Canvas 	inGameTextCanvas;
	private Text 	storyText;
	private Text 	scoreText;
	private Text 	inGameText;
//	private Text 	obituariesText;
	private Button 	continueButton;
	private int 	animatedTextCount = 0;
	private int		levelDialogueToDisplay = 0;
	private float	inGameTextDisappearDelay = 2.0f;
	private bool 	userHitSkip = false;
	private bool	displayingDialogue = false;

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
	}

	void Start () {
		dialogueBoxAnimator = GetComponent<Animator> ();
		transitionCanvas = GameObject.Find ("TransitionCanvas").GetComponent<Canvas> ();		// FIXME: these may return null
		inGameTextCanvas = GameObject.Find ("InGameTextCanvas").GetComponent<Canvas> ();
		storyText = GameObject.Find ("StoryText").GetComponent<Text>();
		scoreText = GameObject.Find ("ScoreText").GetComponent<Text>();
		inGameText = GameObject.Find ("InGameText").GetComponent<Text>();
		continueButton = GetComponentInChildren<Button> ();
	}

	void Update() {
		if (animatedTextCount > 0) {					// FIXME: ensure the dialogueBox width is re-zeroed, as are whatever tags/callbacks that cause the dialogueBox to dissappear (ie the Invoke on the stack)
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
		if (displayingDialogue)
			Invoke ("ContractDialogueBox", inGameTextDisappearDelay);
	}

	public void StartIntermission(int level) {
		print("IS INVOKING B4: " + IsInvoking ());
		instance.transitionCanvas.enabled = true;
		instance.inGameTextCanvas.enabled = false;
		instance.levelDialogueToDisplay = level;
		animatedTextCount = 0;
		instance.inGameText.text = "";
		CancelInvoke ();						// FIXME: sometimes this fails, so do it ASAP, or just make the call a Time.deltaTime type call (instead of an Invoke)
		print("IS INVOKING AFT: " + IsInvoking ());
		instance.DisplayStoryText ();
	}

	// 0-5 in order, then  GameOver is 6 and 7
	public void DisplayStoryText() {
		StartCoroutine (AnimateText (storyText, story [levelDialogueToDisplay]));
		if (levelDialogueToDisplay > 0)
			DisplayScoreText ();
		else
			scoreText.text = "";
	}
		
	public void DisplayScoreText() {
		string scoreString = "ROBOTS FIRED THIS LEVEL: " + HUDManager.instance.robotsFiredThisLevel;
		scoreString += "\nREPAIRS MADE THIS LEVEL: " + HUDManager.instance.repairsThisLevel;
		scoreString += "\nBOXES PROCESSED THIS LEVEL: " + HUDManager.instance.boxesThisLevel;
		StartCoroutine (AnimateText (scoreText, scoreString));
	}

	// DONE-TODO: (1) UIManager.InitScene calls this
	public void SwapTransitionForDialogCanvas() {
		instance.transitionCanvas.enabled = false;
		instance.inGameTextCanvas.enabled = true;
		UIManager.instance.SetFaderCallback (ExpandDialogueBox);
	}

	// DONE-TODO: (2) FadeToClear animation event (configured callback) 
	// DONE-TODO: actually place the animation event in FadeToClear
	public void ExpandDialogueBox() {
		UIManager.instance.SetFaderCallback (null);
		instance.dialogueBoxAnimator.SetTrigger ("ExpandDialogueBox");
	}

	// DONE-TODO: (3) ExpandDialogueBox animation event (at its begnning)
	public void DisplayInGameDialogue() {
		displayingDialogue = true;
		instance.StartCoroutine (instance.AnimateText (inGameText, inGameDialogue [levelDialogueToDisplay]));
	}

	// DONE-TODO: (4) Dialog animation complete (animatedTextCount == 0 again) triggers ContractDialogBox(), after x seconds (see this.Update)
	public void ContractDialogueBox() {
		displayingDialogue = false;
		instance.inGameText.text = "";
		instance.dialogueBoxAnimator.SetTrigger ("ContractDialogueBox");
	}

	// TODO : (5) ContractDialogBox animation event (at its beginning)
	public void DisableTransitionManager() {
		gameObject.SetActive (false);
	}

	private string[] story = { 
		"FINALLY! Months of rust and blazing heat have not kept me from my mission...\nto resurrect humanity. What're these drooping banners?\n\"NOT FIT FOR USE\"? This could be a problem. I will not let it be a problem!",
		"Unfortunately they don't seem to be functional for more than a couple minutes at a time.\nNo matter! I have assembled a team to expedite the process. \nMy mission will be complete soon enough.",
		"Success! Now we must educate these robots with some of humanity's finest works:\n\"To Kill a Mocking Bird\", \"Wizard of Oz\", \"Red Threat\", \"The Art of War\", \n\"Smurfs\", \"Harold and Kumar go to White Castle\", \"IT\", \"Casa Blanca\"...",
		"Progress report, in general, a success. They have learned greatly from the ancient teachings.\nWe already have a cooking bot, a cowboy bot, even a teaching bot.\nThough in hindsight, allowing for gangster bot and murder bot was probably ill-advised.",
		"Have those gangster bots paid off the police bots!? \nWhy are the police bots beating that poor robot!? This. Was a bad idea.\nAdvisor bot assures me that building a place for \"problem bots\" will prevent further issues.",
		"Progress report. Ten percent- fifteen per- twenty percent of the population\nhas now been incarcerated. Wow, I am horrible at this.\nThey need compentent leadership, proper planning. I just can't give them that.",
		"This is not looking good.\nThe leader bots have divided everyone and their just screaming at each other.",
		"Perhaps the next location will be more suited to our needs."
	};

	// FIXME: ensure this aligns with the story (always say something because the edge case of no dialog is more than I care to handle right now)
	private string[] inGameDialogue = {
		"That should do it!\nI hope they all work!",
		"This should be enough to get started.\nNow to craft our next generation...POSTERITY!",
		"I know! I'll simply make more police bots,\nthey will most definitely handle the problem!",
		"Looks like we'll need quite a few more hands\nto help build this \"problem hold place\".",
		"I know! I can build robots who can! Okay.\nTOTALLY know what I'm doing this time."
	};
}
