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
///  This class is responsible for determining the platform we are currently on
///  and choosing which manager to create in order to send data that platform supports
/// </summary>
public class DeviceManager : MonoBehaviour {

	/// <summary>
	/// Chooses manager on initalization. The only difference right now, is whether the device is a Tango or not.
	/// NOTE: These managers must inherit from TransformManagerInterface so that the proper Instance variable is accessed
	/// </summary>
	void Awake () {
		if (AndroidHelper.IsTangoCorePresent ()) {
			this.gameObject.AddComponent<PoseManager> ();
		} else {
			this.gameObject.AddComponent<GyroManager> ();
		}

		this.enabled = false; //this script is no longer needed so we can disable ourselves
	}
}
