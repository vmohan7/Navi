using UnityEngine;
using System.Collections;

public class SimpleCopyTablet : MonoBehaviour {
	
	// Update is called once per frame
	void Update () {
		transform.rotation = NaviDeviceLocation.DeviceLocation.transform.rotation;
	}
}
