using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

[CustomNode("Custom/AudioNode", 1000)]
public class AudioNode : Node {

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
		var newNode = new AudioNode();
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
		if (Output != null && connectionsToOutput != null && connectionsToOutput.Any())
		{
			// 有下游节点时，进行输出
			var destination = connectionsToOutput.First();
			Output(destination, new Dictionary<string, List<AssetReference>>());
		}
		else
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

							if (audioImporter != null)
							{
								// 如果是双声道且左右声道内容相同，则将其合并为单声道
								// if (IsStereoWithSameContent(audioImporter))
								// {
								// 	audioImporter.forceToMono = true;
								// }
								audioImporter.forceToMono = true;

								// 启用后台加载和预加载音频数据
								audioImporter.loadInBackground = true;
								// //audioImporter.preloadAudioData = true;
								//
								// // 根据音效类型选择压缩格式
								// if (IsShortEffect(audioImporter))
								// {
								// 	// 短音效使用PCM压缩格式
								// 	audioImporter.audioCompressionFormat = AudioCompressionFormat.PCM;
								// }
								// else if (IsLongEffectOrMusic(audioImporter))
								// {
								// 	// 长音效或背景音乐使用Vorbis压缩格式
								// 	audioImporter.audioCompressionFormat = AudioCompressionFormat.Vorbis;
								// }
								//
								// // 根据音频使用频率设置加载模式
								// if (IsHighFrequencySound(audioImporter))
								// {
								// 	// 高频音效（例如UI音效、按钮点击音效）使用Decompress On Load
								// 	audioImporter.loadType = AudioImporterLoadType.DecompressOnLoad;
								// }
								// else if (IsLowFrequencySound(audioImporter))
								// {
								// 	// 低频音效（例如环境音效）使用Compressed In Memory
								// 	audioImporter.loadType = AudioImporterLoadType.CompressedInMemory;
								// }
								// else
								// {
								// 	// 大型音频（如背景音乐）使用Streaming
								// 	audioImporter.loadType = AudioImporterLoadType.Streaming;
								// }
								//
								// // 如果是移动平台，且音频采样率超过22050Hz，则重写为22050Hz
								// if (IsMobilePlatform() && audioImporter.sampleRate > 22050)
								// {
								// 	audioImporter.sampleRateOverride = 22050;
								// }
							}

							// if (!outputGroups.ContainsKey(group.Key))
							// {
							// 	outputGroups[group.Key] = new List<AssetReference>();
							// }
							//
							// outputGroups[group.Key].Add(asset);
							// 应用修改到资产
							AssetDatabase.ImportAsset(asset.path, ImportAssetOptions.ForceUpdate);
						}
					}
				}
			}
			// 没有下游节点时，什么都不做，避免报错
			Debug.Log("没有下游节点，跳过输出");
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

	// 检查是否为双声道且左右声道内容相同
	private bool IsStereoWithSameContent(AudioImporter audioImporter)
	{
		AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioImporter.assetPath);
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

// 判断是否为短音效（例如：长度小于10秒的音效）
	private bool IsShortEffect(AudioImporter audioImporter)
	{
		AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioImporter.assetPath);
		return audioClip.length < 10.0f; // 如果音频长度小于10秒，认为是短音效
	}

// 判断是否为长音效或音乐（例如：长度大于等于10秒的音效或音乐）
	private bool IsLongEffectOrMusic(AudioImporter audioImporter)
	{
		AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioImporter.assetPath);
		return audioClip.length >= 10.0f; // 如果音频长度大于等于10秒，认为是长音效或音乐
	}

// 判断是否为高频音效（例如：UI音效、按钮点击音效等）
	private bool IsHighFrequencySound(AudioImporter audioImporter)
	{
		AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioImporter.assetPath);
		return audioClip.length < 2.0f; // 如果音频很短（小于2秒），认为是高频音效
	}

	///判断是否为低频音效（例如：环境音效、背景音乐等）
	private bool IsLowFrequencySound(AudioImporter audioImporter)
	{
		AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioImporter.assetPath);
		return audioClip.length >= 10.0f; // 如果音频较长（大于等于10秒），认为是低频音效
	}

	//判断是否为移动平台（iOS或Android）
	private bool IsMobilePlatform()
	{
		return (Application.platform == RuntimePlatform.IPhonePlayer ||
		        Application.platform == RuntimePlatform.Android);
	}
}
