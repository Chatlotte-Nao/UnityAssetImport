using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

public class AssetImportTool
{
    private static readonly HashSet<string> s_executeGraph = new HashSet<string>();
    private static readonly Dictionary<string, ConfigGraph> s_graphCache = new Dictionary<string, ConfigGraph>();

    /// <summary>
    /// 导入资源并设置参数
    /// </summary>
    public static void ImportAssetsAndSetUp(ImportAssetInfo importAssetInfo)
    {
        if (importAssetInfo == null || importAssetInfo.LoadPath == null || importAssetInfo.EnumType == null)
        {
            Debug.LogError("ImportAssetInfo 为空或无效");
            return;
        }

        s_executeGraph.Clear();
        s_graphCache.Clear();

        //遍历 LoadPath，批量设置 LoadPath
        for (int i = 0; i < importAssetInfo.LoadPath.Length; i++)
        {
            SetLoadPath(importAssetInfo.EnumType[i], importAssetInfo.LoadPath[i]);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //执行所有已记录的 Graph
        var graphGuids = s_executeGraph.Select(AssetDatabase.AssetPathToGUID).ToList();
        AssetGraphUtility.ExecuteAllGraphs(graphGuids);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 设置加载路径
    /// </summary>
    private static void SetLoadPath(Enum enumType, string loadPath)
    {
        if (enumType == null || string.IsNullOrEmpty(loadPath)) return;

        string graphPath = enumType switch
        {
            AssetEnum.TextureType => ImportAssetConfig.TEXTURE_GRAPH_PATH,
            AssetEnum.AudioType => ImportAssetConfig.AUDIO_GRAPH_PATH,
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(graphPath)) return;

        //记录 GraphPath 以便执行
        s_executeGraph.Add(graphPath);

        //从缓存获取 ConfigGraph，避免重复加载
        if (!s_graphCache.TryGetValue(graphPath, out var graph))
        {
            graph = AssetDatabase.LoadAssetAtPath<ConfigGraph>(graphPath);
            if (graph == null)
            {
                Debug.LogError($"未找到 AssetGraph 文件: {graphPath}");
                return;
            }
            s_graphCache[graphPath] = graph;
        }

        //查找对应的 Node 并设置 LoadPath
        foreach (var node in graph.Nodes)
        {
            if (node.Name.Equals($"Load{enumType}"))
            {
                if (node.Operation.Object is UnityEngine.AssetGraph.Loader loader)
                {
                    loader.LoadPath = loadPath;
                }
                break;
            }
        }
    }
}

