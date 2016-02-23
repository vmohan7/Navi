using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIManager : MonoBehaviour {

	public static UIManager Instance;

	public Animator transitionController; //set in the inspector

	public const string SearchTranstion = "SearchSlideOut";
	public const string AppTranstion = "AppsSlideOut";

	private int state = 0;

	void Awake() {
		if (Instance == null)
			Instance = this;
	}

	void OnDestroy() {
		if (Instance == this)
			Instance = null;
	}
	
	public void LoadSearching() {
		state = 0;
		transitionController.SetBool (SearchTranstion, false);
		transitionController.SetBool (AppTranstion, false);
	}

	public void LoadConnectTo() {
		state = 1;
		transitionController.SetBool (SearchTranstion, true);
		transitionController.SetBool (AppTranstion, false);
	}

	public void LoadGameScene() {
		state = 2;
		transitionController.SetBool (SearchTranstion, true);
		transitionController.SetBool (AppTranstion, true);
	}

	public int GetState() {
		return state;
	}

}
