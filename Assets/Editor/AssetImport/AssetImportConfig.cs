using System;
using System.Collections.Generic;

public class AssetEnum
{
    public enum TextureType
    {
        Default,//默认格式
        NormalMap,//法线贴图
        Sprite,//精灵UI图片
        SingleChannel,//单通道图片
        Translucent //半透明图片
    }
    
    public enum AudioType
    {
        HighFrequencyClip,  //高频率使用，文件小，比如用作按钮点击音效等
        LowFrequencyClip,   //低频率使用，文件大小中等，比如角色的对话语音
        LargeFileClip      //文件大播放时间长，用作背景音乐
    }
}

/// <summary>
/// 记录导入资源信息
/// </summary>
public class ImportAssetInfo
{
    public string[] LoadPath { get;}
    public Enum[] EnumType { get; }

    public ImportAssetInfo(string[] loadPath, Enum[] enumType)
    {
        LoadPath = loadPath;
        EnumType = enumType;
    }
}

public class ImportAssetConfig
{
    public const string TEXTURE_GRAPH_PATH = "Assets/AssetGraph/Graph/TextureImportGraph.asset";
    public const string AUDIO_GRAPH_PATH = "Assets/AssetGraph/Graph/AudioImportGraph.asset";
}







