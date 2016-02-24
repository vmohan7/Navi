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
using UnityEngine.Networking;

using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

/// <summary>
///  This class does most of the heavy-lifting for connecting the PC to the smart device.
///  This class can only have one Instance running for a given game and can be accessed via the Instance variable.
///  The Instance variable allows you access all public methods and variables. 
///  The smart device sends data on two connections: one that sends pose data and another that sends RPC touch data
/// </summary>
public class NaviConnectionSDK : MonoBehaviour {

	//must be set to the DevicePrefab GameObject
	public NaviDevice DeviceLocationPrefab;

	public static NaviConnectionSDK Instance;

	//	Events that occur during setup of the Navi connection	
	public delegate void DeviceAction(int connectionID );
	public static event DeviceAction OnDeviceConnected; 
	public static event DeviceAction OnDeviceDisconnected;

	public static event DeviceAction OnDeviceSizeChange;
	public static event DeviceAction OnDevicePlatformRecieved;

	public delegate void GameStartAction();
	public static event GameStartAction OnGameStart;

	public delegate void PoseAction(int connectionID, Vector3 position, Quaternion rotation);
	public static event PoseAction OnPoseData;

	public delegate void AccelerationAction(int connectionID, Vector3 acceleration);
	public static event AccelerationAction OnAccelerationData;

	public delegate void HMDResetAction();
	public static event HMDResetAction OnResetHMD; //add your event here in case you want to do anything special on recenterting

	public delegate void KeyboardAction(int player_id, string currText);
	public static event KeyboardAction OnKeyboardText; //add your event here in case you want to do anything special on recenterting
	public static event KeyboardAction OnKeyboardClosed; //add your event here in case you want to do anything special on recenterting

	[HideInInspector]
	public float LoadingPercent = 0f; //a property that is set while an asset bundle is being uploaded
	public delegate void AssetUploadedAction(int player_id);
	public static event AssetUploadedAction OnAssetUploaded; //add your event here in case you want to do anything special on recenterting


	//total number of devices we support for app
	public const int NUM_CONNECTIONS = 4;

	public const int SERVER_PORT = 8888;
	public const int UDP_SERVER_PORT = 1204;
	public const int REMOTE_PORT = 19784;

	private const int BUFFER_SIZE = 1024;
	private const int ASSET_SEND_SIZE = 20000;

	private const int SDK_BUILD_NO = 1; //current SDK version ID that will be sent to the mobile app for proper sync

	//keywords that trigger events on the smart device
	private const string SEND_DATA_METHOD_STR = "SendData";
	private const string RESET_STR = "Reset";

	private const string BUILD_MESSAGE_ID = "BuildNo";
	private const string ASSIGN_DEVICE_ID = "DeviceNo";
	private const string VIBRATE_ID = "Vibrate";
	private const string INSTRUCTION_ID = "SetInstruction";
	private const string ASSET_ID = "SetBundle";

	private const string GAMEOBJ_LOC_ID = "SetGameObjLoc";
	private const string GAMEOBJ_ROT_ID = "SetGameObjRot";
	private const string GAMEOBJ_ANIM_ID = "SetGameObjAnim";
	private const string GAMEOBJ_RENDER_ID = "SetGameObjRendState";
	private const string GAMEOBJ_DUP_ID = "DupGameObject";
	private const string GAMEOBJ_DEL_ID = "DeleteGameObject";

	private const string TOUCH_METHOD_ID = "TouchIO";
	private const string SET_SIZE_METHOD_ID = "SetSize";
	private const string SEND_PLATFORM_METHOD_ID = "SendPlatform";

	private const string OPEN_KEYBOARD_ID = "OpenKey";
	private const string CLOSE_KEYBOARD_ID = "CloseKey";
	private const string SEND_KEYBOARD_ID = "SendKey";
	private const string CLEAR_KEYBOARD_ID = "ClearKey";

	private const string SET_DEVICE_ROTATION_ID = "RotationKey";

	private const string IP_SPLITER = "N4V1_SPLIT"; 

	private const string CONTROL_INSTRUCTIONS = "Controls:\n1.Tap with 5 fingers to reset\n2.To change controller #, tap with 5 fingers and then touch the screen with number of fingers\n= the device number\nuntil vibrates";

	//Used to find navi application on smart device
	private UdpClient sender;

	[HideInInspector]
	public int socketID;

	[HideInInspector]
	public List<NaviDevice> playerConnectionIds = new List<NaviDevice>();	//list of all connected devices; index is player id 

	[HideInInspector]
	public int myReliableChannelId;
	[HideInInspector]
	public int myUnreliableChannelId;
	[HideInInspector]
	public int myReliableFragmentedChannelId;

	//Used to start game for the first time
	private bool initalReset = true;

	public const float ResetWaitTime = 2f;
	public const float ResetHoldTime = 2f;

	/// <summary>
	/// Resets the HMD and smart device. Will call OnGameStart once both the device and HMD have been reset.
	/// This method is mapped to a 5 finger tap. So whenever a user taps with 5 fingers they will be reset
	/// </summary>
	public void ResetVR(int playerID) {
		if (playerID == 0) {
			if (OnResetHMD != null)
				OnResetHMD (); 

			ResetDevice ();

			if (OnGameStart != null && initalReset) {
				initalReset = false;
				OnGameStart ();
			}
		}

		NaviDevice dev = GetPlayerPose (playerID);

		dev.playerResetTimer = Time.time;
		dev.playerSwitchEnabled = true;
	}

	/// <summary>
	/// After a reset this method is called, if the player would like to change which device number they are. 
	/// To change which device number they are, they need to hold & press the number of fingers of the device they want to become.
	/// I.e. if they want to become player 2, they touch their device with 2 fingers.
	/// </summary>
	public void StartPlayerSwitchIds(int playerID, int fingerID, Vector2 pos){
		NaviDevice dev = GetPlayerPose (playerID);
		dev.numFingersDown++;
		if (dev.playerSwitchEnabled && (Time.time - dev.playerResetTimer) <= ResetWaitTime) { //you have 2 seconds to place next finger to keep playerReset option
			dev.playerResetTimer = Time.time;
		} else {
			dev.playerSwitchEnabled = false;
		}
	}

	/// <summary>
	/// Method that is called when a player wants to switch to a given device id.
	/// </summary>
	public void SwitchPlayerIds(int playerID, int fingerID, Vector2 pos){
		NaviDevice dev = GetPlayerPose (playerID);
		if (dev.playerSwitchEnabled && (Time.time - dev.playerResetTimer) >= ResetHoldTime) { // hold num fingers on screen for required time
			int dev2PlayerID = dev.numFingersDown - 1;
			NaviDevice dev2 = GetPlayerPose (dev2PlayerID);
			if (dev2 == null) {
				dev2PlayerID = playerConnectionIds.Count-1;
				dev2 = GetPlayerPose(dev2PlayerID); //zero based
			}

			playerConnectionIds[dev2PlayerID] = dev;
			playerConnectionIds[playerID] = dev2;

			//send playerIds to device
			ChangePlayerID(dev2PlayerID, dev.connectionID);
			ChangePlayerID(playerID, dev2.connectionID);

			BinaryFormatter formatter = new BinaryFormatter();
			byte[] buffer = new byte[BUFFER_SIZE];
			Stream stream = new MemoryStream(buffer);
			byte error;

			//vibrate the two devices in recogniztion of switching
			RPCSerializer rpc = new RPCSerializer();
			rpc.methodName = VIBRATE_ID;
			formatter.Serialize(stream, rpc);
			NetworkTransport.Send(socketID, dev.connectionID, myReliableChannelId, buffer, BUFFER_SIZE, out error); 
			NetworkTransport.Send(socketID, dev2.connectionID, myReliableChannelId, buffer, BUFFER_SIZE, out error); 

			dev.playerSwitchEnabled = false;
		}
	}

	/// <summary>
	/// Listener for when the user touches up on the device
	/// </summary>
	public void TouchUp(int playerID, int fingerID, Vector2 pos){
		NaviDevice dev = GetPlayerPose (playerID);
		dev.numFingersDown--;

		if (dev.playerSwitchEnabled && (Time.time - dev.playerResetTimer) > ResetWaitTime) {
			dev.playerSwitchEnabled = false;
		}
	}

	/// <summary>
	/// Method to tell that device that it has changed to a different player number
	/// </summary>
	private void ChangePlayerID(int newID, int currConnection){
		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = ASSIGN_DEVICE_ID;
		rpc.args = new object[1];
		rpc.args [0] = newID+1;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, currConnection, myReliableChannelId, buffer, BUFFER_SIZE, out error); 
	}

	/// <summary>
	/// Method to tell connected device that it should open its mobile keyboard
	/// </summary>
	public void OpenMobileKeyboard(int player_id, string initalText, bool hideInputOnKeyboards){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = OPEN_KEYBOARD_ID;
		rpc.args = new object[2];
		rpc.args [0] = initalText;
		rpc.args [1] = hideInputOnKeyboards; //broken on Android
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}

	/// <summary>
	/// Method to close mobile keyboard on device
	/// </summary>
	public void CloseMobileKeyboard(int player_id){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = CLOSE_KEYBOARD_ID;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}

	/// <summary>
	/// Method to clear the text on the mobile keyboard
	/// </summary>
	public void ClearMobileKeyboard(int player_id) {
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = CLEAR_KEYBOARD_ID;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}

	/// <summary>
	/// Method to set the device orientation on the mobile device. This uses Unity's enum and sends that to the device
	/// </summary>
	public void SetDeviceOrientaton(int player_id, ScreenOrientation orient, bool canUserChange) {
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = SET_DEVICE_ROTATION_ID;
		rpc.args = new object[2];
		rpc.args [0] = orient;
		rpc.args [1] = canUserChange;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}

	/// <summary>
	/// Sets the location of a gameobject located on the mobile device. This should be used after you have uploaded an asset bundle to the device. 
	/// </summary>
	public void SetObjLocation(int player_id, string objName, Vector3 location){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = GAMEOBJ_LOC_ID;
		rpc.args = new object[4];
		rpc.args [0] = objName;
		rpc.args [1] = location.x;
		rpc.args [2] = location.y;
		rpc.args [3] = location.z;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}


	/// <summary>
	/// Animates a gameobject to a given location on the mobile device. This should be used after you have uploaded an asset bundle to the device. 
	/// </summary>
	public void AnimObjLocation(int player_id, string objName, Vector3 location, float time){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = GAMEOBJ_ANIM_ID;
		rpc.args = new object[5];
		rpc.args [0] = objName;
		rpc.args [1] = location.x;
		rpc.args [2] = location.y;
		rpc.args [3] = location.z;
		rpc.args [4] = time;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}


	/// <summary>
	/// Sets whether to render a gameobject located on the mobile device. This should be used after you have uploaded an asset bundle to the device. 
	/// </summary>
	public void RenderObj(int player_id, string objName, bool render){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = GAMEOBJ_RENDER_ID;
		rpc.args = new object[4];
		rpc.args [0] = objName;
		rpc.args [1] = render;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}


	/// <summary>
	/// Sets the rotation of a gameobject located on the mobile device. This should be used after you have uploaded an asset bundle to the device. 
	/// </summary>
	public void SetObjRotation(int player_id, string objName, Quaternion rot){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = GAMEOBJ_ROT_ID;
		rpc.args = new object[5];
		rpc.args [0] = objName;
		rpc.args [1] = rot.x;
		rpc.args [2] = rot.y;
		rpc.args [3] = rot.z;
		rpc.args [4] = rot.w;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}


	/// <summary>
	/// Destroys a gameobject located on the mobile device. This should be used after you have uploaded an asset bundle to the device. 
	/// </summary>
	public void DestroyObj(int player_id, string objName){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = GAMEOBJ_DEL_ID;
		rpc.args = new object[1];
		rpc.args [0] = objName;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}


	/// <summary>
	/// Duplicate a gameobject located on the mobile device. This should be used after you have uploaded an asset bundle to the device. 
	/// </summary>
	public void DuplicateObj(int player_id, string objName, string newName, Vector3 pos, Quaternion rot){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = GAMEOBJ_DUP_ID;
		rpc.args = new object[9];
		rpc.args [0] = objName;
		rpc.args [1] = newName;

		rpc.args [2] = pos.x;
		rpc.args [3] = pos.y;
		rpc.args [4] = pos.z;

		rpc.args [5] = rot.x;
		rpc.args [6] = rot.y;
		rpc.args [7] = rot.z;
		rpc.args [8] = rot.w;

		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, buffer.Length, out error); 
	}


	/// <summary>
	/// Sends an asset bundle to to the mobile device in order to render the scene 
	/// </summary>
	public void SendAssetBundle(int player_id, TextAsset assetFile){
		byte[] bytesTex = assetFile.bytes;

		StartCoroutine (SendAssetData (player_id, bytesTex));
	}

	/// <summary>
	/// Private method to iteratively send asset data to the mobile device, because in general that file will be too big 
	/// </summary>
	private IEnumerator SendAssetData(int player_id, byte[] file){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[ASSET_SEND_SIZE + BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = ASSET_ID;
		rpc.args = new object[2];
		rpc.args [0] = 0;
		rpc.args [1] = file.Length;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableFragmentedChannelId, buffer, buffer.Length, out error); 
		yield return new WaitForSeconds(.5f); //wait for network packets to send

		//for loop to send chunks of data to the device
		for (int i = 0; i < file.Length; i+=ASSET_SEND_SIZE) {
			buffer = new byte[ASSET_SEND_SIZE + BUFFER_SIZE];
			stream = new MemoryStream(buffer);

			int remaining = file.Length - i;
			int l = ASSET_SEND_SIZE > remaining ? remaining : ASSET_SEND_SIZE;
			byte[] data = new byte[l];
			for (int j = 0; j < l; j++) {
				data [j] = file [i + j];
			}

			rpc = new RPCSerializer();
			rpc.methodName = ASSET_ID;
			rpc.args = new object[3];
			rpc.args [0] = 1; //set index
			rpc.args [1] = i;
			rpc.args [2] = data;
			formatter.Serialize(stream, rpc);
			NetworkTransport.Send(socketID, connection_id, myReliableFragmentedChannelId, buffer, buffer.Length, out error); 
			LoadingPercent = ((float)i / (float)file.Length);
			NaviConnectionSDK.Instance.SetInstructions (player_id, "Loading\n"+ (int) (LoadingPercent * 100f) + "%");	

			while (error != 0) {
				yield return new WaitForSeconds(.1f); //wait for network packets to clear
				NetworkTransport.Send(socketID, connection_id, myReliableFragmentedChannelId, buffer, buffer.Length, out error); 
			}
		}

		yield return new WaitForSeconds(.5f); //wait for network packets to send

		buffer = new byte[ASSET_SEND_SIZE + BUFFER_SIZE];
		stream = new MemoryStream(buffer);

		rpc = new RPCSerializer();
		rpc.methodName = ASSET_ID;
		rpc.args = new object[1];
		rpc.args [0] = -1;
		formatter.Serialize(stream, rpc);

		//sends asset bundle completed message
		NetworkTransport.Send(socketID, connection_id, myReliableFragmentedChannelId, buffer, buffer.Length, out error); 

		while (error != 0) {
			yield return new WaitForSeconds(.1f); //wait for network packets to clear
			NetworkTransport.Send(socketID, connection_id, myReliableFragmentedChannelId, buffer, buffer.Length, out error); 
		}

		NaviConnectionSDK.Instance.SetInstructions (player_id, "");	
		if (OnAssetUploaded != null)
			OnAssetUploaded (player_id);
	}

	/// <summary>
	/// Method to set the instructions on the instruction panel screen of the mobile device
	/// </summary>
	public void SetInstructions(int player_id, string instructions){
		int connection_id = GetPlayerPose (player_id).connectionID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[ASSET_SEND_SIZE + BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = INSTRUCTION_ID;
		rpc.args = new object[1];
		rpc.args[0] = instructions; 
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableChannelId, buffer, BUFFER_SIZE, out error); 
	}

	/// <summary>
	/// Public Method to reset just the smart device
	/// </summary>
	public void ResetDevice(){
		sender.Send (Encoding.ASCII.GetBytes (RESET_STR), RESET_STR.Length);
	}

	/// <summary>
	/// First function that is called when scene is loading
	/// </summary>
	void Awake(){
		OnResetHMD += UnityEngine.VR.InputTracking.Recenter; //defaults to recenter DK2 or GearVR

		if (Instance == null)
			Instance = this;

		DontDestroyOnLoad (this.gameObject); //makes sure this game object or its children does not get destroyed when switching scenes
	}

	/// <summary>
	/// Called after Gameobject is initalized. We start sending packets to find Navi-compatible devices
	/// </summary>
	void Start(){
		NaviConnectionSDK.OnDeviceConnected += DeviceConnected;
		TouchManager.OnTouchDown += StartPlayerSwitchIds;
		TouchManager.OnTouchMove += SwitchPlayerIds;
		TouchManager.OnTouchStayed += SwitchPlayerIds;
		TouchManager.OnTouchUp += TouchUp;

		CreateSocket ();
		StartSendingIP (); //should always be broadcasting and looking for other devices
	}

	/// <summary>
	/// Called every frame to process Network events.
	/// </summary>
	void Update()
	{
		int recSocketId; 
		int connectionId; 
		int channelId; 
		byte[] recBuffer = new byte[BUFFER_SIZE]; 
		int dataSize;
		byte error;
		NetworkEventType recData = NetworkTransport.Receive(out recSocketId, out connectionId, out channelId, recBuffer, BUFFER_SIZE, out dataSize, out error);

		while (recData != NetworkEventType.Nothing) { 
			//TODO add some limit so that it does not stall from this while loop i.e. 1000 loops or something
			switch (recData) {
			case NetworkEventType.Nothing:
				break;
			case NetworkEventType.ConnectEvent: //connection was made
				OnConnection (connectionId);
				break;
			case NetworkEventType.DataEvent: //data was received
				if (channelId == myUnreliableChannelId)
					HandlePoseData (connectionId, recBuffer);
				else if (channelId == myReliableChannelId)
					HandleRPC (connectionId, recBuffer);
				break;
			case NetworkEventType.DisconnectEvent: //device disconnected
				OnDisconnect (connectionId);
				break;
			}

			recData = NetworkTransport.Receive(out recSocketId, out connectionId, out channelId, recBuffer, BUFFER_SIZE, out dataSize, out error);
		}
	}

	/// <summary>
	/// Called when object is destroyed i.e. when game ends
	/// </summary>
	void OnDestroy(){
		NaviConnectionSDK.OnDeviceConnected -= DeviceConnected;
		Instance = null;
	}

	/// <summary>
	/// Gets the device information for the specific player
	/// </summary>
	/// <param name="playerID">The id of the player, where 0 is the controller that is used by the headset</param>
	public NaviDevice GetPlayerPose(int playerID){
		if (playerConnectionIds.Count > playerID) {
			return playerConnectionIds [playerID];
		} else {
			return null;
		}
	}

	/// <summary>
	/// Method to parse and send an event when Pose Data is transfer from smart device
	/// </summary>
	/// <param name="connectionID">The id of the device that connected</param>
	/// <param name="recBuffer">The data that was recieved from the smart device</param>
	private void HandlePoseData(int connectionID, byte[] recBuffer){
		Stream stream = new MemoryStream(recBuffer);
		BinaryFormatter formatter = new BinaryFormatter();
		PoseSerializerWithAcceleration poseData = (PoseSerializerWithAcceleration)formatter.Deserialize (stream);

		if (OnPoseData != null) {
			OnPoseData (connectionID, poseData.Position, poseData.Rotation);
		}

		if (OnAccelerationData != null) {
			OnAccelerationData (connectionID, poseData.Acceleration);
		}

	}

	/// <summary>
	/// Given the connection id, we get the player id index
	/// </summary>
	/// <param name="connectionID">The id of the device that connected</param>
	private int GetPlayerID(int connectionID){
		for (int i = 0; i < playerConnectionIds.Count; i++) {
			if ( playerConnectionIds[i].connectionID == connectionID){
				return i;
			}
		}
		return -1;
	}

	/// <summary>
	/// Method to parse and send an RPC event from smart device such as recieving touch input
	/// </summary>
	/// <param name="connectionID">The id of the device that connected</param>
	/// <param name="recBuffer">The data that was recieved from the smart device</param>
	private void HandleRPC(int connectionID, byte[] recBuffer){
		Stream stream = new MemoryStream(recBuffer);
		BinaryFormatter formatter = new BinaryFormatter();
		RPCSerializer rpcData = (RPCSerializer) formatter.Deserialize(stream);

		int playerID = -1;
		NaviDevice dev = null;
		for (int i = 0; i < playerConnectionIds.Count; i++) {
			if ( playerConnectionIds[i].connectionID == connectionID){
				playerID = i;
				dev = playerConnectionIds[i];
				break;
			}
		}

		if (rpcData.methodName.Equals (TOUCH_METHOD_ID)) {
			TouchSerializer ts = (TouchSerializer)rpcData.args [0];
			TouchManager.ProcessTouch (playerID, ts);
		} else if (rpcData.methodName.Equals (SET_SIZE_METHOD_ID)) {
			dev.SetServerScreenSize ((int)rpcData.args [0], (int)rpcData.args [1]);

			if (OnDeviceSizeChange != null)
				OnDeviceSizeChange(playerID);

		} else if (rpcData.methodName.Equals (SEND_PLATFORM_METHOD_ID)) {
			dev.devicePlatform = (RuntimePlatform) rpcData.args [0];

			if (OnDevicePlatformRecieved != null)
				OnDevicePlatformRecieved(playerID);

		} else if (rpcData.methodName.Equals (SEND_KEYBOARD_ID)) {
			if (OnKeyboardText != null)
				OnKeyboardText (playerID, (string)rpcData.args [0]);
		} else if (rpcData.methodName.Equals (CLOSE_KEYBOARD_ID)) {
			if (OnKeyboardClosed != null)
				OnKeyboardClosed (playerID, (string)rpcData.args [0]);
		}
	}

	/// <summary>
	/// Method that is called when one of the connection disconnects. 
	/// </summary>
	/// <param name="recSocketId">The id of the device that is connecting</param>
	/// <param name="connectionID">The connection id that disconnected</param>
	private void OnDisconnect(int connectionID){
		for (int i = 0; i < playerConnectionIds.Count; i++) {
			NaviDevice naviDevice = playerConnectionIds[i];
			if ( naviDevice.connectionID == connectionID){
				playerConnectionIds.Remove (naviDevice);
				GameObject.Destroy(naviDevice.gameObject);

				if (OnDeviceDisconnected != null)
					OnDeviceDisconnected(i);

				break;
			}
		}
	}

	/// <summary>
	/// Method that is called when a connection is made. We then send a message back to the smart device 
	/// so that the smart device knows which connection it should use to send different types of data.
	/// OnDeviceConnected is called when all connections have been made.
	/// </summary>
	/// <param name="connectionID">The id of the connection was made</param>
	private void OnConnection(int connectionID){
		//string message = "";
		//CancelInvoke(SEND_DATA_METHOD_STR);

		NaviDevice deviceLocation = (Instantiate (DeviceLocationPrefab.gameObject) as GameObject).GetComponent<NaviDevice> ();
		deviceLocation.connectionID = connectionID;

		playerConnectionIds.Add (deviceLocation);

		int playerID = playerConnectionIds.Count; //id is the last element of the array
		deviceLocation.name = "Device #" + playerID;

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;

		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = ASSIGN_DEVICE_ID;
		rpc.args = new object[1];
		rpc.args [0] = playerID;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connectionID, myReliableChannelId, buffer, BUFFER_SIZE, out error); 

		//send build number
		buffer = new byte[BUFFER_SIZE];
		stream = new MemoryStream(buffer);

		rpc = new RPCSerializer();
		rpc.methodName = BUILD_MESSAGE_ID;
		rpc.args = new object[1];
		rpc.args[0] = SDK_BUILD_NO; 
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connectionID, myReliableChannelId, buffer, BUFFER_SIZE, out error); 

		//send instructions
		buffer = new byte[BUFFER_SIZE];
		stream = new MemoryStream(buffer);

		rpc = new RPCSerializer();
		rpc.methodName = INSTRUCTION_ID;
		rpc.args = new object[1];
		rpc.args[0] = CONTROL_INSTRUCTIONS; 
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connectionID, myReliableChannelId, buffer, BUFFER_SIZE, out error); 

		if (OnDeviceConnected != null) {
			OnDeviceConnected ( GetPlayerID(connectionID) );
		}
	}

	#if UNITY_ANDROID && !UNITY_EDITOR
	AndroidJavaObject hotspotJavaObj = null;

	public AndroidJavaObject GetMobileHotspotObj(){
	if (hotspotJavaObj == null) {
	hotspotJavaObj = new AndroidJavaObject("navi.net.androidhotspot.MobileHotspot");
	AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
	AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
	hotspotJavaObj.Call("SetUnityActivity", jo);
	}

	return hotspotJavaObj;
	}

	#endif


	/// <summary>
	/// Method that will start broadcasting packets to find a smart device
	/// </summary>
	private void StartSendingIP ()
	{
		sender = new UdpClient (NaviConnectionSDK.UDP_SERVER_PORT, AddressFamily.InterNetwork);
		IPEndPoint groupEP;
		#if UNITY_ANDROID && !UNITY_EDITOR
		string hsIP = GetMobileHotspotObj().Call<string>("getBroadcastAddress");
		if (hsIP != null) {
		groupEP = new IPEndPoint (IPAddress.Parse( hsIP ), NaviConnectionSDK.REMOTE_PORT);
		} else {
		groupEP = new IPEndPoint (IPAddress.Broadcast, NaviConnectionSDK.REMOTE_PORT);
		}
		#else
		groupEP = new IPEndPoint (IPAddress.Broadcast, NaviConnectionSDK.REMOTE_PORT);
		#endif
		sender.Connect (groupEP);

		InvokeRepeating(SEND_DATA_METHOD_STR,0,0.3f); //send data every half a second
	}

	/// <summary>
	/// Method that is called on an interval to broadcast the ipAddress for listening devices
	/// </summary>
	void SendData ()
	{
		//TODO: consider sending the port number as well
		string ipAddress = Network.player.ipAddress.ToString();
		string message = Application.productName + IP_SPLITER + ipAddress;

		if (message != "") {
			byte[] b = Encoding.ASCII.GetBytes (message);
			sender.Send (b, b.Length);
		}

	}

	/// <summary>
	/// Method that creates the socket on the PC that will be used for receiving data
	/// </summary>
	private void CreateSocket(){
		NetworkTransport.Init();
		ConnectionConfig config = new ConnectionConfig();

		myReliableChannelId  = config.AddChannel(QosType.Reliable);
		myUnreliableChannelId = config.AddChannel(QosType.Unreliable);
		myReliableFragmentedChannelId = config.AddChannel(QosType.ReliableFragmented);

		HostTopology topology = new HostTopology(config, NUM_CONNECTIONS); //only supports one other connection

		socketID = NetworkTransport.AddHost(topology, NaviConnectionSDK.SERVER_PORT);
	}

	/// <summary>
	/// Method that is called when a smart device connects
	/// </summary>
	private void DeviceConnected(int connectionID){
		if (OnResetHMD != null)
			OnResetHMD ();
		GestureManager.OnFiveFingerTap += ResetVR; //enable reset at any time
	}
}