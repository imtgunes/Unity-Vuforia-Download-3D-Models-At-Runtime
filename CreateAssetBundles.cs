using UnityEditor;
using System.IO;
using UnityEngine;

public class CreateAssetBundles 
{
   [MenuItem("Assets/Build AssetBundles")]
   static void BuildAllAssetBundles()
   {
   string assetBundleDirectory = "Assets/StreamingAssets";
   if (!Directory.Exists(Application.streamingAssetsPath))
   {
   Directory.CreateDirectory(assetBundleDirectory);
   }
   
   BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);//if build android : BuildTarget.Android, ios:BuildTarget.iOS
   }
}
