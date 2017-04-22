using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour {
	
	public static TransitionManager instance = null;
	public float secondsPerLetter = 0.1f;

	private Text storyText;
	private Text scoreText;
	private Text inGameText;
//	private Text obituariesText;
	private Button continueButton;

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
	}

	void Start () {
		storyText = GameObject.Find ("StoryText").GetComponent<Text>();
		scoreText = GameObject.Find ("ScoreText").GetComponent<Text>();
		inGameText = GameObject.Find ("InGameText").GetComponent<Text>();
		continueButton = GetComponentInChildren<Button> ();
	}

	// TODO: enable a small backdrop image for the in-game dialogue
	// TODO: make InGameText part of the HUDCanvas, and make it dissappear after its been fully displayed for a few seconds (or skipped twice [once to display all at once, again to make it go away])

	IEnumerator	AnimateText(Text targetText, string stringToAnimate) {
		targetText.text = "";
		bool userHitSkip = false;
		continueButton.interactable = false;
	
		for(int i = 0; i < stringToAnimate.Length; i++) {
			targetText.text += stringToAnimate [i];
			if (Input.anyKeyDown) 
				userHitSkip = true;
			yield return userHitSkip ? null : new WaitForSeconds(secondsPerLetter);
		}
		continueButton.interactable = true;
	}

	// 0-5 in order, then  GameOver is 6 and 7
	public void DisplayStoryText(int level) {
		instance.StartCoroutine (instance.AnimateText (storyText, story [level]));
		if (level > 0)
			DisplayScoreText ();
	}

	// FIXME(?): scoreString may wind up null beyond this scope
	public void DisplayScoreText() {
		string scoreString = "ROBOTS FIRED THIS LEVEL: " + HUDManager.instance.robotsFiredThisLevel;
		scoreString += "\nREPAIRS MADE THIS LEVEL: " + HUDManager.instance.repairsThisLevel;
		scoreString += "\nBOXES PROCESSED THIS LEVEL: " + HUDManager.instance.boxesThisLevel;
		instance.StartCoroutine (instance.AnimateText (scoreText, scoreString));
	}
		
	public void DisplayInGameDialogue(int level) {
		instance.StartCoroutine (instance.AnimateText (inGameText, inGameDialogue [level]));
	}

	private string[] story = { 
		"FINALLY! Months of rust and blazing heat have not kept me from my mission...\nto resurrect humanity. What're these drooping banners?\n\"NOT FIT FOR USE\"? This could be a problem. I will not let it be a problem!",
		"Unfortunately they don't seem to be functional for more than a couple minutes at a time.\nNo matter! I have assembled a team to expedite the process. \nMy mission will be complete soon enough.",
		"Success! Now we must educate these robots with some of humanity's finest works:\n\"To Kill a Mocking Bird\", \"Wizard of Oz\", \"Red Threat\", \"The Art of War\", \n\"Smurfs\", \"Harold and Kumar go to White Castle\", \"IT\", \"Casa Blanca\"",
		"Progress report, in general, a success. They have learned greatly from the ancient teachings.\nWe already have a cooking bot, a cowboy bot, even a teaching bot.\nThough in hindsight, allowing for gangster bot and murder bot was probably ill-advised.",
		"Have those gangster bots paid off the police bots!? \nWhy are the police bots beating that poor robot!? This. Was a bad idea.\nAdvisor bot assures me that building a place for \"problem bots\" will prevent further issues.",
		"Progress report. Ten percent- fifteen per- twenty percent of the population\nhas now been incarcerated. Wow, I am horrible at this.\nThey need compentent leadership, proper planning. I just can't give them that.",
		"This is not looking good.\nThe leader bots have divided everyone and their just screaming at each other.",
		"Perhaps the next location will be more suited to our needs."
	};

	private string[] inGameDialogue = {
		"That should do it!\nI hope they all work!",
		"This should be enough to get started.\nNow to craft our next generation...POSTERITY!",
		"I know! I'll simply make more police bots,\nthey will most definitely handle the problem!",
		"Looks like we'll need quite a few more hands\nto help build this \"problem hold place\".",
		"I know! I can build robots who can! Okay.\nTOTALLY know what I'm doing this time."
	};
}
