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
using UnityEditor;
using System.IO;


public class CreateAssetBundles
{
	private const string OUTPUT_PATH = "Assets/NaviSDK/AssetBundleOutput";

	[MenuItem ("NaviSDK/Build AssetBundles")]
	static void BuildAllAssetBundles ()
	{
		DirectoryInfo info = new DirectoryInfo (OUTPUT_PATH);
		FileInfo[] files = info.GetFiles ();
		string oldPath = OUTPUT_PATH + "\\Old" + System.DateTime.Today.ToFileTime() + "\\";
		Directory.CreateDirectory(oldPath);
		foreach (FileInfo f in files) {
			FileUtil.MoveFileOrDirectory (f.FullName, oldPath + f.Name);
		}


		BuildPipeline.BuildAssetBundles ("Assets/NaviSDK/AssetBundleOutput", BuildAssetBundleOptions.None, BuildTarget.Android | BuildTarget.iOS | BuildTarget.StandaloneWindows);
		info = new DirectoryInfo (OUTPUT_PATH);
		files = info.GetFiles ();
		foreach (FileInfo f in files) {
			if (f.Name.Contains (".manifest") || f.Name.Contains ("AssetBundle") || f.Name.Contains ("meta")) {
				FileUtil.DeleteFileOrDirectory (f.FullName);
			} else {
				FileUtil.MoveFileOrDirectory (f.FullName, f.FullName + ".txt");
			}
		}
	}
}