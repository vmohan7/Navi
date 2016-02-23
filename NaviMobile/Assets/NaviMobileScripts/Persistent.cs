using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Persistent : MonoBehaviour {

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad (this.gameObject);
		SceneManager.LoadScene (1); //Load UI
	}

}
