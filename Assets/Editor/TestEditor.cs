using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;
public class TestEditor : Editor
{
    [MenuItem("Tools/MyCustomMenu/ClickMe")]
    public static void TestClick()
    {
        string[] testLoadPath =
        {
            "Assets/Texture/Default",
            "Assets/Texture/NormalMap",
            "Assets/Texture/SingleChannel",
            "Assets/Texture/Sprite",
            "Assets/Texture/Translucent",
            "Assets/Audio/HighFrequencyClip",
            "Assets/Audio/LargeFileClip",
            "Assets/Audio/LowFrequencyClip"
        };

        Enum[] testEnum =
        {
            AssetEnum.TextureType.Default,
            AssetEnum.TextureType.NormalMap,
            AssetEnum.TextureType.SingleChannel,
            AssetEnum.TextureType.Sprite,
            AssetEnum.TextureType.Translucent,
            AssetEnum.AudioType.HighFrequencyClip, 
            AssetEnum.AudioType.LowFrequencyClip, 
            AssetEnum.AudioType.LargeFileClip, 
        };
         
        ImportAssetInfo info = new ImportAssetInfo(testLoadPath,testEnum);
        AssetImportTool.ImportAssetsAndSetUp(info);
    }
}
