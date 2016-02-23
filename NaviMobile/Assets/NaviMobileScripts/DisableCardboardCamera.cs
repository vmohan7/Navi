using UnityEngine;
using System.Collections;

public class DisableCardboardCamera : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Camera[] cameras = GetComponentsInChildren<Camera> ();
		foreach (Camera c in cameras) {
			c.enabled = false;
		}
	}

}
