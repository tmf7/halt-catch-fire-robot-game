using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathRequestManager : MonoBehaviour {

	private Dictionary<string, PathRequest> pathRequestDict = new Dictionary<string, PathRequest>();
	private PathRequest 		currentPathRequest;
	private PathFinding 		pathfinding;
	private bool 				processingPath;

	static PathRequestManager 	instance = null;

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	

		pathfinding = GetComponent<PathFinding> ();
	}

	public static void RequestPath(string owner, Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool, int> callback, int subPathIndex = -1) {
		instance.pathRequestDict [owner + subPathIndex.ToString()] = new PathRequest (owner, pathStart, pathEnd, callback, subPathIndex);
		if (subPathIndex > -1)
			instance.RemoveDeadSubPaths (owner, subPathIndex);
		
		instance.TryProcessNext ();
	}

	void RemoveDeadSubPaths(string owner, int subPathIndex) {
		// Don't Remove what's already been overwritten
		int index = subPathIndex + 1;
		string tryKey = owner + index.ToString ();
		while (pathRequestDict.ContainsKey (tryKey)) {
			pathRequestDict.Remove (tryKey);
			index++;
			tryKey = owner + index.ToString ();
		}
	}

	void TryProcessNext() {
		if (!processingPath && pathRequestDict.Count > 0) {
			var dictEnumerator = pathRequestDict.GetEnumerator();
			dictEnumerator.MoveNext ();
			currentPathRequest = dictEnumerator.Current.Value;
			processingPath = true;
			pathfinding.StartFindPath (currentPathRequest.pathStart, currentPathRequest.pathEnd, currentPathRequest.subPathIndex);
		}
	}

	public void FinishedProcessingPath(Vector3[] path, bool success, int subPathIndex) {
		instance.pathRequestDict.Remove(currentPathRequest.owner + subPathIndex.ToString());
		currentPathRequest.callback (path, success, subPathIndex);
		processingPath = false;
		TryProcessNext ();
	}

	struct PathRequest {
		public Vector3 pathStart;
		public Vector3 pathEnd;
		public Action<Vector3[], bool, int> callback;
		public int subPathIndex;
		public string owner;

		public PathRequest(string _owner, Vector3 _start, Vector3 _end, Action<Vector3[], bool, int> _callback, int _subPathIndex) {
			owner = _owner;
			pathStart = _start;
			pathEnd = _end;
			callback = _callback;
			subPathIndex = _subPathIndex;
		}
	}
}
