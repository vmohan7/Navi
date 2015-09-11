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
///  This class manages handling various types of gestures.
///  Right now this only supports detecting if multiple fingers are on the screen 
///  but can be extended to more complicated gestures
/// </summary>
public class GestureManager : MonoBehaviour {

	//variable to keep track of number of fingers on screen
	private int numFingersDown = 0;

	public delegate void MultiFingerAction();
	public static event MultiFingerAction OnThreeFingerTap;
	public static event MultiFingerAction OnFiveFingerTap;
	public static event MultiFingerAction OnSixFingerTap;

	//Swipe detection events & variables
	public delegate void SwipeAction();
	public static event SwipeAction SwipeLeft;
	public static event SwipeAction SwipeRight;
	public static event SwipeAction SwipeUp;
	public static event SwipeAction SwipeDown;
	
	private const int MAX_STATIONARY_FRAMES = 6; //maximum number of stay events for a swipe
	private const int MIN_SWIPE_DIST = 300; //distance for it to be considered a swipe
	private const int MAX_SWIPE_TIME = 10; //number of seconds before it is not a swipe anymores
	
	private bool couldBeSwipe = false; //determine if it is a swipe
	private Vector2 swipeStartPos = Vector2.zero; //start of swipe
	private int stationaryForFrames = 0; //number of stationary frames in swipe
	private float swipeStartTime = 0f; //time swipe started


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
	}

	/// <summary>
	/// Remove events when object is deleted i.e. game ends
	/// </summary>
	void OnDestroy(){
		TouchManager.OnTouchDown -= HandleOnTouchDown;
		TouchManager.OnTouchUp -= HandleOnTouchUp;

		TouchManager.OnTouchStayed -= HandleOnTouchStay;

	}

	/// <summary>
	/// Called every frame and dispatches events when a gesture is performed
	/// </summary>
	void Update(){
		if (numFingersDown == 3 && OnThreeFingerTap != null)
			OnThreeFingerTap ();

		if (numFingersDown == 5 && OnFiveFingerTap != null)
			OnFiveFingerTap ();

		if (numFingersDown == 6 && OnSixFingerTap != null)
			OnSixFingerTap ();

	}

	/// <summary>
	/// Callback for when we receive a touch
	/// </summary>
	private void HandleOnTouchDown (int fingerID, Vector2 pos)
	{
		if (numFingersDown > 15)
			numFingersDown = 0; //something went wrong

		numFingersDown++;

		//for swipes
		couldBeSwipe = true;
		swipeStartPos = pos;  //Position where the touch started
		swipeStartTime = Time.time; //The time it started
		stationaryForFrames = 0;
	}
	
	/// <summary>
	/// Callback for when a touch ends
	/// </summary>
	private void HandleOnTouchUp (int fingerID, Vector2 pos)
	{
		numFingersDown--;
		
		if (numFingersDown < 0)
			numFingersDown = 0; //something went wrong
		
		float swipeTime = Time.time - swipeStartTime; //Time the touch stayed at the screen till now.
		if (couldBeSwipe && swipeTime < MAX_SWIPE_TIME) {
			float xSwipeDist = pos.x - swipeStartPos.x; //X Swipe distance
			float ySwipeDist = pos.y - swipeStartPos.y; //Y Swipe distance
			
			if (Mathf.Abs(xSwipeDist) > MIN_SWIPE_DIST) { //only one swipe allowed at a time
				if (xSwipeDist < 0 && SwipeLeft != null)
					SwipeLeft();
				else if (xSwipeDist >= 0 && SwipeRight != null)
					SwipeRight();
			}
			else if (Mathf.Abs(ySwipeDist) > MIN_SWIPE_DIST) {
				if (ySwipeDist < 0 && SwipeDown != null)
					SwipeDown();
				else if (ySwipeDist >= 0 && SwipeUp != null)
					SwipeUp();
			}
		}
	}

	/// <summary>
	/// Callback for when a touch stays in the same position
	/// </summary>
	private void HandleOnTouchStay (int fingerID, Vector2 pos)
	{
		stationaryForFrames++;
		if (couldBeSwipe && stationaryForFrames > MAX_STATIONARY_FRAMES) {
			couldBeSwipe = false;
		}
	}

}