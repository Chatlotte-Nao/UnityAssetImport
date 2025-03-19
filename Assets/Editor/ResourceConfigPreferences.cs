using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static ResourceConfigPreferences;

public class ResourceConfigPreferences : SettingsProvider
{
    [System.Serializable]
    public enum ResourceType
    {
        Texture,
        Audio,
        Model,
        Script,
        Other
    }
    [System.Serializable]
    public class ResourceEntry
    {       
        public string ResourceName = "";//配置方案名称
        public string NamingPrefix = "";//前缀
        public int TypeIndex = 0;//类型
        public string ExternalDirectory = "";//美术资源路径
        public string AssetsDirectory = "";//项目资源路径
    }

    [System.Serializable]
    private class ResourceListWrapper
    {
        public List<ResourceEntry> Entries = new List<ResourceEntry>();
    }

    private List<ResourceEntry> resourceEntries = new List<ResourceEntry>();

    private static readonly string[] typeOptions = Enum.GetNames(typeof(ResourceType));

    public ResourceConfigPreferences(string path, SettingsScope scope) : base(path, scope)
    {
        LoadSettings();
    }

    public override void OnGUI(string searchContext)
    {
        GUILayout.Label("美术资源配置", EditorStyles.boldLabel);

        if (GUILayout.Button("添加"))
        {
            resourceEntries.Add(new ResourceEntry());
        }

        if (GUILayout.Button("执行"))
        {
            foreach (var entry in resourceEntries) 
            {
                //从美术目录拷贝到资源目录
                ResourceEntryOperationMode1.Copy(entry, true);
            }

            AssetDatabase.Refresh();
            Debug.Log("所有文件更新成功");
        }

        EditorGUILayout.Space();

        for (int i = 0; i < resourceEntries.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            var entry = resourceEntries[i];

            entry.ResourceName = EditorGUILayout.TextField("资源名称", entry.ResourceName);
            entry.NamingPrefix = EditorGUILayout.TextField("命名规则前缀", entry.NamingPrefix);
            entry.TypeIndex = EditorGUILayout.Popup("类型", entry.TypeIndex, typeOptions);

            // 外部目录
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("外部目录", GUILayout.Width(80));
            entry.ExternalDirectory = EditorGUILayout.TextField(entry.ExternalDirectory, GUILayout.Width(500));

            if (GUILayout.Button("选择目录", GUILayout.Width(100)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择外部目录", entry.ExternalDirectory, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    entry.ExternalDirectory = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            // 项目目录
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Assets目录", GUILayout.Width(80));
            entry.AssetsDirectory = EditorGUILayout.TextField(entry.AssetsDirectory, GUILayout.Width(380));
            EditorGUILayout.EndHorizontal();

            /*if (GUILayout.Button("设置规则"))
            {
                Debug.Log($"设置规则: {entry.ResourceName}");
            }*/

            // 单独保存当前项
            if (GUILayout.Button("保存", GUILayout.Width(80)))
            {
                SaveSingleEntry(i);
            }

            if (GUILayout.Button("删除", GUILayout.Width(80)))
            {
                resourceEntries.RemoveAt(i);
                DeleteSettings(); // 删除后保存
                break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.Space();
    }

    [SettingsProvider]
    public static SettingsProvider CreateCustomPreferences()
    {
        return new ResourceConfigPreferences("Preferences/CustomAssetConfiguration", SettingsScope.User);
    }

    private void DeleteSettings()
    {
        var wrapper = new ResourceListWrapper { Entries = resourceEntries };
        string json = JsonUtility.ToJson(wrapper, true);
        EditorPrefs.SetString("ResourceConfig", json);
        Debug.Log("资源配置已保存: " + json);
    }

    private void SaveSingleEntry(int index)
    {
        if (index < 0 || index >= resourceEntries.Count) return;

        if (resourceEntries[index].NamingPrefix == "") 
        {
            EditorUtility.DisplayDialog("提示", "美术命名规则前缀未配置", "确定");
            return;
        }

        if (resourceEntries[index].ExternalDirectory == "")
        {
            EditorUtility.DisplayDialog("提示", "美术资源路劲未配置", "确定");
            return;
        }

        if (resourceEntries[index].AssetsDirectory == "")
        {
            EditorUtility.DisplayDialog("提示", "Assets目录未配置", "确定");
            return;
        }


        if (resourceEntries[index].ResourceName == "")
        {
            EditorUtility.DisplayDialog("提示", "配置名称未配置", "确定");
            return;
        }
        
        string directoryPath = "Assets/" + resourceEntries[index].AssetsDirectory;
        if (!File.Exists(directoryPath))
        {
            // 文件不存在时，创建文件夹
            CreateDirectory(directoryPath);
        }

        var wrapper = new ResourceListWrapper { Entries = resourceEntries };
        string json = JsonUtility.ToJson(wrapper, true);
        EditorPrefs.SetString("ResourceConfig", json);
        Debug.Log($"资源配置第 {index + 1} 项已保存: " + json);
    }

    private void LoadSettings()
    {
        if (EditorPrefs.HasKey("ResourceConfig"))
        {
            string json = EditorPrefs.GetString("ResourceConfig");
            var wrapper = JsonUtility.FromJson<ResourceListWrapper>(json);

            if (wrapper != null && wrapper.Entries != null)
            {
                resourceEntries = wrapper.Entries;
            }
        }
    }


    private static void CreateDirectory(string directoryPath)
    {
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        AssetDatabase.Refresh();
    }
}



public class ResourceEntryOperationMode1
{
    public static void Copy(ResourceEntry Entry, bool includeChildren = false) 
    {
        CopyFilesFromDirectory(Entry, includeChildren);
    }

    private static void CopyFilesFromDirectory(ResourceEntry Entry, bool includeChildren = false)
    {

        // 源目录（外部目录）
        string sourceDirectory = Entry.ExternalDirectory;  // 外部目录路径
        // 目标目录（Assets 目录下）
        string targetDirectory = "Assets\\" + Entry.AssetsDirectory;   // 目标目录路径

        // 检查源目录是否存在
        if (Directory.Exists(sourceDirectory))
        {
            // 确保目标目录存在
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);  // 如果目标目录不存在则创建它
            }

            List<string> allfiles = new List<string>();
            string[] files = null;
            if (includeChildren)
            {
                // 调用递归方法获取所有文件
                GetFilesRecursively(sourceDirectory, allfiles);
                files = allfiles.ToArray();
            }
            else 
            {
                files = Directory.GetFiles(sourceDirectory);
            }
            // 复制所有文件到目标目录
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                //判断是否包含前缀，如果没有包含则不拷贝
                if (fileName.ToLower().StartsWith(Entry.NamingPrefix.ToLower()))
                {
                    //查看本地目录是否有文件，获得C2C码 判断是否一致，如果不一致,需要更新到项目中
                    string inAssetsNamePath = "Assets\\" + Entry.AssetsDirectory + "\\" + Path.GetFileName(file);
                    if (File.Exists(inAssetsNamePath)) 
                    {
                        string crc32_out = CRC32.GetFileCrc32(file);
                        string crc32_in = CRC32.GetFileCrc32(inAssetsNamePath);
                        if (crc32_out == crc32_in) continue;
                        Debug.Log($"资源有变化{inAssetsNamePath}已更新");
                    }

                    //FileCheck(file, Entry);
                    // 构造目标文件路径
                    string targetFilePath = Path.Combine(targetDirectory, fileName);

                    // 复制文件到目标目录，true 表示覆盖已存在的文件
                    File.Copy(file, targetFilePath, true);
                }
                else if(!includeChildren) 
                {
                    Debug.LogWarning($"资源{file}不包含前缀{Entry.NamingPrefix}");
                }
                // 获取文件名（不包含路径）
             
            }
        }  
    }



    static void GetFilesRecursively(string directoryPath, List<string> fileList)
    {
        try
        {
            // 获取当前目录下的所有文件并加入列表
            string[] files = Directory.GetFiles(directoryPath);
            fileList.AddRange(files);

            // 获取当前目录下的所有子目录
            string[] subdirectories = Directory.GetDirectories(directoryPath);

            // 递归遍历每个子目录
            foreach (var subdirectory in subdirectories)
            {
                GetFilesRecursively(subdirectory, fileList);
            }
        }
        catch (Exception ex)
        {
            // 异常处理，例如权限问题，或目录不存在等
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

}

