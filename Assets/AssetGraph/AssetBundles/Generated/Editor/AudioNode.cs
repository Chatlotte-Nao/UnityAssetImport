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
/// 音频文件设置节点
/// </summary>
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
						string[] labels = AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(path));
						if (labels.Contains("AudioModified"))
						{
							Debug.Log($"跳过已修改的文件: {path}");
							continue;
						}
						
						if (audioImporter != null)
						{
							AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioImporter.assetPath);
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
							AudioImporterSampleSettings settings = audioImporter.defaultSampleSettings;
							settings.preloadAudioData = true;
							//根据音效类型选择压缩格式
							if (IsShortEffect(audioClip))
							{
								//短音效使用PCM压缩格式
								settings.compressionFormat=AudioCompressionFormat.PCM;
							}
							else if (IsLongEffectOrMusic(audioClip))
							{
								//长音效或背景音乐使用Vorbis压缩格式
								settings.compressionFormat=AudioCompressionFormat.Vorbis;
								settings.quality = 0.9f;
							}
							// 根据音频使用频率设置加载模式
							if (IsHighFrequencySound(audioClip))
							{
								//高频音效（例如UI音效、按钮点击音效）使用Decompress On Load
								settings.loadType=AudioClipLoadType.DecompressOnLoad;
							}
							else if (IsLowFrequencySound(audioClip))
							{
								//低频音效（例如环境音效）使用Compressed In Memory
								settings.loadType=AudioClipLoadType.CompressedInMemory;
							}
							else
							{
								//大型音频（如背景音乐）使用Streaming
								settings.loadType=AudioClipLoadType.Streaming;
							}
							//如果是移动平台，且音频采样率超过22050Hz，则重写为22050Hz
							if (IsMobilePlatform() && audioClip.frequency > 22050)
							{
								settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;
								settings.sampleRateOverride = 22050;
							}
							audioImporter.defaultSampleSettings = settings;
						}
						// 添加标签
						AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(path), labels.Append("AudioModified").ToArray());
						AssetDatabase.ImportAsset(asset.path, ImportAssetOptions.ForceUpdate);
					}
				}
			}
		}
	}
	
	/// <summary>
	/// 检查是否为双声道且左右声道内容相同
	/// </summary>
	/// <param name="audioClip"></param>
	/// <returns></returns>
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
	/// 判断是否为短音效（例如：长度小于10秒的音效）
	/// </summary>
	/// <param name="audioClip"></param>
	/// <returns></returns>
	private bool IsShortEffect(AudioClip audioClip)
	{
		return audioClip.length < 10.0f; // 如果音频长度小于10秒，认为是短音效
	}
	/// <summary>
	/// 判断是否为长音效或音乐（例如：长度大于等于10秒的音效或音乐）
	/// </summary>
	/// <param name="audioClip"></param>
	/// <returns></returns>
	private bool IsLongEffectOrMusic(AudioClip audioClip)
	{
		return audioClip.length >= 10.0f; // 如果音频长度大于等于10秒，认为是长音效或音乐
	}
	/// <summary>
	/// 判断是否为高频音效（例如：UI音效、按钮点击音效等）
	/// </summary>
	/// <param name="audioClip"></param>
	/// <returns></returns>
	private bool IsHighFrequencySound(AudioClip audioClip)
	{
		return audioClip.length < 2.0f; // 如果音频很短（小于2秒），认为是高频音效
	}
	/// <summary>
	/// 判断是否为低频音效（例如：环境音效、背景音乐等）
	/// </summary>
	/// <param name="audioClip"></param>
	/// <returns></returns>
	private bool IsLowFrequencySound(AudioClip audioClip)
	{
		return audioClip.length >= 10.0f; // 如果音频较长（大于等于10秒），认为是低频音效
	}
	/// <summary>
	/// 判断是否为移动平台（iOS或Android）
	/// </summary>
	/// <returns></returns>
	private bool IsMobilePlatform()
	{
		return (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android);
	}
}
