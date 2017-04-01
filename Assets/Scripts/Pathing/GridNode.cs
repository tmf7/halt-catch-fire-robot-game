using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode : IHeapItem<GridNode> {

	public bool 	walkable;
	public Vector2 	worldPosition;
	public int 		gridRow;
	public int 		gridCol;

	public GridNode	parent;
	public int 		gCost;
	public int 		hCost;

	private int 	heapIndex;

	public GridNode(Vector2 _worldPos, bool _walkable, int _gridRow, int _gridCol) {
		walkable = _walkable;
		worldPosition = _worldPos;
		gridRow = _gridRow;
		gridCol = _gridCol;
	}

	public int fCost{
		get { 
			return gCost + hCost;
		}
	}

	public int HeapIndex{
		get { 
			return heapIndex;
		}
		set { 
			heapIndex = value;
		}
	}

	public int CompareTo(GridNode nodeToCompare) {
		int compare = fCost.CompareTo (nodeToCompare.fCost);
		if (compare == 0)
			compare = hCost.CompareTo (nodeToCompare.hCost);
		return -compare;
	}
}
