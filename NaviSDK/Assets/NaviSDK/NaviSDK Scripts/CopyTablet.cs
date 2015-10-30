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
using System.Collections;

/// <summary>
///  This script can be attached to any game object and will copy the smart devices rotation and position
///  Based on the user preference, this can copy just rotation or both rotation and position
/// </summary>
public class CopyTablet : MonoBehaviour {
	
	public GameObject moveableObject;
	private Vector3 initalPos;
	private float initalRadius;

	private bool justRotation = true;

	/// <summary>
	/// First function that is called when scene is loading
	/// </summary>
	void Awake() {
		initalPos = this.transform.position;
		initalRadius = moveableObject.transform.localPosition.magnitude;
	}

	/// <summary>
	/// Set up callbacks
	/// </summary>
	void Start(){
		TouchManager.OnDoubleTap += OnSwitchModes;
	}

	/// <summary>
	/// Remove callback when game object is destroyed
	/// </summary>
	void OnDestroy(){
		TouchManager.OnDoubleTap -= OnSwitchModes;
	}
	
	/// <summary>
	/// Updates the game objects transform based on the device location and the current copy setting (rotation or both rotation and position)
	/// </summary>
	void Update () {
		if (NaviConnectionSDK.Instance.GetPlayerPose(0) != null) {
			NaviDevice player0 = NaviConnectionSDK.Instance.GetPlayerPose(0);
			if (justRotation) {
				transform.rotation = player0.transform.rotation;
			} else {
				//TODO figure out how to scale the position and make it rotate around you based on some assumptions of reset
				transform.position = initalRadius*player0.transform.position + initalPos;
				moveableObject.transform.rotation = player0.transform.rotation;
			}

		}
	}

	/// <summary>
	/// Callback function for double tap, which will switch between copy modes
	/// </summary>
	private void OnSwitchModes(int playerID, int fingerId, Vector2 pos){
		if (playerID == 0) {
			justRotation = !justRotation; //toggles mode
			moveableObject.transform.rotation = new Quaternion (); //reset rotation of device
			transform.position = initalPos; //reset position
		}
	}
}