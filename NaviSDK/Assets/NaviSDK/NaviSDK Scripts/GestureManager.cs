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
using System.Collections.Generic;

using PDollarGestureRecognizer;

/// <summary>
///  This class manages handling various types of gestures.
///  Right now this only supports detecting if multiple fingers are on the screen 
///  but can be extended to more complicated gestures
/// </summary>
public class GestureManager : MonoBehaviour {

	public delegate void MultiFingerAction(int playerID);
	public static event MultiFingerAction OnThreeFingerTap;
	public static event MultiFingerAction OnFiveFingerTap;
	public static event MultiFingerAction OnSixFingerTap;

	//Swipe detection events & variables
	public delegate void SwipeAction(int playerID);
	public static event SwipeAction SwipeLeft;
	public static event SwipeAction SwipeRight;
	public static event SwipeAction SwipeUp;
	public static event SwipeAction SwipeDown;

	//Swipe detection events & variables
	public delegate void ComplexGesture(int playerID, string gestureClass);
	public static event ComplexGesture OnComplexGesture;
	
	private const int MAX_STATIONARY_FRAMES = 6; //maximum number of stay events for a swipe
	private const int MIN_SWIPE_DIST = 300; //distance for it to be considered a swipe
	private const int MAX_SWIPE_TIME = 10; //number of seconds before it is not a swipe anymores
	
	private Gesture[] trainingSet;
	
	private class GestureData{
		//variable to keep track of number of fingers on screen
		public int numFingersDown = 0;

		public bool couldBeSwipe = false; //determine if it is a swipe
		public Vector2 swipeStartPos = Vector2.zero; //start of swipe
		public int stationaryForFrames = 0; //number of stationary frames in swipe
		public float swipeStartTime = 0f; //time swipe started

		public List<Point> points = new List<Point> ();
	}

	//keeps track of state for each player
	private Dictionary<int, GestureData> playerGestureState = new Dictionary<int, GestureData>();

	/// <summary>
	/// First function that is called when scene is loading
	/// </summary>
	void Awake() {
		DontDestroyOnLoad (this.gameObject);
	}

	/// <summary>
	/// Init listenting for events
	/// </summary>
	void Start () {
		TouchManager.OnTouchDown += HandleOnTouchDown;
		TouchManager.OnTouchUp += HandleOnTouchUp;

		TouchManager.OnTouchStayed += HandleOnTouchStay;

		TouchManager.OnTouchMove += HandleOnTouchMove;

		NaviConnectionSDK.OnDeviceConnected += HandleOnDeviceConnected;
		NaviConnectionSDK.OnDeviceDisconnected += HandleOnDeviceDisconnected;

		StartCoroutine (InitalizeGestures ());
	}

	/// <summary>
	/// Remove events when object is deleted i.e. game ends
	/// </summary>
	void OnDestroy(){
		TouchManager.OnTouchDown -= HandleOnTouchDown;
		TouchManager.OnTouchUp -= HandleOnTouchUp;

		TouchManager.OnTouchStayed -= HandleOnTouchStay;

		TouchManager.OnTouchMove -= HandleOnTouchMove;

	}

	/// <summary>
	/// Handles when a player connects to the game and we want to initalize their data
	/// </summary>
	private void HandleOnDeviceConnected (int playerID)
	{
		playerGestureState.Add (playerID, new GestureData ());
	}

	/// <summary>
	/// Handles when player leaves and we need to clean up their data
	/// </summary>
	private void HandleOnDeviceDisconnected (int playerID)
	{
		playerGestureState.Remove (playerID);
	}
	
	/// <summary>
	/// Callback for when we receive a touch
	/// </summary>
	private void HandleOnTouchDown (int playerID, int fingerID, Vector2 pos)
	{
		GestureData gs = playerGestureState [playerID];
		if (gs.numFingersDown > 15)
			gs.numFingersDown = 0; //something went wrong

		gs.numFingersDown++;

		if (gs.numFingersDown == 3 && OnThreeFingerTap != null)
			OnThreeFingerTap (playerID);
		else if (gs.numFingersDown == 5 && OnFiveFingerTap != null)
			OnFiveFingerTap (playerID);
		else if (gs.numFingersDown == 6 && OnSixFingerTap != null)
			OnSixFingerTap (playerID);

		//for swipes
		gs.couldBeSwipe = true;
		gs.swipeStartPos = pos;  //Position where the touch started
		gs.swipeStartTime = Time.time; //The time it started
		gs.stationaryForFrames = 0;

		//for generic gestures
		gs.points.Add (new Point (pos.x, pos.y, fingerID));

		playerGestureState [playerID] = gs;
	}

	/// <summary>
	/// Callback for when a touch ends
	/// </summary>
	private void HandleOnTouchUp (int playerID, int fingerID, Vector2 pos)
	{
		GestureData gs = playerGestureState [playerID];
		gs.numFingersDown--;
		
		if (gs.numFingersDown < 0)
			gs.numFingersDown = 0; //something went wrong
		
		float swipeTime = Time.time - gs.swipeStartTime; //Time the touch stayed at the screen till now.
		if (gs.couldBeSwipe && swipeTime < MAX_SWIPE_TIME) {
			float xSwipeDist = pos.x - gs.swipeStartPos.x; //X Swipe distance
			float ySwipeDist = pos.y - gs.swipeStartPos.y; //Y Swipe distance
			
			if (Mathf.Abs(xSwipeDist) > MIN_SWIPE_DIST) { //only one swipe allowed at a time
				if (xSwipeDist < 0 && SwipeLeft != null)
					SwipeLeft(playerID);
				else if (xSwipeDist >= 0 && SwipeRight != null)
					SwipeRight(playerID);
			}
			else if (Mathf.Abs(ySwipeDist) > MIN_SWIPE_DIST) {
				if (ySwipeDist < 0 && SwipeDown != null)
					SwipeDown(playerID);
				else if (ySwipeDist >= 0 && SwipeUp != null)
					SwipeUp(playerID);
			}
		}

		if (gs.numFingersDown == 0 && gs.points.Count >= 2) {
			gs.points.Add (new Point (pos.x, pos.y, fingerID));
			Gesture candidate = new Gesture(gs.points.ToArray());  
			string gestureClass = PointCloudRecognizer.Classify(candidate, trainingSet); 
			if (OnComplexGesture != null){
				OnComplexGesture(playerID, gestureClass);
			}
			gs.points.Clear();
		}

		playerGestureState [playerID] = gs;
	}

	/// <summary>
	/// Callback for when a touch stays in the same position
	/// </summary>
	private void HandleOnTouchStay (int playerID, int fingerID, Vector2 pos)
	{
		GestureData gs = playerGestureState [playerID];
		gs.stationaryForFrames++;
		if (gs.couldBeSwipe && gs.stationaryForFrames > MAX_STATIONARY_FRAMES) {
			gs.couldBeSwipe = false;
		}

		playerGestureState [playerID] = gs;
	}

	/// <summary>
	/// Callback for when a finger moves on the screen
	/// </summary>
	private void HandleOnTouchMove (int playerID, int fingerID, Vector2 pos)
	{
		playerGestureState[playerID].points.Add (new Point (pos.x, pos.y, fingerID));
	}

	/// <summary>
	/// Loads all gestures stored in the resources folder in a semi-threaded manner
	/// </summary>
	private IEnumerator InitalizeGestures(){
		Object[] assets = Resources.LoadAll("Gestures");
		trainingSet = new Gesture[assets.Length];
		for (int i = 0; i < assets.Length; i++) {
			trainingSet[i] = PDollarDemo.GestureIO.ReadXMLGesture(((TextAsset)assets[i]).text);
			yield return new WaitForEndOfFrame(); //wait in case there are a lot of files to load 
		}
	}

}