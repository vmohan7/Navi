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
///  This struct is the structure of how touch data is serialized and transfered 
/// </summary>
public struct TouchSerializer
{
	public int fingerID;
	public int phase;
	public float position_x;
	public float position_y;
	public int tapCount;

	/// <summary>
	///  Convenience method to fill the data for the struct
	/// </summary>
	public void Fill(Touch t){
		fingerID = t.fingerId;
		phase = IntLookup (t.phase);
		position_x = t.position.x;
		position_y = t.position.y;
		tapCount = t.tapCount;
	}

	/// <summary>
	///  Convenience method to get a touch position vector from the struct
	/// </summary>
	public Vector2 position
	{ get { return new Vector2(position_x, position_y); } }

	/// <summary>
	///  Method to convert a touch phase into an int
	/// </summary>
	private int IntLookup(TouchPhase phase){
		if (phase == TouchPhase.Began)
			return 1;				
		
		if (phase == TouchPhase.Ended)
			return 2;			
		
		if (phase == TouchPhase.Stationary)
			return 3;
		
		if (phase == TouchPhase.Moved)
			return 4;
		
		if (phase == TouchPhase.Canceled)
			return 5;
		
		return -1;
	}
}