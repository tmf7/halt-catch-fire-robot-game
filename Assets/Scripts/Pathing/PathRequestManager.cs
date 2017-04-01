using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathRequestManager : MonoBehaviour {

	private Queue<PathRequest> 	pathRequestQueue = new Queue<PathRequest>();
	private PathRequest 		currentPathRequest;
	private PathFinding 		pathfinding;
	private bool 				processingPath;

	static PathRequestManager 	instance;

	void Awake() {
		// FIXME: re-enable this to enforce the singleton
//		if (instance != this) {
//			Destroy (this);
//			return;
//		}
		instance = this;
		pathfinding = GetComponent<PathFinding> ();
	}

	public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback) {
		PathRequest newRequest = new PathRequest (pathStart, pathEnd, callback);
		instance.pathRequestQueue.Enqueue (newRequest);
		instance.TryProcessNext ();
	}

	void TryProcessNext() {
		if (!processingPath && pathRequestQueue.Count > 0) {
			currentPathRequest = pathRequestQueue.Dequeue ();
			processingPath = true;
			pathfinding.StartFindPath (currentPathRequest.pathStart, currentPathRequest.pathEnd);
		}
	}

	public void FinishedProcessingPath(Vector3[] path, bool success) {
		currentPathRequest.callback (path, success);
		processingPath = false;
		TryProcessNext ();
	}

	struct PathRequest {
		public Vector3 pathStart;
		public Vector3 pathEnd;
		public Action<Vector3[], bool> callback;

		public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback) {
			pathStart = _start;
			pathEnd = _end;
			callback = _callback;			
		}

	}
}
