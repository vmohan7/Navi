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
/// This is the class that handles the network connections between the smart device and the PC.
/// It is also responsbile for updating the instructions or images shown if any.
/// There are two phases to the network connection. The first is where the mobile device listens for an ipAddress 
/// being broadcasted by the VR device. The second step is connecting to that ipAddress in order to send data wirelesssly.
/// </summary>
public class NaviMobileManager : MonoBehaviour {

	public Text instructionPanel; //the text that is displayed to the user
	public RawImage background; //an image that we can show from the app

	public static NaviMobileManager Instance; //the single instance of this class

	private UdpClient receiver; //the object that connects to the VR device
	private int SDKBuildNo = 1; //set by the NaviSDK via a Network connection once the channels are setup

	[HideInInspector]
	public int socketID;
	[HideInInspector]
	public int naviPoseConnectionID = -1; //set once the devices connect to each other
	private bool naviPoseAssigned = false;

	[HideInInspector]
	public int touchConnectionID = -1; //set once the devices connect to each other
	private bool touchConnectionAssigned = false;

	[HideInInspector]
	public int myReiliableChannelId;
	[HideInInspector]
	public int myUnreliableChannelId;
	[HideInInspector]
	public int myUnreliableFramentedChannelId;

	public const int NumConnections = 2; //number of connections the socket can handle

	public const int ServerPort = 8888;
	public const int UDPServerPort = 1204;
	public const int RemotePort = 19784;

	private const int BUFFER_SIZE = 256;

	private const string SEND_DATA_METHOD_STR = "SendData";
	private const string RESET_STR = "Reset";

	private const string POSE_MESSAGE_ID = "Pose";
	private const string TOUCH_MESSAGE_ID = "Touch";
	private const string VIDEO_MESSAGE_ID = "Video";

	private const string BUILD_MESSAGE_ID = "BuildNo";

	private const string TOUCH_METHOD_ID = "TouchIO";
	private const string SET_SIZE_METHOD_ID = "SetSize";

	private const string searchInstructions = "Searching for Navi-Compatible Application running on PC in same Local Network...";
	
	/// <summary>
	///  Initalize before the scene loads
	/// </summary>
	void Awake () {
		AudioListener.volume = 0f; //get audio from the VR device not the smart device
		
		if (Instance == null)
			Instance = this;
		
		DontDestroyOnLoad (this.gameObject);
	}

	/// <summary>
	///  Always keep screen on to keep app alive and start listening for the ipAddress
	/// </summary>
	void Start () {
		// Disable screen dimming
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		CreateSocket ();
		StartReceivingIP();

	}

	/// <summary>
	///  Handles sending all data to the VR display, this includes pose data via the naviPoseconnection and touch data via the rpcConnection
	/// It also handles receiving any data from the VR display. Currently, it just listens for which connection should send which type of data
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
		switch (recData)
		{
		case NetworkEventType.DataEvent:
			if (!naviPoseAssigned || !touchConnectionAssigned) {
				Stream stream = new MemoryStream(recBuffer);
				BinaryFormatter formatter = new BinaryFormatter();
				string message = formatter.Deserialize(stream) as string;
				AssignID(connectionId, message);
			} else {
				if (connectionId == touchConnectionID) {
					HandleRPC (recBuffer);
				}
			}

			break;
		case NetworkEventType.DisconnectEvent:
			OnDisconnect(connectionId);
			break;
		default: //i.e. NetworkEventType.ConnectEvent, NetworkEventType.Nothing:\
			break;
		}

		if (naviPoseConnectionID > 0) {
			if (SDKBuildNo == 1) {
				byte[] buffer = new byte[BUFFER_SIZE];
				Stream stream = new MemoryStream(buffer);
				BinaryFormatter formatter = new BinaryFormatter();
				PoseSerializer pose = new PoseSerializer();
				pose.Fill(TransformManagerInterface.Instance.transform.position, TransformManagerInterface.Instance.transform.rotation);
				formatter.Serialize(stream, pose);
				NetworkTransport.Send(socketID, naviPoseConnectionID, myUnreliableChannelId, buffer, BUFFER_SIZE, out error); //send full buffer
			} else {
				byte[] buffer = new byte[BUFFER_SIZE];
				Stream stream = new MemoryStream(buffer);
				BinaryFormatter formatter = new BinaryFormatter();
				PoseSerializerWithAcceleration pose = new PoseSerializerWithAcceleration();
				pose.Fill(TransformManagerInterface.Instance.transform.position, TransformManagerInterface.Instance.transform.rotation, Input.acceleration);
				formatter.Serialize(stream, pose);
				NetworkTransport.Send(socketID, naviPoseConnectionID, myUnreliableChannelId, buffer, BUFFER_SIZE, out error); //send full buffer
			}
		}


		if (touchConnectionID > 0) {
			BinaryFormatter formatter = new BinaryFormatter();
			foreach (Touch t in Input.touches){
				byte[] buffer = new byte[BUFFER_SIZE];
				Stream stream = new MemoryStream(buffer);

				RPCSerializer rpc = new RPCSerializer();
				rpc.methodName = TOUCH_METHOD_ID;
				rpc.args = new object[1];
				TouchSerializer ts = new TouchSerializer();
				ts.Fill(t);
				rpc.args[0] = ts; 
				formatter.Serialize(stream, rpc);
				NetworkTransport.Send(socketID, touchConnectionID, myReiliableChannelId, buffer, BUFFER_SIZE, out error);
			}
		}
	
	}

	/// <summary>
	/// This method handles remembering which connection shuold be used to send which type of data
	/// Once all connections have been establishsed, we also send a one time message of the size of the device we are using
	/// </summary>
	/// <param name="connectionID">ConnectionId we recieved data from</param>
	/// <param name="message">The type of data that should be sent over this connection</param>
	private void AssignID(int connectionID, string message){
		if (message.Equals (POSE_MESSAGE_ID)) {
			naviPoseConnectionID = connectionID;
			naviPoseAssigned = true;
		} else if (message.Equals (TOUCH_MESSAGE_ID)) {
			touchConnectionID = connectionID;
			touchConnectionAssigned = true;

			//Send RPC with size data
			byte error;
			BinaryFormatter formatter = new BinaryFormatter ();
			byte[] buffer = new byte[BUFFER_SIZE];
			Stream stream = new MemoryStream (buffer);
			
			RPCSerializer rpc = new RPCSerializer ();
			rpc.methodName = SET_SIZE_METHOD_ID;
			rpc.args = new object[2];
			rpc.args [0] = Screen.width; 
			rpc.args [1] = Screen.height; 
			formatter.Serialize (stream, rpc);
			NetworkTransport.Send (socketID, touchConnectionID, myReiliableChannelId, buffer, BUFFER_SIZE, out error);
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
		
		if (rpcData.methodName.Equals (BUILD_MESSAGE_ID)) {
			SDKBuildNo = (int) rpcData.args[0];
		}
		
	}


	/// <summary>
	/// This method handles when the VR display disconnects in order to restart the app and listen for a new connection
	/// </summary>
	/// <param name="connectionID">ConnectionId that disconnected</param>
	private void OnDisconnect(int connectionID){
		if (naviPoseConnectionID == connectionID) {
			naviPoseConnectionID = -1;
			naviPoseAssigned = false;
		} else if (touchConnectionID == connectionID) {
			touchConnectionID = -1;
			touchConnectionAssigned = false;
		}

		if (!naviPoseAssigned && !touchConnectionAssigned){
			SetInstruction (searchInstructions);
		}
	}

	/// <summary>
	/// Sets the text to display to the user
	/// </summary>
	private void SetInstruction(string instruction){
		if (instructionPanel != null)
			instructionPanel.text = instruction;
	}

	/// <summary>
	/// Creates a socket using the Unity Transport layer. This is a UDP connection that will support two channels of communication. 
	/// </summary>
	private void CreateSocket(){
		NetworkTransport.Init();
		ConnectionConfig config = new ConnectionConfig();
		
		myReiliableChannelId  = config.AddChannel(QosType.Reliable);
		myUnreliableChannelId = config.AddChannel(QosType.Unreliable);
		myUnreliableFramentedChannelId = config.AddChannel(QosType.UnreliableFragmented);
		
		HostTopology topology = new HostTopology(config, NumConnections);
		
		socketID = NetworkTransport.AddHost(topology, NaviMobileManager.ServerPort);
	}

	/// <summary>
	/// This method begins the process of listening to all messages sent on the router.
	/// </summary>
	public void StartReceivingIP ()
	{
		SetInstruction (searchInstructions);
		try {
			if (receiver == null) {
				receiver = new UdpClient (NaviMobileManager.RemotePort);
				receiver.BeginReceive (new AsyncCallback (ReceiveData), null);
			}
		} catch (SocketException e) {
			Debug.Log (e.Message);
		}
	}

	/// <summary>
	/// This method continously listens for a reset command or a possible ipAddress to connect to
	/// </summary>
	/// <param name="result">The data that was recieved from the network</param>
	private void ReceiveData (IAsyncResult result)
	{
		IPEndPoint receiveIPGroup = new IPEndPoint (IPAddress.Any, NaviMobileManager.RemotePort);
		byte[] received;
		if (receiver != null) {
			received = receiver.EndReceive (result, ref receiveIPGroup);
		} else {
			return;
		}
		
		string message = Encoding.ASCII.GetString (received);
		if (message.Equals (RESET_STR)) {
			SetInstruction ("");
			if (TransformManagerInterface.Instance != null)
				TransformManagerInterface.Instance.Reset ();
		} else {
			//assume that the message is an ipAddress
			byte error;
			if (naviPoseConnectionID < 0 && touchConnectionID < 0) {
				//connect twice; once for sending pose data and once for sending RPC touch data
				//temp assign in case we recieve data before the validation from server
				naviPoseConnectionID = NetworkTransport.Connect(socketID, message, NaviMobileManager.ServerPort, 0, out error); 
				touchConnectionID= NetworkTransport.Connect(socketID, message, NaviMobileManager.ServerPort, 0, out error);
				SetInstruction ("");
			}
		}
		
		//start listening again
		receiver.BeginReceive (new AsyncCallback (ReceiveData), null);
	}
	
}
