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
///  This class manages handling touch data sent over the network
///  and dispatches events that can be registered by the game.
///  It also contains the devices width and height in case 
///  the game wants to use that information to calculate tapping on the right vs 
///  taping on the left of the device.
///  There can only be one of these classes active at a given time and it can be accessed via
///  the Instance variable.
/// </summary>
public class TouchManager : MonoBehaviour {

	//The different types of events that can be sent out
	public delegate void TouchEvent(int fingerID, Vector2 pos);
	public static event TouchEvent OnTouchDown;
	public static event TouchEvent OnTouchUp;
	public static event TouchEvent OnTouchStayed;
	public static event TouchEvent OnTouchMove;
	public static event TouchEvent OnTouchCanceled;

	//special events based on the number of taps
	public static event TouchEvent OnDoubleTap;
	public static event TouchEvent OnTripleTap;

	[HideInInspector]
	public int DeviceWidth;
	[HideInInspector]
	public int DeviceHeight; 

	public static TouchManager Instance;
	
	/// <summary>
	/// First function that is called when scene is loading
	/// </summary>
	void Awake() {
		DontDestroyOnLoad (this.gameObject);
		
		if (Instance == null)
			Instance = this;
	}
	
	/// <summary>
	/// A lookup for converting the touch phase id that was seralized into an event
	/// </summary>
	/// <param name="phase">The id that we want to convert into an event</param>
	private static TouchEvent TouchPhaseLookup(int phase){
		switch (phase) {
		case 1:
			return OnTouchDown;
		case 2:
			return OnTouchUp;	
		case 3:
			return OnTouchStayed;
		case 4:
			return OnTouchMove;
		case 5:
			return OnTouchCanceled;
		}
		
		return null;
	}

	/// <summary>
	/// A static method that takes the network data and dispatches events based on what was inputted on the smart device
	/// </summary>
	/// <param name="ts">The data from the smart device, which has been parsed into a TouchSerializer already</param>
	public static void ProcessTouch(TouchSerializer ts){
		TouchEvent touchType = TouchPhaseLookup(ts.phase);
		if (touchType != null) {
			touchType(ts.fingerID, ts.position);
		}
		
		if (OnDoubleTap != null && ts.tapCount == 2 && touchType == OnTouchUp) {
			OnDoubleTap (ts.fingerID, ts.position);
		}
		
		if (OnTripleTap != null && ts.tapCount == 3 && touchType == OnTouchUp) {
			OnTripleTap (ts.fingerID, ts.position);
		}
	}

	/// <summary>
	/// Stores the smart device's width and height in this instance to be accessed by the game
	/// </summary>
	/// <param name="width">The width of the smart device</param>
	/// <param name="height">The height of the smart device</param>
	public void SetServerScreenSize(int width, int height){
		DeviceWidth = width;
		DeviceHeight = height;
	}
}