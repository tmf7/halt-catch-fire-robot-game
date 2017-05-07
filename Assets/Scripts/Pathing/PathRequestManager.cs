using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathRequestManager : MonoBehaviour {

	private Dictionary<string, KeyValuePair<int, int>>	pathRequestOwners = new Dictionary<string, KeyValuePair<int, int>> ();		// KVP for maxPathRequests and pathRequestsRemaining

	private Dictionary<string, PathRequest> 	pathRequestDict = new Dictionary<string, PathRequest>();
	private PathRequest 						currentPathRequest;
	private PathFinding 						pathfinding;
	private string 								currentOwner;
	private bool 								processingPath;

	static PathRequestManager 					instance = null;
	public static int 							AUTO_PATH = -1;

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	

		pathfinding = GetComponent<PathFinding> ();
	}

	public static void RequestPath(string owner, Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool, int> callback, int subPathIndex = -1, bool finalPathRequest = true) {
		if (subPathIndex <= 0)
			KillPathRequests (owner);
		
		if (finalPathRequest)
			instance.RegisterPathOwner (owner, subPathIndex);

		int index = subPathIndex;
		if (subPathIndex == AUTO_PATH)
			index = 0;
		
		instance.pathRequestDict [owner + index.ToString()] = new PathRequest (owner, pathStart, pathEnd, callback, subPathIndex);
		instance.TryProcessNext ();
	}

	public static void KillPathRequests (string owner, int startIndex = 0) {
//		print ("(" + owner + ") PATHS REQ. ABOVE (" + startIndex + ") KILLED");
		int maxRemovals = instance.PathRequestsSubmitted (owner);
		for (int index = startIndex; index < maxRemovals; index++) {
			string tryKey = owner + index.ToString ();
			instance.pathRequestDict.Remove (tryKey);
			instance.DecrementRequestsRemaining (owner);					// FIXME: this affects the registered path count
		}
	}

	public static int PathRequestsRemaining (string owner) {
		KeyValuePair<int, int> requestRatio;
		if (instance.pathRequestOwners.TryGetValue (owner, out requestRatio))
			return requestRatio.Value;
		else
			return -1;
	}

	int PathRequestsSubmitted (string owner) {
		KeyValuePair<int, int> requestRatio;
		if (pathRequestOwners.TryGetValue (owner, out requestRatio))
			return requestRatio.Key;
		else
			return -1;
	}

	void DecrementRequestsRemaining (string owner) {
		KeyValuePair<int, int> requestRatio;
		if (!pathRequestOwners.TryGetValue (owner, out requestRatio))
			return;
		pathRequestOwners [owner] = new KeyValuePair<int, int> (requestRatio.Key, requestRatio.Value - 1);	
	}

	void RegisterPathOwner (string owner, int numPathRequests) {
		if (numPathRequests <= 0)
			numPathRequests = 1;
		else
			numPathRequests++;
		pathRequestOwners [owner] = new KeyValuePair<int, int> (numPathRequests, numPathRequests);	
	}

	bool GetNextOwner() {
		currentOwner = null;
		foreach (KeyValuePair<string, KeyValuePair<int, int>> owner in pathRequestOwners) {
			if (PathRequestsRemaining (owner.Key) > 0) {
				currentOwner = owner.Key;
				break;
			}
		}
		return currentOwner != null;
	}

	void TryProcessNext() {
		if (!processingPath && pathRequestDict.Count > 0 && GetNextOwner()) {

			int index = PathRequestsSubmitted(currentOwner) - PathRequestsRemaining (currentOwner);
			string pathRequestKey = currentOwner + index.ToString ();
			if (!pathRequestDict.TryGetValue (pathRequestKey, out currentPathRequest)) {
				processingPath = false;
				return;
			}

			processingPath = true;
			DecrementRequestsRemaining (currentOwner);
			pathfinding.StartFindPath (currentPathRequest.pathStart, currentPathRequest.pathEnd, currentPathRequest.subPathIndex);
		}
	}

	public void FinishedProcessingPath(Vector3[] path, bool success, int subPathIndex) {
		instance.pathRequestDict.Remove(currentPathRequest.owner + subPathIndex.ToString());
		if (currentPathRequest.callback.Target != null)
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
