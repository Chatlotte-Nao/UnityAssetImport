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
            "Assets/Texture/Translucent"
        };

        Enum[] testEnum =
        {
            TextureType.Default,
            TextureType.NormalMap,
            TextureType.SingleChannel,
            TextureType.Sprite,
            TextureType.Translucent
        };
         
        ImportAssetInfo info = new ImportAssetInfo(testLoadPath,testEnum);
        AssetImportTool.ImportAssetsAndSetUp(info);
    }
}
