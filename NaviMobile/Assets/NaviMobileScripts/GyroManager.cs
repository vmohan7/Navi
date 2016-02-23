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
///  This class extends TransformManagerInterface and is responsbile for informing the system of the current pose of the
///  device. In this case, it uses the gyro to set rotational data. 
/// </summary>
public class GyroManager : TransformManagerInterface {
	
	private const float ScreenRotationThreshold = 5f; //Accelerometer threshold for switching orientation on reset
	private ScreenOrientation currOrient;

	/// <summary>
	///  Gets gyro data and uses it to update rotation relative to reset position
	/// </summary>
	void Update () {
		transform.rotation =  ComputeRotation();
	}

	/// <summary>
	///  How to reset the position of the device
	/// </summary>
	public override void Reset(){
		ResetOrientation ();
		Cardboard.SDK.Recenter ();
	}

	private Quaternion ComputeRotation() {
		Quaternion temp = Cardboard.SDK.transform.GetChild (0).rotation;

		if (currOrient == ScreenOrientation.Portrait) {
			return Quaternion.Euler (0f, -270f, 0f) * temp * Quaternion.Euler (0f, 0f, -270f);
		} else if (currOrient == ScreenOrientation.PortraitUpsideDown) {
			return Quaternion.Euler (0f, -90f, 0f) * temp * Quaternion.Euler (0f, 0f, -90f);
		} else if (currOrient == ScreenOrientation.LandscapeRight) {
			return Quaternion.Euler (0f, -180f, 0f) * temp * Quaternion.Euler (0f, 0f, -180f);
		} else {
			return temp;
		}

		/*
			if (Screen.orientation == ScreenOrientation.Portrait) {
				return Quaternion.Euler (0f, -270f, 0f) * temp * Quaternion.Euler (0f, 0f, -270f);
			} else if (Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
				return Quaternion.Euler (0f, -90f, 0f) * temp * Quaternion.Euler (0f, 0f, -90f);
			} else if (Screen.orientation == ScreenOrientation.LandscapeRight) {
				return Quaternion.Euler (0f, -180f, 0f) * temp * Quaternion.Euler (0f, 0f, -180f);
			} else {
				return temp;
			}
			*/
	}

	/// <summary>
	///  Method to approraitiatly rotate the device and screen orientation
	/// </summary>
	private void ResetOrientation() {
		Vector3 cardboardRot = Cardboard.SDK.transform.GetChild (0).rotation.eulerAngles;
		if ( (cardboardRot.z + ScreenRotationThreshold > 360f) || (0f > cardboardRot.z - ScreenRotationThreshold) ) {
			currOrient = ScreenOrientation.LandscapeLeft;
		} else if ( (cardboardRot.z - ScreenRotationThreshold < 270f) && (270f < cardboardRot.z + ScreenRotationThreshold) ) {
			currOrient = ScreenOrientation.Portrait;
		} else if ( (cardboardRot.z - ScreenRotationThreshold < 90f) && (90f < cardboardRot.z + ScreenRotationThreshold) ) {
			currOrient = ScreenOrientation.PortraitUpsideDown;
		} else if ( (cardboardRot.z - ScreenRotationThreshold < 180f) && (180f < cardboardRot.z + ScreenRotationThreshold) ) {
			currOrient = ScreenOrientation.LandscapeRight;
		}

		if (NaviMobileManager.Instance.canUserResetOrientation)
			Screen.orientation = currOrient;

		NaviMobileManager.Instance.SendCurrentSize ();
	}
}