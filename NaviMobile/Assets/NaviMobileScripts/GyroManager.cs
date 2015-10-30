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
	
	private const float ScreenRotationThreshold = 0.7f; //Accelerometer threshold for switching orientation on reset

	private bool isUpsideDown = false; //Boolean to detech if we are using upside down portrait, which requires a slightly different use of gyrp
	private bool isReseting = false; //avoid multiple reset commands interfering with each other
	private Quaternion initalRotation; //rotation of last reset

	private Quaternion sensorFused = Quaternion.identity;
	private const float SamplePeriod = 1f / 256f; //Might want to make this 1/60 fps
	private const float Beta =  0.1f;

	/// <summary>
	///  Method to initalize the gyro at start of game
	/// </summary>
	void Start () {
		// Activate the gyroscope
		Input.gyro.enabled = true;
		initalRotation = Quaternion.identity;

	}
	
	/// <summary>
	///  Gets gyro data and uses it to update rotation relative to reset position
	/// </summary>
	void Update () {
		if (!isReseting) { //only change orientation if we are not reseting
			Quaternion tempRot = Quaternion.identity;
			ComputeRotation (out tempRot);
			transform.rotation = Quaternion.Inverse (initalRotation) * tempRot;
		}
	}

	/// <summary>
	///  Computes the rotation from the gyro sensor data
	/// </summary>
	/// <param name="rot">The output rotation from this calculation</param> 
	private void ComputeRotation(out Quaternion rot){
		Quaternion att = Input.gyro.attitude;
		if (isUpsideDown) {
			att = new Quaternion(att.x, att.y, att.z, att.w);
		} else {
			att = new Quaternion(att.x, att.y, -att.z, -att.w);
		}

		rot = Quaternion.Euler(90f, 0f, 0f) * att;
	}

	/// <summary>
	///  How to reset the position of the device
	/// </summary>
	public override void Reset(){
		StartCoroutine (ResetOrientation ()); //start a courtine to apply reset settings
	}

	/// <summary>
	///  Couroutine to apply approraite wait times for calibrating device and screen orientation
	/// </summary>
	IEnumerator ResetOrientation() {
		if (!isReseting) { //only reset if no other coroutine is reseting
			isReseting = true;
			Input.compensateSensors = false; //do not compensate for orientation, so that we get raw reading from accelerometer
			yield return new WaitForSeconds(.05f); //wait a few frames for a fresh reading
			isUpsideDown = false;
			if (Input.acceleration.x < -ScreenRotationThreshold) {
				Screen.orientation = ScreenOrientation.LandscapeLeft;
			} else if (Input.acceleration.y < -ScreenRotationThreshold) {
				Screen.orientation = ScreenOrientation.Portrait;
			} else if (Input.acceleration.y > ScreenRotationThreshold) {
				Screen.orientation = ScreenOrientation.PortraitUpsideDown;
				isUpsideDown = true;
			} else if (Input.acceleration.x > ScreenRotationThreshold) {
				Screen.orientation = ScreenOrientation.LandscapeRight;
			}
			yield return new WaitForSeconds(.1f); //wait for device to switch orientations
			Input.compensateSensors = true; //recompensate for orientation for the gyro orientation
			yield return new WaitForSeconds(.05f); //wait a few frames for a fresh reading

			ComputeRotation (out initalRotation); //used to reset inital position to be right in front of the camera  
			isReseting = false; //done reseting
		}
	}
}