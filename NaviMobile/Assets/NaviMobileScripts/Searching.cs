using UnityEngine;
using System.Collections;

public class Searching : MonoBehaviour {


	// Update is called once per frame
	void Update () {
		if (NaviMobileManager.Instance.possibleConnections.Count > 0 && UIManager.Instance.GetState() == 0 ) {
			UIManager.Instance.LoadConnectTo ();
		}
	}
}
