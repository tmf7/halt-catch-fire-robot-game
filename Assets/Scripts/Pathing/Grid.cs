using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Grid : MonoBehaviour {

	public bool displayGridGizmos;
	public int 	pathingBlurSize = 8;
	public int 	obstacleProximityPenalty = 10;

	public LayerMask unwalkableMask;
	public Vector2 gridWorldSize;
	public float nodeRadius;
	public TerrainType[] walkableRegions;

	private Dictionary<int, int> walkableRegionsDict = new Dictionary<int, int> ();
	private LayerMask walkableMask;
	private GridNode[,] grid;
	private float nodeDiameter;
	private int numGridRows;
	private int numGridCols;
	private int penaltyMin = int.MaxValue;
	private int penaltyMax = int.MinValue;

	void Awake () {
		nodeDiameter = 2 * nodeRadius;
		numGridRows = Mathf.RoundToInt (gridWorldSize.x / nodeDiameter);
		numGridCols = Mathf.RoundToInt (gridWorldSize.y / nodeDiameter);

		foreach (TerrainType region in walkableRegions) {
			walkableMask |= region.terrainMask.value;
			walkableRegionsDict.Add ((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
		}
		
		CreateGrid ();
	}

	public int MaxSize {
		get { 
			return numGridRows * numGridCols;
		}
	}
	
	void CreateGrid () {
		grid = new GridNode[numGridRows, numGridCols];
		Vector3 worldBottomLeft = transform.position + Vector3.left * gridWorldSize.x * 0.5f + Vector3.down * gridWorldSize.y * 0.5f;

		for (int row = 0; row < numGridRows; row++) {
			for (int col = 0; col < numGridCols; col++) {
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (row * nodeDiameter + nodeRadius) + Vector3.up * (col * nodeDiameter + nodeRadius);
				bool walkable = Physics2D.BoxCast (worldPoint, Vector2.one * nodeDiameter, 0.0f, Vector2.zero, 0.0f, unwalkableMask).collider == null;

				int movementPenalty = 0;
				float rayHeight = 5.0f;
				float rayRange = 10.0f;

				// apply grid weights
				Ray ray = new Ray (worldPoint + Vector3.forward * rayHeight, Vector3.back);
				RaycastHit2D hit = Physics2D.GetRayIntersection (ray, rayRange, walkableMask);
				if (hit)
					walkableRegionsDict.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
				if (!walkable)
					movementPenalty += obstacleProximityPenalty;

				grid [row, col] = new GridNode (worldPoint, walkable, row, col, movementPenalty);
			}
		}
		BlurPenaltyMap (pathingBlurSize);
	}

	void BlurPenaltyMap(int blurSize) {
		int kernelSize = blurSize * 2 + 1;
		int kernelExtents = (kernelSize - 1) / 2;
		int[,] penaltiesHorizontalPass = new int[numGridRows, numGridCols];
		int[,] penaltiesVerticalPass = new int[numGridRows, numGridCols];

		// horizontal pass of kernel over grid
		for (int c = 0; c < numGridCols; c++) {
			for (int r = -kernelExtents; r <= kernelExtents; r++) {
				int sampleRow = Mathf.Clamp (r, 0, kernelExtents);
				penaltiesHorizontalPass [0, c] += grid [sampleRow, c].movementPenalty;
			}

			for (int r = 1; r < numGridRows; r++) {
				int removeIndex = Mathf.Clamp (r - kernelExtents, 0, numGridRows);
				int addIndex = Mathf.Clamp (r + kernelExtents, 0, numGridRows - 1);
				penaltiesHorizontalPass [r, c] = penaltiesHorizontalPass [r - 1, c] - grid [removeIndex, c].movementPenalty + grid [addIndex, c].movementPenalty;
			}
		}

		// vertical pass of kernel over grid
		for (int r = 0; r < numGridRows; r++) {
			for (int c = -kernelExtents; c <= kernelExtents; c++) {
				int sampleCol = Mathf.Clamp (c, 0, kernelExtents);
				penaltiesVerticalPass [r, 0] += penaltiesHorizontalPass [r, sampleCol];
			}

			int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[r,0] / (kernelSize * kernelSize));
			grid [r, 0].movementPenalty = blurredPenalty;

			for (int c = 1; c < numGridCols; c++) {
				int removeIndex = Mathf.Clamp (c - kernelExtents, 0, numGridCols);
				int addIndex = Mathf.Clamp (c + kernelExtents, 0, numGridCols - 1);

				penaltiesVerticalPass [r, c] = penaltiesVerticalPass [r, c - 1] - penaltiesHorizontalPass[r, removeIndex] + penaltiesHorizontalPass [r, addIndex];
				blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[r,c] / (kernelSize * kernelSize));
				grid [r, c].movementPenalty = blurredPenalty;

				// gizmo debug visualization
				if (blurredPenalty > penaltyMax)
					penaltyMax = blurredPenalty;
				if (blurredPenalty < penaltyMin)
					penaltyMin = blurredPenalty;
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

				Gizmos.color = Color.Lerp (Color.white, Color.black, Mathf.InverseLerp (penaltyMin, penaltyMax, n.movementPenalty));
				Gizmos.color = (n.walkable) ? Gizmos.color : Color.red;
				Gizmos.DrawCube (n.worldPosition, Vector3.one * nodeDiameter);
			}
		}
	}

	[Serializable]
	public class TerrainType {
		public LayerMask terrainMask;
		public int terrainPenalty;
	}
}
