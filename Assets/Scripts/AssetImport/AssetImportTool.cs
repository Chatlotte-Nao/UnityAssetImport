using System;
using System.Collections.Generic;
using UnityEngine;

public class AssetImportTool
{
    /// <summary>
    /// 导入资源进行各自参数设置
    /// </summary>
    public static void ImportAssetsAndSetUp(ImportAssetInfo importAssetInfo)
    {
        for (int i = 0; i < importAssetInfo.LoadPath.Length; i++)
        {
            switch (importAssetInfo.EnumType[i])
            {
                case TextureType.Default:
                
                    break;
            
                case TextureType.NormalMap:
                
                    break;
            
                case TextureType.Sprite:
                
                    break;
            
                case AudioType.HighFrequencyClip:
                
                    break;
            
                case AudioType.LowFrequencyClip:
                
                    break;
            
                case AudioType.LargeFileClip:
                
                    break;
            
                default:
                    Debug.LogError("未知的枚举类型 ");
                    break;
            }
        }
    }
}
