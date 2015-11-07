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
	
	public delegate void GameStartAction();
	public static event GameStartAction OnGameStart;
	
	public delegate void PoseAction(int connectionID, Vector3 position, Quaternion rotation);
	public static event PoseAction OnPoseData;
	
	public delegate void AccelerationAction(int connectionID, Vector3 acceleration);
	public static event AccelerationAction OnAccelerationData;
	
	public delegate void HMDResetAction();
	public static event HMDResetAction OnResetHMD; //add your event here in case you want to do anything special on recenterting

	//total number of devices we support for app
	public const int NUM_CONNECTIONS = 2;
	
	public const int SERVER_PORT = 8888;
	public const int UDP_SERVER_PORT = 1204;
	public const int REMOTE_PORT = 19784;
	
	private const int BUFFER_SIZE = 1024;
	
	private const int SDK_BUILD_NO = 1; //current SDK version ID that will be sent to the mobile app for proper sync
	
	//keywords that trigger events on the smart device
	private const string SEND_DATA_METHOD_STR = "SendData";
	private const string RESET_STR = "Reset";

	private const string BUILD_MESSAGE_ID = "BuildNo";
	private const string ASSIGN_DEVICE_ID = "DeviceNo";
	private const string VIBRATE_ID = "Vibrate";
	private const string INSTRUCTION_ID = "SetInstruction";
	private const string IMAGE_ID = "SetImage";
	
	private const string TOUCH_METHOD_ID = "TouchIO";
	private const string SET_SIZE_METHOD_ID = "SetSize";

	private const string CONTROL_INSTRUCTIONS = "Controls:\n1.Tap with 5 fingers to reset\n2.To change controller #, tap with 5 fingers and then touch the screen with number of fingers\n= the device number\nuntil vibrates";
	
	//Used to find navi application on smart device
	private UdpClient sender;
	
	[HideInInspector]
	public int socketID;
	
	[HideInInspector]
	public List<NaviDevice> playerConnectionIds = new List<NaviDevice>();	//list of all connected devices; index is player id 
	
	[HideInInspector]
	public int myReiliableChannelId;
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

	//TODO: comment what is going on here
	//Basic is that immediately after a reset, you need to hold X fingers on the device for 5 seconds, where X is the player number you would like to become
	public void StartPlayerSwitchIds(int playerID, int fingerID, Vector2 pos){
		NaviDevice dev = GetPlayerPose (playerID);
		dev.numFingersDown++;
		if (dev.playerSwitchEnabled && (Time.time - dev.playerResetTimer) <= ResetWaitTime) { //you have 2 seconds to place next finger to keep playerReset option
			dev.playerResetTimer = Time.time;
		} else {
			dev.playerSwitchEnabled = false;
		}
	}

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
			NetworkTransport.Send(socketID, dev.connectionID, myReiliableChannelId, buffer, BUFFER_SIZE, out error); 
			NetworkTransport.Send(socketID, dev2.connectionID, myReiliableChannelId, buffer, BUFFER_SIZE, out error); 

			dev.playerSwitchEnabled = false;
		}
	}

	public void TouchUp(int playerID, int fingerID, Vector2 pos){
		NaviDevice dev = GetPlayerPose (playerID);
		dev.numFingersDown--;

		if (dev.playerSwitchEnabled && (Time.time - dev.playerResetTimer) > ResetWaitTime) {
			dev.playerSwitchEnabled = false;
		}
	}

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
		NetworkTransport.Send(socketID, currConnection, myReiliableChannelId, buffer, BUFFER_SIZE, out error); 
	}

	public void SendImage(int player_id, Texture2D tex){
		int connection_id = GetPlayerPose (player_id).connectionID;

		byte[] bytesTex = tex.EncodeToPNG ();

		BinaryFormatter formatter = new BinaryFormatter();
		byte[] buffer = new byte[bytesTex.Length + BUFFER_SIZE];
		Stream stream = new MemoryStream(buffer);
		byte error;
		
		//send player id
		RPCSerializer rpc = new RPCSerializer();
		rpc.methodName = IMAGE_ID;
		rpc.args = new object[1];
		rpc.args [0] = bytesTex;
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connection_id, myReliableFragmentedChannelId, buffer, buffer.Length, out error); 
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
				else if (channelId == myReiliableChannelId)
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
			dev.SetServerScreenSize((int)rpcData.args[0], (int)rpcData.args[1]);
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
				GameObject.Destroy(naviDevice);

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
		NetworkTransport.Send(socketID, connectionID, myReiliableChannelId, buffer, BUFFER_SIZE, out error); 

		//send build number
		buffer = new byte[BUFFER_SIZE];
		stream = new MemoryStream(buffer);
		
		rpc = new RPCSerializer();
		rpc.methodName = BUILD_MESSAGE_ID;
		rpc.args = new object[1];
		rpc.args[0] = SDK_BUILD_NO; 
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connectionID, myReiliableChannelId, buffer, BUFFER_SIZE, out error); 

		//send instructions
		buffer = new byte[BUFFER_SIZE];
		stream = new MemoryStream(buffer);
		
		rpc = new RPCSerializer();
		rpc.methodName = INSTRUCTION_ID;
		rpc.args = new object[1];
		rpc.args[0] = CONTROL_INSTRUCTIONS; 
		formatter.Serialize(stream, rpc);
		NetworkTransport.Send(socketID, connectionID, myReiliableChannelId, buffer, BUFFER_SIZE, out error); 
		
		if (OnDeviceConnected != null) {
			OnDeviceConnected ( GetPlayerID(connectionID) );
		}
	}
	
	/// <summary>
	/// Method that will start broadcasting packets to find a smart device
	/// </summary>
	private void StartSendingIP ()
	{
		sender = new UdpClient (NaviConnectionSDK.UDP_SERVER_PORT, AddressFamily.InterNetwork);
		IPEndPoint groupEP = new IPEndPoint (IPAddress.Broadcast, NaviConnectionSDK.REMOTE_PORT);
		sender.Connect (groupEP);
		
		InvokeRepeating(SEND_DATA_METHOD_STR,0,0.5f); //send data every half a second
	}
	
	/// <summary>
	/// Method that is called on an interval to broadcast the ipAddress for listening devices
	/// </summary>
	void SendData ()
	{
		//TODO: consider sending the port number as well
		string ipAddress = Network.player.ipAddress.ToString();
		
		if (ipAddress != "") {
			sender.Send (Encoding.ASCII.GetBytes (ipAddress), ipAddress.Length);
		}
		
	}
	
	/// <summary>
	/// Method that creates the socket on the PC that will be used for receiving data
	/// </summary>
	private void CreateSocket(){
		NetworkTransport.Init();
		ConnectionConfig config = new ConnectionConfig();
		
		myReiliableChannelId  = config.AddChannel(QosType.Reliable);
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