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

	private Quaternion initalRotation; //rotation of last reset

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
		//transform.Rotate (-Input.gyro.rotationRateUnbiased.x, -Input.gyro.rotationRateUnbiased.y, Input.gyro.rotationRateUnbiased.z);

		Quaternion tempRot = Quaternion.identity;
		ComputeRotation (out tempRot);
		transform.rotation = Quaternion.Inverse (initalRotation) * tempRot;
	}

	/// <summary>
	///  Computes the rotation from the gyro sensor data
	/// </summary>
	/// <param name="rot">The output rotation from this calculation</param> 
	private void ComputeRotation(out Quaternion rot){
		var att = Input.gyro.attitude;
		att = new Quaternion(att.x, att.y, -att.z, -att.w);
		rot = Quaternion.Euler(90, 0, 0) * att;
	}

	/// <summary>
	///  How to reset the position of the device
	/// </summary>
	public override void Reset(){
		ComputeRotation (out initalRotation);
	}
}