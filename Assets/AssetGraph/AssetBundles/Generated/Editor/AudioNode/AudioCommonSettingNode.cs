using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;
/// <summary>
/// 音频文件最后通用设置节点
/// </summary>
[CustomNode("Custom/Audio/AudioCommonSettingNode", 1000)]
public class AudioCommonSettingNode : Node {

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
		var newNode = new AudioCommonSettingNode();
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
						AudioImporter audioImporter = AssetImporter.GetAtPath(path) as AudioImporter;
						AudioCommonSetting(audioImporter);
						AssetDatabase.ImportAsset(asset.path, ImportAssetOptions.ForceUpdate);
					}
				}
			}
		}
	}

	/// <summary>
	/// 每个类型音频文件最后都要设置一遍
	/// </summary>
	private void AudioCommonSetting(AudioImporter audioImporter)
	{
		if (audioImporter != null)
		{
			AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioImporter.assetPath);
			AudioImporterSampleSettings settings = audioImporter.defaultSampleSettings;
			//如果是双声道且左右声道内容相同，则将其设置为单声道
			if (IsStereoWithSameContent(audioClip))
			{
				audioImporter.forceToMono = true;
			}
			else
			{
				audioImporter.forceToMono = false;
			}
							
			audioImporter.loadInBackground = true;
			settings.preloadAudioData = true;
							
			//如果是移动平台，且音频采样率超过22050Hz，则重写为22050Hz
			if (IsMobilePlatform() && audioClip.frequency > 22050)
			{
				settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;
				settings.sampleRateOverride = 22050;
			}
			audioImporter.defaultSampleSettings = settings;
		}
	}
	
	
	/// <summary>
	/// 检查是否为双声道且左右声道内容相同
	/// </summary>
	/// <param name="audioClip"></param>
	private bool IsStereoWithSameContent(AudioClip audioClip)
	{
		if (audioClip.channels == 2)
		{
			// 获取左右声道的数据
			float[] leftChannel = new float[audioClip.samples];
			float[] rightChannel = new float[audioClip.samples];
			audioClip.GetData(leftChannel, 0);
			audioClip.GetData(rightChannel, 0);

			// 比较左右声道数据是否相同
			for (int i = 0; i < audioClip.samples; i++)
			{
				if (leftChannel[i] != rightChannel[i])
				{
					return false; // 左右声道不相同
				}
			}
			return true; // 左右声道相同
		}
		return false;
	}
	/// <summary>
	/// 判断是否为移动平台（iOS或Android）
	/// </summary>
	private bool IsMobilePlatform()
	{
		return (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS || EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android);
	}
}
