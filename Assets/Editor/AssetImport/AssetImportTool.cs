using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

public class AssetImportTool
{
    /// <summary>
    /// 导入资源进行各自参数设置
    /// </summary>
    public static void ImportAssetsAndSetUp(ImportAssetInfo importAssetInfo)
    {
        for (int i = 0; i < importAssetInfo.LoadPath.Length; i++)
        {
            SetLoadPath(importAssetInfo.EnumType[i],importAssetInfo.LoadPath[i]);
        }
        AssetGraphUtility.ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, ImportAssetConfig.TEXTURE_GRAPH_PATH);
    }
    /// <summary>
    /// 设置加载路径
    /// </summary>
    private static void SetLoadPath(Enum enumType,string loadPath)
    {
        string graphPath = string.Empty;
        if (enumType.GetType() == typeof(AssetEnum.TextureType))
        {
            graphPath = ImportAssetConfig.TEXTURE_GRAPH_PATH;
        }
        else if (enumType.GetType() == typeof(AssetEnum.AudioType))
        {
            graphPath = ImportAssetConfig.AUDIO_GRAPH_PATH;
        }

        var graph = AssetDatabase.LoadAssetAtPath<ConfigGraph>(graphPath);
        if (graph == null)
        {
            Debug.LogError("未找到 AssetGraph 文件: " + graphPath);
            return;
        }

        foreach (var node in graph.Nodes)
        {
            if (string.Equals("Load"+enumType,node.Name))
            {
                var loader = node.Operation.Object as UnityEngine.AssetGraph.Loader;
                loader.LoadPath = loadPath;
                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
