using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathRequestManager : MonoBehaviour {

	private Queue<PathRequest> 	pathRequestQueue = new Queue<PathRequest>();
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

	public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool, bool> callback, bool isSubPath = false) {
//		if (instance.pathRequestQueue.Count > GameManager.instance.maxRobots)
//			instance.pathRequestQueue.Clear ();
		
		PathRequest newRequest = new PathRequest (pathStart, pathEnd, callback, isSubPath);
		instance.pathRequestQueue.Enqueue (newRequest);
		instance.TryProcessNext ();
	}

	void TryProcessNext() {
		if (!processingPath && pathRequestQueue.Count > 0) {
			currentPathRequest = pathRequestQueue.Dequeue ();
			processingPath = true;
			pathfinding.StartFindPath (currentPathRequest.pathStart, currentPathRequest.pathEnd, currentPathRequest.isSubPath);
		}
	}

	public void FinishedProcessingPath(Vector3[] path, bool success, bool isSubPath) {
		currentPathRequest.callback (path, success, isSubPath);
		processingPath = false;
		TryProcessNext ();
	}

	struct PathRequest {
		public Vector3 pathStart;
		public Vector3 pathEnd;
		public Action<Vector3[], bool, bool> callback;
		public bool isSubPath;

		public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool, bool> _callback, bool _isSubPath) {
			pathStart = _start;
			pathEnd = _end;
			callback = _callback;
			isSubPath = _isSubPath;
		}
	}
}
