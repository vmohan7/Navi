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
///  This class is the base interface needed to handle positional input from the mobile device
///  The reset and GUI interface allow for external control of reseting the virtual position of the device
/// </summary>
public abstract class TransformManagerInterface : MonoBehaviour {
	
	public static TransformManagerInterface Instance; //the generic instance of the manager, whether it be with out without positional tracking

	/// <summary>
	///  Initialize generic Instance
	/// </summary>
	protected void Awake () {
		if (Instance == null)
			Instance = this;
	}

	/// <summary>
	///  Nullify instance when this object is destroyed
	/// </summary>
	protected void OnDestroy(){
		Instance = null;
	}

	/// <summary>
	///  Reset will be handled different depending on the subclass
	/// </summary>
	public abstract void Reset();

	/// <summary>
	///  Displays a small reset button in the top right for users to hard reset the device
	/// </summary>
	void OnGUI() {
		//TODO: Consider removing this
		if (GUI.Button (new Rect (Screen.width - 200, 50, 150, 80), "Reset Position")) {
			Reset ();
		}
		
	}
}
