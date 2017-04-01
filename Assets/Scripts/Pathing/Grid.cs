using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour {

	public bool displayGridGizmos;

	public LayerMask unwalkableMask;
	public Vector2 gridWorldSize;
	public float nodeRadius;

	private GridNode[,] grid;
	private float nodeDiameter;
	private int numGridRows;
	private int numGridCols;

	void Awake () {
		nodeDiameter = 2 * nodeRadius;
		numGridRows = Mathf.RoundToInt (gridWorldSize.x / nodeDiameter);
		numGridCols = Mathf.RoundToInt (gridWorldSize.y / nodeDiameter);
		CreateGrid ();
	}

	public int MaxSize {
		get { 
			return numGridRows * numGridCols;
		}
	}
	
	void CreateGrid () {
		grid = new GridNode[numGridRows, numGridCols];
		Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x * 0.5f - Vector3.up * gridWorldSize.y * 0.5f;

		for (int row = 0; row < numGridRows; row++) {
			for (int col = 0; col < numGridCols; col++) {
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (row * nodeDiameter + nodeRadius) + Vector3.up * (col * nodeDiameter + nodeRadius);
				bool walkable = Physics2D.BoxCast (worldPoint, Vector2.one * nodeDiameter, 0.0f, Vector2.zero, 0.0f, unwalkableMask).collider == null;
				grid [row, col] = new GridNode (worldPoint, walkable, row, col);
			}
		}
	}

	public List<GridNode> GetNeighbors(GridNode node) {
		List<GridNode> neighbors = new List<GridNode> ();

		for (int row = -1; row <= 1; row++) {
			for (int col = -1; col <= 1; col++) {
				if (row == 0 && col == 0)
					continue;

				int checkRow = node.gridRow + row;
				int checkCol = node.gridCol + col;
				if (checkRow >= 0 && checkRow < numGridRows && checkCol >= 0 && checkCol < numGridCols)
					neighbors.Add (grid [checkRow, checkCol]);
			}	
		}
		return neighbors;
	}

	public GridNode NodeFromWorldPoint(Vector3 worldPosition) {
		float percentX = (worldPosition.x + gridWorldSize.x * 0.5f) / gridWorldSize.x;
		float percentY = (worldPosition.y + gridWorldSize.y * 0.5f) / gridWorldSize.y;
		percentX = Mathf.Clamp01 (percentX);
		percentY = Mathf.Clamp01 (percentY);

		int row = Mathf.RoundToInt (percentX * (numGridRows - 1));
		int col = Mathf.RoundToInt (percentY * (numGridCols - 1));
		return grid [row, col];
	}

	// Debug Drawing
	void OnDrawGizmos() {
		Gizmos.DrawWireCube (transform.position, new Vector3 (gridWorldSize.x, gridWorldSize.y, 1.0f));
		if (displayGridGizmos && grid != null) {
			foreach (GridNode n in grid) {
				Gizmos.color = (n.walkable) ? Color.white : Color.red;
				Gizmos.DrawCube (n.worldPosition, Vector3.one * nodeDiameter);
			}
		}
	}
}
