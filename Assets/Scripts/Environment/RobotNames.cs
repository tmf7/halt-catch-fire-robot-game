using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RobotNames {

	private static RobotNames instance = null;
	private Dictionary<string, bool> unusedNames = new Dictionary<string, bool>();

	private RobotNames() {
		foreach (string name in names)
			unusedNames.Add (name, false);
	}

	public static RobotNames Instance {
		get { 
			if (instance == null) 
				instance = new RobotNames ();
			return instance;
		}
	}

	public string GetUnusedName() {
		string tryName = null;
		int tryCount = 0;
		bool used = false;
		do {
			tryName = names [ Random.Range (0, names.Length)];
			used = unusedNames [tryName];
			tryCount++;
		} while (used && tryCount < unusedNames.Count);

		if (!used) {
			unusedNames [tryName] = true;
			return tryName;
		} else {
			ResetUnusedNames ();
			return GetUnusedName ();
		}
	}

	private void ResetUnusedNames() {
		foreach (KeyValuePair<string, bool> pair in unusedNames)
			unusedNames[pair.Key] = false;
	}
		
	private string[] names = { 
		"Bob",
		"Ethan",
		"Rupert",
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
		"Mo",
		"Mike",
		"Kit",
		"Kat",
		"Nora",
		"Keaton",
		"Kathy",
		"Mosh",
		"Dobble",
		"Diskette",
		"Disk"
	};
}
