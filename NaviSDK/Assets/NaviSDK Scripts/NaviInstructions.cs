/*
 * This file is part of Navi.
 * Copyright 2015 Vasanth Mohan. All Rights Reserved.
 * 
 * Navi is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Navi is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Navi.  If not, see <http://www.gnu.org/licenses/>.
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
///  This script controls showing the user instructions on how to use the device
/// </summary>
public class NaviInstructions : MonoBehaviour {

	//the device prefab that we use to visually show the user how to control their device
	public GameObject devicePrefab;
	//the text output
	public Text instructionPanel;

	//all the instructions
	private const string searchInstructions = "Searching for Navi app running on Android device in same Local Network...";
	private const string acknowledgeConnection = "Hold you device horizontally. Tap to continue when you are ready.";
	private const string tiltUpDownInstruction = "Tilt your device up and down to control the y-direction of your virtual device.";
	private const string tiltRightLeftInstruction = "Tilt your device left and right to control the x-direction of your virtual device.";
	private const string resetInstructions = "Face forward with your tablet facing your chest and tap with your right 5 fingers to reset orientation and begin!";

	/// <summary>
	/// Waits for smart device to connect and listens for events
	/// </summary>
	void Start() {
		SetInstruction (searchInstructions);
		NaviConnectionSDK.OnDeviceConnected += OnDeviceConnect;
		NaviConnectionSDK.OnDeviceDisconnected += OnDeviceDisconnect;
	}

	/// <summary>
	/// Removes events when this game object is destroyed i.e. when the game starts
	/// </summary>
	void OnDestroy(){
		NaviConnectionSDK.OnDeviceConnected -= OnDeviceConnect;
		NaviConnectionSDK.OnDeviceDisconnected -= OnDeviceDisconnect;
	}

	/// <summary>
	/// Callback for when Device connects, so we start showing instructions
	/// </summary>
	private void OnDeviceConnect(){
		StartCoroutine (InstructionGuide ());
	}

	/// <summary>
	/// Callback for when Device disconnects, so we can restart instructions
	/// </summary>
	private void OnDeviceDisconnect(){
		StopAllCoroutines ();
		SetInstruction (searchInstructions);
	}

	private const float TurnMargin = 160f; //margin required move to next instruction
	/// <summary>
	/// Coroutine to guide the user through the instructions on how to use their smart device
	/// </summary>
	IEnumerator InstructionGuide(){
		SetInstruction (acknowledgeConnection);
		TouchManager.OnTouchUp += OnPermissionTap;
		while (!permissionInstruction) {
			yield return new WaitForFixedUpdate();
		}
		NaviConnectionSDK.Instance.ResetVR ();
		TouchManager.OnTouchUp -= OnPermissionTap;
		
		Instantiate (devicePrefab, Camera.main.transform.position, new Quaternion ());
		NaviConnectionSDK.Instance.ResetDevice ();
		SetInstruction (tiltUpDownInstruction);
		
		yield return new WaitForSeconds(1f); //wait for reset
		while (Mathf.Abs(NaviDeviceLocation.DeviceLocation.rotation.eulerAngles.x - 180f) > TurnMargin) {
			yield return new WaitForFixedUpdate(); //wait until they move up and down
		}
		
		SetInstruction (tiltRightLeftInstruction);
		while (Mathf.Abs(NaviDeviceLocation.DeviceLocation.rotation.eulerAngles.y - 180f) > TurnMargin) {
			yield return new WaitForFixedUpdate(); //wait until they move up and down
		}
		
		SetInstruction (resetInstructions);
	}
	
	private bool permissionInstruction = false;
	/// <summary>
	/// Wait for user to verify that the smart device is ready
	/// </summary>
	private void OnPermissionTap(int fingerID, Vector2 pos){
		permissionInstruction = true;
	}
	
	private bool toggleTapInstruction = false;
	/// <summary>
	/// Waits for smart device to recieve a double tap
	/// </summary>
	private void OnToggleTap(int fingerID, Vector2 pos){
		toggleTapInstruction = true;
	}

	/// <summary>
	/// Displays instructions on text GUI
	/// </summary>
	private void SetInstruction(string instruction){
		if (instructionPanel != null)
			instructionPanel.text = instruction;
	}
}