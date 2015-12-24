using UnityEngine;
using System.Collections;

public class Persistent : MonoBehaviour {

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad (this.gameObject);
	}

}
