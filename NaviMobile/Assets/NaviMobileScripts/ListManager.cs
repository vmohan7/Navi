using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ListManager : MonoBehaviour {

	public const float REFRESH_RATE = 1f;

	public const float SPACING = .4f;

	public RectTransform listParent;
	public RectTransform listItemPrefab;

	private List<RectTransform> currentItems = new List<RectTransform>();

	void Start() {
		StartCoroutine (Refresh ());
	}

	void OnDestroy() {
		StopAllCoroutines ();
	}

	// Update is called once per frame
	IEnumerator Refresh () {

		//TODO: add an auto connect if there is only one after the second referesh

		//TODO: put in an auto connect if it has not happened in say one iteration
		while (true) {
			if (currentItems.Count != NaviMobileManager.Instance.possibleConnections.Count) {
				foreach (RectTransform rt in currentItems) {
					Destroy (rt.gameObject);
				}
				currentItems.Clear ();

				CreateList ();
			} else if (currentItems.Count == 1) {
				currentItems [0].gameObject.GetComponent<IPConnection> ().Connect ();
			}
				
			yield return new WaitForSeconds (REFRESH_RATE);
		}
	}

	//TODO: add scrolling if we think the list is getting too long
	private void CreateList() {
		if (NaviMobileManager.Instance.possibleConnections.Count == 0 && UIManager.Instance.GetState() == 1 ) {
			UIManager.Instance.LoadSearching ();
			return;
		}

		for( int i = 0 ; i < NaviMobileManager.Instance.possibleConnections.Count; i++ ) {
			GameObject obj = Instantiate(listItemPrefab.gameObject) as GameObject;
			RectTransform rt = obj.GetComponent<RectTransform> ();

			obj.transform.SetParent( listParent );
			obj.transform.localPosition = new Vector3(0f, -i*((1f + SPACING)*rt.rect.height), 0f);
			obj.transform.localScale = Vector3.one;

			string[] values = NaviMobileManager.Instance.SplitIP_Message (NaviMobileManager.Instance.possibleConnections [i]);
			string name;
			string ip;

			if (values.Length == 1) {
				ip = values [0];
				name = "Connect to " + ip;
			} else {
				name = values[0];
				ip = values [1];
			}

			obj.GetComponent<IPConnection> ().SetIP ( name , ip );
			currentItems.Add(obj.GetComponent<RectTransform>());
		}
	}
}
