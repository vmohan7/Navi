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
using Tango;
using System;

/// <summary>
///  This class extends TransformManagerInterface and is responsbile for informing the system of the current pose of the
///  device. In this case, it uses the TangoSDK to set rotational and positional data. 
/// </summary>
public class PoseManager : TransformManagerInterface , ITangoPose {

	private TangoApplication m_tangoApplication; // Instance for Tango Client
	private Vector3 m_tangoPosition; // Position from Pose Callback
	private Quaternion m_tangoRotation; // Rotation from Pose Callback

	[HideInInspector]
	public float m_metersToWorldUnitsScaler = 1.0f;
	
	private Vector3 m_zeroPosition; //the physical position of the device when reset
	private Quaternion m_zeroRotation; //the physical rotation of the device when reset
	private Vector3 m_startPosition; // the position the device starts at (0,0,0)
	private Quaternion m_startRotation; //the rotation the device starts at (0,0,0,0)
	
	private float m_movementScale = 10.0f; //the translation between physical and virtual movement

	private TangoPoseData prevPose = new TangoPoseData(); //past pose; used for interpolation
	private TangoPoseData currPose = new TangoPoseData(); //current pose
	private float unityTimestampOffset = 0;

	/// <summary>
	///  Initalize variables
	/// </summary>
	void Awake(){
		base.Awake ();

		m_startPosition = transform.position;
		m_startRotation = transform.rotation;
		ComputeTransformUsingPose (out m_zeroPosition, out m_zeroRotation, currPose);
	}

	/// <summary>
	///  Set more variables once scene has loaded and all scripts are ready
	/// </summary>
	void Start ()
	{
		Application.targetFrameRate = 60;

		// Initialize some variables
		m_tangoRotation = Quaternion.identity;
		m_tangoPosition = Vector3.zero;
		m_startPosition = transform.position;
		m_tangoApplication = FindObjectOfType<TangoApplication>();
		if(m_tangoApplication != null)
		{
			// Request Tango permissions
			m_tangoApplication.RegisterPermissionsCallback(PermissionsCallback);
			m_tangoApplication.RequestNecessaryPermissionsAndConnect();
			m_tangoApplication.Register(this);
		}
		else
		{
			Debug.Log("No Tango Manager found in scene.");
		}
	}

	/// <summary>
	///  Clean up when app closes
	/// </summary>
	void OnDestroy(){
		#if UNITY_EDITOR
		return;
		#endif

		base.OnDestroy ();
		m_tangoApplication.Unregister (this);
	}

	/// <summary>
	///  Gets permission of Project Tango to use Motion Control
	/// </summary>
	/// <param name="success">Whether we are given permission</param> 
	private void PermissionsCallback(bool success)
	{
		if(success)
		{
			m_tangoApplication.InitApplication(); // Initialize Tango Client
			m_tangoApplication.InitProviders(string.Empty); // Initialize listeners
			m_tangoApplication.ConnectToService(); // Connect to Tango Service
		}
		else
		{
			#if UNITY_ANDROID
			AndroidHelper.ShowAndroidToastMessage("Motion Tracking Permissions Needed", true);
			#endif
		}
	}

	/// <summary>
	///  Updates the virtual device position and rotation from interpolation data
	/// </summary>
	/// <param name="t">The time between each poll of the sensor data</param> 
	void UpdateUsingInterpolatedPose(double t) {
		float dt = (float)((t - prevPose.timestamp)/(currPose.timestamp - prevPose.timestamp));
		//restrict this, so it isn't doesn't swing out of control
		if(dt > 4)
			dt = 4;
		
		Vector3 currPos = new Vector3();
		Vector3 prevPos = new Vector3();
		Quaternion currRot = new Quaternion();
		Quaternion prevRot = new Quaternion();
		
		ComputeTransformUsingPose(out currPos, out currRot, currPose);
		ComputeTransformUsingPose(out prevPos, out prevRot, prevPose);

		//We actually do not want to zero the rotation, because that will need to awkard rotations on the sphere
		transform.rotation = m_startRotation*Quaternion.Inverse(m_zeroRotation)*Quaternion.Slerp (prevRot, currRot, dt);
		//transform.rotation = m_startRotation*Quaternion.Slerp (prevRot, currRot, dt);
		transform.position = m_startRotation*(Vector3.Lerp(prevPos, currPos, dt) - m_zeroPosition)*m_metersToWorldUnitsScaler + m_startPosition;
	}
	
	/// <summary>
	/// Computes position and rotation based on sensor data from TangoSDK
	/// </summary>
	/// <param name="position">The output position from the calculation</param> 
	/// <param name="rot">The output rotation from the calculation</param> 
	/// <param name="pose">The input pose data</param> 
	void ComputeTransformUsingPose(out Vector3 position, out Quaternion rot, TangoPoseData pose) {
		position = new Vector3((float)pose.translation [0],
		                       (float)pose.translation [2],
		                       (float)pose.translation [1]);
		
		rot = new Quaternion((float)pose.orientation [0],
		                     (float)pose.orientation [2], // these rotation values are swapped on purpose
		                     (float)pose.orientation [1],
		                     (float)pose.orientation [3]);
		
		
		// This rotation needs to be put into Unity coordinate space.
		Quaternion axisFix = Quaternion.Euler(-rot.eulerAngles.x,
		                                      -rot.eulerAngles.z,
		                                      rot.eulerAngles.y);
		Quaternion rotationFix = Quaternion.Euler(90.0f, 0.0f, 0.0f);
		rot = rotationFix * axisFix;
	}

	/// <summary>
	/// Makes a deep copy of the sensor data
	/// </summary>
	/// <param name="other">The object we would like to copy</param> 
	private TangoPoseData DeepCopyTangoPose(TangoPoseData other)
	{
		TangoPoseData poseCopy = new TangoPoseData();
		poseCopy.version = other.version;
		poseCopy.timestamp = other.timestamp;
		poseCopy.orientation = other.orientation;
		poseCopy.translation = other.translation;
		poseCopy.status_code = other.status_code;
		poseCopy.framePair.baseFrame = other.framePair.baseFrame;
		poseCopy.framePair.targetFrame = other.framePair.targetFrame;
		poseCopy.confidence = other.confidence;
		poseCopy.accuracy = other.accuracy;
		return poseCopy;
	}

	/// <summary>
	/// Updates instance pose data based on new data point
	/// </summary>
	/// <param name="pose">The pose data</param> 
	private void UpdateInterpolationData(TangoPoseData pose) {
		prevPose = currPose;
		// We need to make sure to deep copy the pose because it is
		// only guaranteed to be valid for the duration of our callback.
		currPose = DeepCopyTangoPose(pose);
		float timestampSmoothing = 0.95f;
		if(unityTimestampOffset < float.Epsilon)
			unityTimestampOffset = (float)pose.timestamp - Time.realtimeSinceStartup;
		else
			unityTimestampOffset = timestampSmoothing*unityTimestampOffset + (1-timestampSmoothing)*((float)pose.timestamp - Time.realtimeSinceStartup);
	}
	
	/// <summary>
	/// Handle the callback sent by the Tango Service
	/// when a new pose is sampled.
	/// </summary>
	/// <param name="callbackContext">Callback context.</param>
	/// <param name="pose">Pose.</param>
	public void OnTangoPoseAvailable(Tango.TangoPoseData pose)
	{
		// The callback pose is for device with respect to start of service pose.
		if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
		    pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
		{
			if(pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
			{
				
				UpdateInterpolationData(pose);
				if(unityTimestampOffset > float.Epsilon) {
					UpdateUsingInterpolatedPose(Time.realtimeSinceStartup + unityTimestampOffset);
				} 
			}
		}
	}

	/// <summary>
	///  How to reset the position of the device
	/// </summary>
	public override void Reset() {
		ComputeTransformUsingPose (out m_zeroPosition, out m_zeroRotation, currPose);
	}
	
}
