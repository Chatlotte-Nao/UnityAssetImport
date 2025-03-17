using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

[CustomNode("Custom/MyNode", 1000)]
public class MyNode : Node {

	[SerializeField] private SerializableMultiTargetString m_myValue;

	public override string ActiveStyle {
		get {
			return "node 8 on";
		}
	}

	public override string InactiveStyle {
		get {
			return "node 8";
		}
	}

	public override string Category {
		get {
			return "Custom";
		}
	}

	public override void Initialize(Model.NodeData data) {
		m_myValue = new SerializableMultiTargetString();
		data.AddDefaultInputPoint();
		data.AddDefaultOutputPoint();
	}

	public override Node Clone(Model.NodeData newData) {
		var newNode = new MyNode();
		newNode.m_myValue = new SerializableMultiTargetString(m_myValue);
		newData.AddDefaultInputPoint();
		newData.AddDefaultOutputPoint();
		return newNode;
	}

	public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

		EditorGUILayout.HelpBox("My Custom Node: Implement your own Inspector.", MessageType.Info);
		editor.UpdateNodeName(node);

		GUILayout.Space(10f);

		//Show target configuration tab
		editor.DrawPlatformSelector(node);
		using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
			// Draw Platform selector tab. 
			var disabledScope = editor.DrawOverrideTargetToggle(node, m_myValue.ContainsValueOf(editor.CurrentEditingGroup), (bool b) => {
				using(new RecordUndoScope("Remove Target Platform Settings", node, true)) {
					if(b) {
						m_myValue[editor.CurrentEditingGroup] = m_myValue.DefaultValue;
					} else {
						m_myValue.Remove(editor.CurrentEditingGroup);
					}
					onValueChanged();
				}
			});

			// Draw tab contents
			using (disabledScope) {
				var val = m_myValue[editor.CurrentEditingGroup];

				var newValue = EditorGUILayout.TextField("My Value:", val);
				if (newValue != val) {
					using(new RecordUndoScope("My Value Changed", node, true)){
						m_myValue[editor.CurrentEditingGroup] = newValue;
						onValueChanged();
					}
				}
			}
		}
	}

	/**
	 * Prepare is called whenever graph needs update. 
	 */
	public override void Prepare(BuildTarget target, Model.NodeData node,
		IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<Model.ConnectionData> connectionsToOutput,
		PerformGraph.Output Output)
	{
		if (Output != null)
		{
			var destination = (connectionsToOutput == null || !connectionsToOutput.Any())
				? null
				: connectionsToOutput.First();

			var outputGroups = new Dictionary<string, List<AssetReference>>();

			// if (incoming != null)
			// {
			// 	foreach (var ag in incoming)
			// 	{
			// 		foreach (var group in ag.assetGroups)
			// 		{
			// 			foreach (var asset in group.Value)
			// 			{
			// 				if (asset.assetType == typeof(Texture2D))
			// 				{
			// 					string path = asset.importFrom;
			// 					TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
			//
			// 					if (importer != null)
			// 					{
			// 						// 设定纹理类型
			// 						importer.textureType = TextureImporterType.Default;
			// 						if (path.Contains("normal")) importer.textureType = TextureImporterType.NormalMap;
			// 						if (path.Contains("UI") || path.Contains("sprite"))
			// 							importer.textureType = TextureImporterType.Sprite;
			//
			// 						// 设置颜色空间
			// 						importer.sRGBTexture = !path.Contains("metallic");
			//
			// 						// 关闭 Read/Write
			// 						importer.isReadable = false;
			//
			// 						// 过滤模式
			// 						importer.filterMode = FilterMode.Bilinear;
			//
			// 						// 纹理压缩
			// 						importer.textureCompression = path.Contains("UI")
			// 							? TextureImporterCompression.Uncompressed
			// 							: TextureImporterCompression.Compressed;
			// 						
			// 						importer.SaveAndReimport();
			// 					}
			// 				}
			//
			// 				if (!outputGroups.ContainsKey(group.Key))
			// 				{
			// 					outputGroups[group.Key] = new List<AssetReference>();
			// 				}
			//
			// 				outputGroups[group.Key].Add(asset);
			// 			}
			// 		}
			// 	}
			// }
			//
			// Output(destination, outputGroups);
			
			if (incoming == null) return;

			foreach (var ag in incoming)
			{
				foreach (var kvp in ag.assetGroups)
				{
					foreach (var asset in kvp.Value)
					{
						var path = asset.importFrom;
						var importer = AssetImporter.GetAtPath(path) as TextureImporter;

						if (importer != null)
						{
							TextureImporterSettings settings = new TextureImporterSettings();
							importer.ReadTextureSettings(settings);

							// 获取纹理尺寸
							int maxWidth = importer.maxTextureSize;
							int maxHeight = importer.maxTextureSize;

							// 检查是否是透明纹理
							bool hasAlpha = settings.alphaIsTransparency || importer.DoesSourceTextureHaveAlpha();

							// 设定尺寸阈值（可调整）
							int thresholdSize = 2048;

							if ((maxWidth > thresholdSize || maxHeight > thresholdSize) && hasAlpha)
							{
								Debug.LogWarning($"⚠️ 纹理过大并且包含透明度: {path} (Size: {maxWidth}x{maxHeight})");
							}
						}
					}
				}
			}

			// 继续传递数据
			if (Output != null)
			{
				foreach (var ag in incoming)
				{
					Output(connectionsToOutput?.FirstOrDefault(), ag.assetGroups);
				}
			}
		}
	}


	/**
	 * Build is called when Unity builds assets with AssetBundle Graph. 
	 */ 
	public override void Build (BuildTarget target, 
		Model.NodeData nodeData, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output outputFunc,
		Action<Model.NodeData, string, float> progressFunc)
	{
		// Do nothing
	}
}
