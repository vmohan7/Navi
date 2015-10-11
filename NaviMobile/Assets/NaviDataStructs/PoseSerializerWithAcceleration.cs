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
using System;

[Serializable]
/// <summary>
///  This struct is the structure of how pose data is serialized and transfered 
///  from the navi application on the smart device.
/// </summary>
public struct PoseSerializerWithAcceleration
{
	//positional data, which is sent from devices that support it like Project Tango Dev Kit
	public float pos_x;
	public float pos_y;
	public float pos_z;

	//rotational data, sent mainly from device gyro
	public float quat_x;
	public float quat_y;
	public float quat_z;
	public float quat_w;

	//acceleration data, sent from device accelerometer
	public float accel_x;
	public float accel_y;
	public float accel_z;

	/// <summary>
	///  Convenience method to fill the data for the struct
	/// </summary>
	public void Fill(Vector3 v3, Quaternion q, Vector3 acceleration)
	{
		pos_x = v3.x;
		pos_y = v3.y;
		pos_z = v3.z;

		quat_x = q.x;
		quat_y = q.y;
		quat_z = q.z;
		quat_w = q.w;

		accel_x = acceleration.x;
		accel_y = acceleration.y;
		accel_z = acceleration.z;
	}

	/// <summary>
	///  Convenience method to get a positional vector from the struct
	/// </summary>
	public Vector3 Position
	{ get { return new Vector3(pos_x, pos_y, pos_z); } }

	/// <summary>
	///  Convenience method to get rotation quaternion from the struct
	/// </summary>
	public Quaternion Rotation
	{ get { return new Quaternion(quat_x, quat_y, quat_z, quat_w); } }

	/// <summary>
	///  Convenience method to get acceleration from the struct
	/// </summary>
	public Vector3 Acceleration
	{ get { return new Vector3(accel_x, accel_y, accel_z); } }
}