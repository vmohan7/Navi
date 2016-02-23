using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent (typeof(RectTransform)) ]
public class IPConnection : MonoBehaviour {

	private string ip;

	public void SetIP(string name, string ip){
		this.ip = ip.Trim ();
		GetComponentInChildren<Text> ().text = name.Trim();
	}

	//called in the button inspector
	public void Connect() {
		if (NaviMobileManager.Instance.Connect (this.ip))
			UIManager.Instance.LoadGameScene ();
		//TODO: else case to warn user that they cannot connect
	}
}
