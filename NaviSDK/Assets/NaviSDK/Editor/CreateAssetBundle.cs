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