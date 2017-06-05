using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RobotNames {

	private static RobotNames instance = null;
	private  Dictionary<string, Name> robotNames = new Dictionary<string, Name>();
	private int survivorNamesUsed = 0;

	private RobotNames() {
		ResetNames ();
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

	// BUGFIX: for play that uses more than the stock 100 names
	private void ResurrectNames() {
		survivorNamesUsed = 0;
		foreach (KeyValuePair<string, Name> name in robotNames) {
			Name revivedRobot = name.Value;
			robotNames [name.Key] = new Name (revivedRobot.name, false, false, revivedRobot.timeSurvived, revivedRobot.boxesDelivered);
		}
	}

	public void ResetNames() {
		survivorNamesUsed = 0;
		robotNames.Clear ();
		foreach (string name in rawNames) {
			robotNames.Add (name, new Name (name));
		}
	}

	public void ResetSurvivorNamesUsed() {
		survivorNamesUsed = 0;
	}

	public void AddRobotSurvivalTime(string name, float timeSurvived, bool died = false, MethodOfDeath howRobotDied = MethodOfDeath.SURVIVED) {
		Name robotName = robotNames [name];
		robotNames [name] = new Name (name, true, died, robotName.timeSurvived + timeSurvived, robotName.boxesDelivered, howRobotDied);
	}

	public void AddRobotBoxDelivery(string name) {
		Name robotName = robotNames [name];
		robotNames [name] = new Name (name, true, false, robotName.timeSurvived, robotName.boxesDelivered + 1);
	}

	public List<string> GetObituaries() {
		List<string> obituaries = new List<string> ();

		// sort by boxesDelivered then timeSurvived
		List<Name> sortedNames = new List<Name> ();
		foreach (KeyValuePair<string, Name> pair in robotNames) {
			if (pair.Value.died)
				sortedNames.Add (pair.Value);
		}
		NameComparer nc = new NameComparer ();
		sortedNames.Sort (nc);

		foreach (Name robot in sortedNames) {
			obituaries.Add (robot.name + " delivered " + robot.boxesDelivered + " boxes, " + GetDeathString(robot.howDied) + Mathf.RoundToInt(robot.timeSurvived) + " seconds.");
		}
		return obituaries;
	}

	private string GetDeathString(MethodOfDeath howRobotDied) {
		switch (howRobotDied) {
			case MethodOfDeath.DEATH_BY_CRUSHER:
				return "and was crushed after ";
			case MethodOfDeath.DEATH_BY_FIRE:
				return "and got fired after ";
			case MethodOfDeath.DEATH_BY_PIT:
				return "and fell in a pit after ";
			case MethodOfDeath.DEATH_BY_BOMB:
				return "and was obliterated after ";
			default:
				return "and continued to live? after ";
		}
	}

	public string TryGetSurvivorName () {
		int skipNameCount = 0;
		foreach (KeyValuePair<string, Name> pair in robotNames) {
			if (pair.Value.used && !pair.Value.died && survivorNamesUsed <= skipNameCount++) {
				survivorNamesUsed++;
				return pair.Value.name;
			}
		}
		survivorNamesUsed++;
		HUDManager.instance.BuildRobot ();
		return GetUnusedName ();
	}

	private string GetUnusedName() {
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
			return tryName;
		} else {	// the list is running low, stop trying randomly, just find an unused one
			foreach (KeyValuePair<string, Name> pair in robotNames) {
				if (!pair.Value.used) {
					robotNames[pair.Value.name] = new Name (pair.Value.name, true);
					return pair.Value.name;
				}
			}
		}

		// FIXME(~): should never reach these lines during normal play (average robots in 15 mins is about 60)
		ResurrectNames ();
		return GetUnusedName();
	}

	public int maxAvailableNames {
		get {
			return rawNames.Length;
		}
	}

	public struct Name {
		public string name;
		public bool used;
		public bool died;
		public float timeSurvived;
		public int boxesDelivered;
		public MethodOfDeath howDied;

		public Name(string _name, bool _used = false, bool _died = false, float _timeSurvived = 0.0f, int _boxesDelivered = 0, MethodOfDeath _howDied = MethodOfDeath.SURVIVED) {
			name = _name;
			used = _used;
			died = _died;
			timeSurvived = _timeSurvived;
			boxesDelivered = _boxesDelivered;
			howDied = _howDied;
		} 
	};

	public enum MethodOfDeath {
		SURVIVED,
		DEATH_BY_CRUSHER,
		DEATH_BY_PIT,
		DEATH_BY_FIRE,
		DEATH_BY_BOMB
	};

	// 120 names
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
		"Kristen",		// 10
		"Blert",
		"Nine",
		"Fish",
		"Kate",
		"Jessica",
		"Doris",
		"Betty",
		"Jack",
		"K9",
		"Ronald",		// 20
		"Jorsh",
		"Tom",
		"Brandon",
		"Russell",
		"Atron",
		"Kevin",
		"Kyle",
		"Jarett",
		"Nikolai",
		"Sebastian",	// 30
		"Ana",
		"Devin",
		"Bread",
		"Rerun",
		"Radar",
		"Domo",
		"Roboto",
		"Lucy",
		"Gladis",
		"Mlem",			// 40
		"Rick",
		"Maureen",
		"Mike",
		"Kit",
		"Kat",
		"Nora",
		"Keaton",
		"Kathy",
		"Mosh",
		"Dobble",		// 50
		"Diskette",
		"Disk",
		"Morty",
		"Dice",
		"Robert Paulson",
		"Armondo",
		"Emily",
		"Zeek",
		"Allons-y Alonso",
		"Dwalin",		// 60
		"Balin",
		"Kili",
		"Fili",
		"Dori",
		"Nori",
		"Ori",
		"Oin",
		"Gloin",
		"Bifur",
		"Bofur",		// 70
		"Bombur",
		"Thorin",
		"Bilbo",
		"Spock",
		"Kirk",
		"Bones",
		"Uhura",
		"7 of 9",
		"Borg",
		"Cloud",		// 80
		"Cid",
		"Tifa",
		"Aeris",
		"Barret",
		"Vincent",
		"Cait Sith",
		"Yuffie",
		"Sephiroth",
		"Chrono",
		"Red XIII",		// 90
		"Gainsborough",
		"Lockhart",
		"Kisaragi",
		"Strife",
		"Wallace",
		"Valentine",
		"Highwind",
		"Marle",
		"Lucca",
		"Frog",			// 100
		"Robo",
		"Ayla",
		"Magus",
		"Lavos",
		"Isaac",
		"Schrodinger",
		"Dalton",
		"Heisenberg",
		"Mendeleev",
		"Ada Lovelace",// 110
		"Marie Curie",
		"Rosa Parks",
		"George W. Carver",
		"MLK Jr.",
		"Harvey Milk",
		"Frederick Douglass",
		"Harriet",
		"Tubman",
		"Gosling",
		"McGregor"		// 120
	};

	public class NameComparer: Comparer<Name> {
		public override int Compare (Name a, Name b) {
			if (a.boxesDelivered > b.boxesDelivered || (a.boxesDelivered == b.boxesDelivered && a.timeSurvived > b.timeSurvived))
				return -1;
			else if (a.boxesDelivered < b.boxesDelivered || (a.boxesDelivered == b.boxesDelivered && a.timeSurvived < b.timeSurvived))
				return 1;
			return 0;
		}
	}
}