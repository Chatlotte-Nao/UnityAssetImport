using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

[CustomNode("Custom/Texture/TranslucentTex/TranslucentTexRuleNode", 1000)]
public class TranslucentTexRuleNode : Node {
	[SerializeField] private SerializableMultiTargetString m_maxTexSize;
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
		m_maxTexSize = new SerializableMultiTargetString();
		data.AddDefaultInputPoint();
		data.AddDefaultOutputPoint();
	}

	public override Node Clone(Model.NodeData newData) {
		var newNode = new TranslucentTexRuleNode();
		newNode.m_maxTexSize = new SerializableMultiTargetString(m_maxTexSize);
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
			var disabledScope = editor.DrawOverrideTargetToggle(node, m_maxTexSize.ContainsValueOf(editor.CurrentEditingGroup), (bool b) => {
				using(new RecordUndoScope("Remove Target Platform Settings", node, true)) {
					if(b) {
						m_maxTexSize[editor.CurrentEditingGroup] = m_maxTexSize.DefaultValue;
					} else {
						m_maxTexSize.Remove(editor.CurrentEditingGroup);
					}
					onValueChanged();
				}
			});

			// Draw tab contents
			using (disabledScope) {
				var val = m_maxTexSize[editor.CurrentEditingGroup];
				var newValue = EditorGUILayout.TextField("MaxTexSize:", val);
				if (newValue != val) {
					using(new RecordUndoScope("MaxTexSize Changed", node, true)){
						m_maxTexSize[editor.CurrentEditingGroup] = newValue;
						onValueChanged();
					}
				}
			}
		}
	}

	/**
	 * Prepare is called whenever graph needs update. 
	 */ 
	public override void Prepare (BuildTarget target, 
		Model.NodeData node, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output Output) 
	{
		// Pass incoming assets straight to Output
		if(Output != null) {
			var destination = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();

			if(incoming != null) {
				foreach(var ag in incoming) {
					Output(destination, ag.assetGroups);
				}
			} else {
				// Overwrite output with empty Dictionary when there is no incoming asset
				Output(destination, new Dictionary<string, List<AssetReference>>());
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
		if (incoming != null)
		{
			foreach (var ag in incoming)
			{
				foreach (var group in ag.assetGroups)
				{
					foreach (var asset in group.Value)
					{
						string path = asset.importFrom;
						TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
						if (importer != null)
						{
							int maxTexSize = importer.maxTextureSize;
							int.TryParse(m_maxTexSize.CurrentPlatformValue, out int curValue);
							if (maxTexSize > curValue)
							{
								Debug.LogWarning(string.Format("存在半透明纹理尺寸超出预设值{0}，目录位于{1}",curValue,importer.assetPath));
							}
						}
					}
				}
			}
		}
	}
}
