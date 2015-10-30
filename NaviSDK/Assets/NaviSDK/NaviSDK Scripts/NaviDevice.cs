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
public class NaviDevice : MonoBehaviour {

	//variables used to reset player id for the device
	[HideInInspector]
	public float playerResetTimer = 0f;
	[HideInInspector]
	public int numFingersDown = 0;
	[HideInInspector]
	public bool playerSwitchEnabled = false;

	//ID is set when prefab is created
	[HideInInspector]
	public int connectionID;
	
	[HideInInspector]
	public int DeviceWidth;
	[HideInInspector]
	public int DeviceHeight; 

	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;
	private Quaternion syncStartRotation = new Quaternion ();
	private Quaternion syncEndRotation = new Quaternion ();

	[HideInInspector]
	public Vector3 interpolatedAcceleration = Vector3.zero;
	private Vector3 syncStartAccel = Vector3.zero;
	private Vector3 syncEndAccel = Vector3.zero;

	/// <summary>
	/// Called after Gameobject is initalized. We start listening for pose data
	/// </summary>
	void Start () {
		NaviConnectionSDK.OnPoseData += OnPoseData;
		NaviConnectionSDK.OnAccelerationData += OnAccelerationData;
		DontDestroyOnLoad (this.gameObject);
	}

	/// <summary>
	/// Called when object is destroyed i.e. when game ends
	/// </summary>
	void OnDestroy(){
		NaviConnectionSDK.OnPoseData -= OnPoseData;
		NaviConnectionSDK.OnAccelerationData -= OnAccelerationData;
	}
	
	/// <summary>
	/// Calculates the difference in time between sycnhronizations based on acceleration data sent from the network
	/// </summary>
	private void OnAccelerationData(int connectionID, Vector3 syncAccelData){
		if (this.connectionID == connectionID) {
			syncStartAccel = interpolatedAcceleration;
			syncEndAccel = syncAccelData;
		}
	}

	//TODO: add some sort of prediction using velocity
	/// <summary>
	/// Calculates the difference in time between sycnhronizations based on pose data sent from the network
	/// </summary>
	private void OnPoseData(int connectionID, Vector3 syncPosition, Quaternion syncRotation){
		if (this.connectionID == connectionID) {
			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;
		
			syncStartPosition = transform.position;
			syncEndPosition = syncPosition;
		
			syncStartRotation = transform.rotation;
			syncEndRotation = syncRotation;
		}

	}

	/// <summary>
	/// Called every frame and interpolates the current pose to the synchronized pose based on time.
	/// </summary>
	void Update() {
		syncTime += Time.deltaTime;
		interpolatedAcceleration = Vector3.Lerp(syncStartAccel, syncEndAccel, syncTime / syncDelay);
		transform.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
		transform.rotation = Quaternion.Slerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
	}

	/// <summary>
	/// Stores the smart device's width and height to be accessed by the game
	/// </summary>
	/// <param name="width">The width of the smart device</param>
	/// <param name="height">The height of the smart device</param>
	public void SetServerScreenSize(int width, int height){
		DeviceWidth = width;
		DeviceHeight = height;
	}
}