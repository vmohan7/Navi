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

	//please refresh the unity editor once the build is created so that the file names get updated
	[MenuItem ("NaviSDK/Build AssetBundles")]
	static void BuildAllAssetBundles ()
	{

		/* code to move asset bundles to old folder; buggy
		DirectoryInfo info = new DirectoryInfo (OUTPUT_PATH);
		FileInfo[] files = info.GetFiles ();
		string oldPath = OUTPUT_PATH + "\\Old" + System.DateTime.Today.ToFileTime() + "\\";
		Directory.CreateDirectory(oldPath);
		foreach (FileInfo f in files) {
			if ((System.IO.File.GetAttributes (f.FullName) & FileAttributes.Directory) != FileAttributes.Directory) {
				Directory.CreateDirectory (oldPath);
				FileUtil.MoveFileOrDirectory (f.FullName, oldPath + f.Name);
			}
		}
		*/

		AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles ("Assets/NaviSDK/AssetBundleOutput/", BuildAssetBundleOptions.None, BuildTarget.Android);
		//Debug.Log ( manifest.GetAllAssetBundles()[0] );

		//removing all the junk files created by the asset bundle
		DirectoryInfo info = new DirectoryInfo (OUTPUT_PATH);
		FileInfo[] files = info.GetFiles ();
		foreach (FileInfo f in files) {
			if (f.Name.Contains (".manifest") || f.Name.Contains ("AssetBundle") || f.Name.Contains ("meta")) {
				FileUtil.DeleteFileOrDirectory (f.FullName);
			} else if (!f.Name.Contains (".txt")) {
				FileUtil.MoveFileOrDirectory (f.FullName, f.FullName + "_" + System.DateTime.Now.ToFileTime() + "_Android.txt");
			}
		}

		manifest = BuildPipeline.BuildAssetBundles ("Assets/NaviSDK/AssetBundleOutput/", BuildAssetBundleOptions.None, BuildTarget.iOS);
		info = new DirectoryInfo (OUTPUT_PATH);
		files = info.GetFiles ();
		foreach (FileInfo f in files) {
			if (f.Name.Contains (".manifest") || f.Name.Contains ("AssetBundle") || f.Name.Contains ("meta")) {
				FileUtil.DeleteFileOrDirectory (f.FullName);
			} else if (!f.Name.Contains (".txt")) {
				FileUtil.MoveFileOrDirectory (f.FullName, f.FullName + "_" + System.DateTime.Now.ToFileTime() + "_iOS.txt");
			}
		}

	}
}