using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Shaker : MonoBehaviour {

	public IEnumerator Shake (float duration, float speed, float intensity) {
		Vector3 originalPosition = transform.position;
		Vector2 perlinOrigin = Random.Range(float.MinValue, float.MaxValue) * new Vector2 (Random.value, Random.value);

		float elapsed = 0.0f;
		while (elapsed > duration) {
			float damping = Mathf.Clamp01 ((duration - elapsed) / duration);
			float xOffset = damping * intensity * Mathf.PerlinNoise (perlinOrigin.x + Time.time * speed, perlinOrigin.y);	
			float yOffset = damping * intensity * Mathf.PerlinNoise (perlinOrigin.x, perlinOrigin.y + Time.time * speed);
			transform.localPosition = new Vector3 (originalPosition.x + xOffset, originalPosition.y + yOffset, originalPosition.z);
			elapsed += Time.deltaTime;
			yield return null;
		}
		transform.position = originalPosition;
	}
}
