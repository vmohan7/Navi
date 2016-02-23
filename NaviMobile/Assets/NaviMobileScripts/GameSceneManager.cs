using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameSceneManager : MonoBehaviour {

	public Text instructions;

	// Update is called once per frame
	void Update () {
		instructions.text = NaviMobileManager.Instance.displayMessage;
	}
}
