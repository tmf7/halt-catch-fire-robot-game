using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RobotNames {

	public int maxNames = 60;

	private static RobotNames instance = null;
	private  Dictionary<string, Name> robotNames;
	private int numNamesUsed = 0;

	private RobotNames() {
		robotNames = new Dictionary<string, Name>();
		foreach (string name in rawNames) {
			robotNames.Add (name, new Name (name));
		}
	}

	public static RobotNames Instance {
		get { 
			if (instance == null) 
				instance = new RobotNames ();
			return instance;
		}
	}

	public int numRobotNames {
		get { 
			return robotNames.Count;
		}
	}

	public bool atMaxNames {
		get { 
			return numNamesUsed >= maxNames;
		}
	}

	public void ResetNames() {
		robotNames.Clear ();
		foreach (string name in rawNames) {
			robotNames.Add (name, new Name (name));
		}
	}

	public void AddRobotSurvivalTime(string name, float timeSurvived, bool died) {
		float currentTimeSurvived = robotNames [name].timeSurvived;
		robotNames [name] = new Name (name, true, died, currentTimeSurvived + timeSurvived);
	}

	// TODO: call this after each level for the intermission/summary screen
	public Dictionary<string, int> GetObituaries() {
		Dictionary<string, int> obituaries = new Dictionary<string, int> ();

		foreach (KeyValuePair<string, Name> pair in robotNames) {
			if (pair.Value.died)
				obituaries.Add (pair.Value.name, Mathf.RoundToInt(pair.Value.timeSurvived));
		}
		return obituaries;
	}

	public string GetUnusedName() {
		string tryName = null;
		int tryCount = 0;
		bool used = false;
		do {
			tryName = rawNames [Random.Range (0, rawNames.Length)];
			used = robotNames[tryName].used;
			tryCount++;
		} while (used && tryCount < robotNames.Count);

		if (!used) {
			robotNames [tryName] = new Name (tryName, true);
			return robotNames [tryName].name;
		} else {	// the list is running low, stop trying randomly, just find an unused one
			foreach (KeyValuePair<string, Name> pair in robotNames) {
				if (!pair.Value.used)
					return pair.Value.name;
			}
		}
		return "Ghost";		// GameOver should occur before this happens (Ghost has no Name properties for the obituaries and will cause game breaking exceptions)
	}

	// GetUnusedName can be called maxNames times before GameOver
	// this function increases maxNames UP TO 60 (the count of the robotNames dictionary)
	public void IncreaseAvailableNames(int increaseBy) {
		maxNames += increaseBy;
		if (maxNames > robotNames.Count)
			maxNames = robotNames.Count;
	}

	public struct Name {
		public string name;
		public bool used;
		public bool died;
		public float timeSurvived;

		public Name(string _name, bool _used = false, bool _died = false, float _timeSurvived = 0.0f) {
			name = _name;
			used = _used;
			died = _died;
			timeSurvived = _timeSurvived;
		}
	};

	// 60 names
	private string[] rawNames = {
		"Bob",
		"Ethan",
		"Ruptert",
		"Nathan",
		"Olivia",
		"John",
		"Jade",
		"Quincy",
		"Dilbert",
		"Sarah",
		"Kristen",
		"Blert",
		"Nine",
		"Fish",
		"Kate",
		"Jessica",
		"Doris",
		"Betty",
		"Jack",
		"K9",
		"Ronald",
		"Jorsh",
		"Tom",
		"Brandon",
		"Russell",
		"Atron",
		"Kevin",
		"Kyle",
		"Jarett",
		"Nikolai",
		"Sebastian",
		"Ana",
		"Devin",
		"Bread",
		"Rerun",
		"Radar",
		"Domo",
		"Roboto",
		"Lucy",
		"Gladis",
		"Mlem",
		"Rick",
		"Maureen",
		"Mike",
		"Kit",
		"Kat",
		"Nora",
		"Keaton",
		"Kathy",
		"Mosh",
		"Dobble",
		"Diskette",
		"Disk",
		"Morty",
		"Dice",
		"Robert Paulson",
		"Armondo",
		"Emily",
		"Zeek",
		"Allons-y Alonso"
	};
}