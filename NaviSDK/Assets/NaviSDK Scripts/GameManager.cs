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
///  Script that listens for OnGameStart and then starts the appropriate scene
///  This script can be modified to do custom setup when game starts.
/// </summary>
public class GameManager : MonoBehaviour {

	public static GameManager Instance;

	/// <summary>
	/// First function that is called when scene is loading
	/// </summary>
	void Awake () {
		if (Instance == null)
			Instance = this;
	}

	/// <summary>
	/// Set up callbacks
	/// </summary>
	void Start () {
		NaviConnectionSDK.OnGameStart += OnGameStart;
	}

	/// <summary>
	/// Remove callback when game object is destroyed
	/// </summary>
	void OnDestroy(){
		NaviConnectionSDK.OnGameStart -= OnGameStart;
	}

	/// <summary>
	/// Callback for when game starts; loads the next scene along with the SDK that cannot be destroyed
	/// </summary>
	private void OnGameStart(){
		Application.LoadLevel (1); //load the test scene
	}
}
