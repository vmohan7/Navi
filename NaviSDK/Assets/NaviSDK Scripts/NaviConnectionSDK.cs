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
	
	public static NaviConnectionSDK Instance;

//	Events that occur during setup of the Navi connection	
	public delegate void DeviceAction();
	public static event DeviceAction OnDeviceConnected;
	public static event DeviceAction OnDeviceDisconnected;
	
	public delegate void GameStartAction();
	public static event GameStartAction OnGameStart;
	
	public delegate void PoseAction(Vector3 position, Quaternion rotation);
	public static event PoseAction OnPoseData;

	public delegate void HMDResetAction();
	public static event HMDResetAction OnResetHMD; //add your event here in case you want to do anything special on recenterting

	//currently we only support two types of connections: one to send pose data and another to send touch data via RPC
	public const int NUM_CONNECTIONS = 2;
	
	public const int SERVER_PORT = 8888;
	public const int UDP_SERVER_PORT = 1204;
	public const int REMOTE_PORT = 19784;

	//keywords that trigger events on the smart device
	private const string SEND_DATA_METHOD_STR = "SendData";
	private const string RESET_STR = "Reset";
	
	private const string POSE_MESSAGE_ID = "Pose";
	private const string TOUCH_MESSAGE_ID = "Touch";
	
	private const string TOUCH_METHOD_ID = "TouchIO";
	private const string SET_SIZE_METHOD_ID = "SetSize";

	//Used to find navi application on smart device
	private UdpClient sender;

	[HideInInspector]
	public int socketID;
	[HideInInspector]
	public int tangoPoseConnectionID = -1; //set once the devices connect to each other
	[HideInInspector]
	public int touchConnectionID = -1; //set once the devices connect to each other
	[HideInInspector]
	public int myReiliableChannelId;
	[HideInInspector]
	public int myUnreliableChannelId;
	[HideInInspector]
	public int myUnreliableFramentedChannelId;

	//Used to start game for the first time
	private bool initalReset = true;
	/// <summary>
	/// Resets the HMD and smart device. Will call OnGameStart once both the device and HMD have been reset.
	/// This method is mapped to a 5 finger tap. So whenever a user taps with 5 fingers they will be reset
	/// </summary>
	public void ResetVR() {
		//
		if (OnResetHMD != null)
			OnResetHMD (); 

		ResetDevice ();
		
		if (OnGameStart != null && initalReset) {
			StopAllCoroutines();
			initalReset = false;
			OnGameStart ();
		}
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

		CreateSocket ();
		StartSendingIP ();
	}

	/// <summary>
	/// Called every frame to process Network events.
	/// </summary>
	void Update()
	{
		int recHostId; 
		int connectionId; 
		int channelId; 
		byte[] recBuffer = new byte[1024]; 
		int bufferSize = 1024;
		int dataSize;
		byte error;
		NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
		while (recData != NetworkEventType.Nothing) { 
			//TODO add some limit so that it does not stall from this while loop i.e. 1000 loops or something
			switch (recData) {
			case NetworkEventType.Nothing:
				break;
			case NetworkEventType.ConnectEvent: //connection was made
				CancelInvoke (SEND_DATA_METHOD_STR);
				OnConnection (connectionId);
				break;
			case NetworkEventType.DataEvent: //data was received
				if (connectionId == tangoPoseConnectionID)
					HandlePoseData (recBuffer);
				else if (connectionId == touchConnectionID)
					HandleRPC (recBuffer);
				break;
			case NetworkEventType.DisconnectEvent: //device disconnected
				OnDisconnect (connectionId);
				break;
			}

			recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
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
	/// Method to parse and send an event when Pose Data is transfer from smart device
	/// </summary>
	/// <param name="recBuffer">The data that was recieved from the smart device</param>
	private void HandlePoseData(byte[] recBuffer){
		Stream stream = new MemoryStream(recBuffer);
		BinaryFormatter formatter = new BinaryFormatter();
		PoseSerializer poseData = (PoseSerializer) formatter.Deserialize(stream);

		if (OnPoseData != null) {
			OnPoseData(poseData.V3, poseData.Q);
		}
	}

	/// <summary>
	/// Method to parse and send an RPC event from smart device such as recieving touch input
	/// </summary>
	/// <param name="recBuffer">The data that was recieved from the smart device</param>
	private void HandleRPC(byte[] recBuffer){
		Stream stream = new MemoryStream(recBuffer);
		BinaryFormatter formatter = new BinaryFormatter();
		RPCSerializer rpcData = (RPCSerializer) formatter.Deserialize(stream);

		if (rpcData.methodName.Equals (TOUCH_METHOD_ID)) {
			TouchSerializer ts = (TouchSerializer)rpcData.args [0];
			TouchManager.ProcessTouch (ts);
		} else if (rpcData.methodName.Equals (SET_SIZE_METHOD_ID)) {
			TouchManager.Instance.SetServerScreenSize ((int)rpcData.args[0], (int)rpcData.args[1]);
		}

	}

	/// <summary>
	/// Method that is called when one of the connection disconnects. 
	/// </summary>
	/// <param name="connectionID">The connection id that disconnected</param>
	private void OnDisconnect(int connectionID){
		if (tangoPoseConnectionID == connectionID)
			tangoPoseConnectionID = -1;
		else if (touchConnectionID == connectionID)
			touchConnectionID = -1;

		if (tangoPoseConnectionID == -1 && touchConnectionID == -1){
			OnDeviceDisconnected();
			InvokeRepeating(SEND_DATA_METHOD_STR,0,0.5f); //send data every second
		}
	}

	/// <summary>
	/// Method that is called when a connection is made. We then send a message back to the smart device 
	/// so that the smart device knows which connection it should use to send different types of data.
	/// OnDeviceConnected is called when all connections have been made.
	/// </summary>
	/// <param name="connectionID">The id of the connection was made</param>
	private void OnConnection(int connectionID){
		string message = "";
		CancelInvoke(SEND_DATA_METHOD_STR);
		if (tangoPoseConnectionID < 0) {
			tangoPoseConnectionID = connectionID;
			message = POSE_MESSAGE_ID;
		} else if (touchConnectionID < 0) {
			touchConnectionID = connectionID;
			message = TOUCH_MESSAGE_ID;

			if (OnDeviceConnected != null)
				OnDeviceConnected();
		}

		byte error;
		byte[] buffer = new byte[1024];
		Stream stream = new MemoryStream(buffer);
		BinaryFormatter formatter = new BinaryFormatter();
		formatter.Serialize(stream, message);
		NetworkTransport.Send(socketID, connectionID, myReiliableChannelId, buffer, 1024, out error); //send full buffer
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
		myUnreliableFramentedChannelId = config.AddChannel(QosType.UnreliableFragmented);
		
		HostTopology topology = new HostTopology(config, NUM_CONNECTIONS); //only supports one other connection
		
		socketID = NetworkTransport.AddHost(topology, NaviConnectionSDK.SERVER_PORT);
	}
	
	/// <summary>
	/// Method that is called when a smart device connects
	/// </summary>
	private void DeviceConnected(){
		if (OnResetHMD != null)
			OnResetHMD ();
		GestureManager.OnFiveFingerTap += ResetVR; //enable reset at any time
	}
}