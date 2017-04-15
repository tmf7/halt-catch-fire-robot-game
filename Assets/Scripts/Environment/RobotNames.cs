using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class RobotNames {

	private static RobotNames instance = null;

	private RobotNames() {
		
	}

	public static RobotNames Instance {
		get { 
			if (instance == null) 
				instance = new RobotNames ();
			return instance;
		}
	}

	public string GetUnusedName() {
		int tryIndex = 0;
		int tryCount = 0;
		bool used = false;
		do {
			tryIndex = Random.Range (0, robotNames.Length);
			used = robotNames [tryIndex].used;
			tryCount++;
		} while (used && tryCount < robotNames.Length);

		if (!used) {
			robotNames [tryIndex].used = true;
			return robotNames[tryIndex].name;
		} else {
			ResetUnusedNames ();
			return "Reset";
		}
	}

	private void ResetUnusedNames() {
		for (int i = 0; i < robotNames.Length; i++)
			robotNames [i].used = false;
	}	

	struct Name {
		public string name;
		public bool used;

		public Name(string _name, bool _used = false) {
			name = _name;
			used = _used;
		}
	}

	// 55 names
	private Name[] robotNames = { 
		new Name("Bob"),
		new Name("Ethan"),
		new Name("Rupert"),
		new Name("Nathan"),
		new Name("Olivia"),
		new Name("John"),
		new Name("Jade"),
		new Name("Quincy"),
		new Name("Dilbert"),
		new Name("Sarah"),
		new Name("Kristen"),
		new Name("Blert"),
		new Name("Nine"),
		new Name("Fish"),
		new Name("Kate"),
		new Name("Jessica"),
		new Name("Doris"),
		new Name("Betty"),
		new Name("Jack"),
		new Name("K9"),
		new Name("Ronald"),
		new Name("Jorsh"),
		new Name("Tom"),
		new Name("Brandon"),
		new Name("Russell"),
		new Name("Atron"),
		new Name("Kevin"),
		new Name("Kyle"),
		new Name("Jarett"),
		new Name("Nikolai"),
		new Name("Sebastian"),
		new Name("Ana"),
		new Name("Devin"),
		new Name("Bread"),
		new Name("Rerun"),
		new Name("Radar"),
		new Name("Domo"),
		new Name("Roboto"),
		new Name("Lucy"),
		new Name("Gladis"),
		new Name("Mlem"),
		new Name("Rick"),
		new Name("Mo"),
		new Name("Mike"),
		new Name("Kit"),
		new Name("Kat"),
		new Name("Nora"),
		new Name("Keaton"),
		new Name("Kathy"),
		new Name("Mosh"),
		new Name("Dobble"),
		new Name("Diskette"),
		new Name("Disk"),
		new Name("Morty"),
		new Name("Dice")
	};
}