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
///  This class is responsible for taking the unreliable network 
///  pose data and interpolating. The interpolated data can be accessed 
///  via the DeviceLocation static variable, which is the transform of
///  the game object this script is attached to. Supports only one device.
/// </summary>
public class NaviDeviceLocation : MonoBehaviour {

	//The handle to check the transform of the device.
	public static Transform DeviceLocation;

	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;
	private Quaternion syncStartRotation = new Quaternion ();
	private Quaternion syncEndRotation = new Quaternion ();

	/// <summary>
	/// First function that is called when scene is loading
	/// </summary>
	void Awake(){
		if (DeviceLocation == null)
			DeviceLocation = gameObject.transform;
	}

	/// <summary>
	/// Called after Gameobject is initalized. We start listening for pose data
	/// </summary>
	void Start () {
		NaviConnectionSDK.OnPoseData += OnPoseData;
		DontDestroyOnLoad (this.gameObject);
	}

	/// <summary>
	/// Called when object is destroyed i.e. when game ends
	/// </summary>
	void OnDestroy(){
		DeviceLocation = null;
		NaviConnectionSDK.OnPoseData -= OnPoseData;
	}

	//TODO: add some sort of prediction using velocity
	/// <summary>
	/// Calculates the difference in time between sycnhronizations based on pose data sent from the network
	/// </summary>
	private void OnPoseData(Vector3 syncPosition, Quaternion syncRotation){
		syncTime = 0f;
		syncDelay = Time.time - lastSynchronizationTime;
		lastSynchronizationTime = Time.time;
		
		syncStartPosition = transform.position;
		syncEndPosition = syncPosition;
		
		syncStartRotation = transform.rotation;
		syncEndRotation = syncRotation;

	}

	/// <summary>
	/// Called every frame and interpolates the current pose to the synchronized pose based on time.
	/// </summary>
	void Update() {
		syncTime += Time.deltaTime;
		transform.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
		transform.rotation = Quaternion.Slerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
	}
}