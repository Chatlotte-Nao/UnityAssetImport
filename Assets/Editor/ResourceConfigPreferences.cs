using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VersionControl;
using UnityEngine;
using static ResourceConfigPreferences;

public class ResourceConfigPreferences : SettingsProvider
{
 

    [System.Serializable]
    public class ResourceEntry
    {       
        public string ResourceName = "";//配置方案名称
        public string NamingPrefix = "";//前缀
        public int TypeIndex = 0;//类型
        public int SubTypeIndex = 0;//类型
        public string ExternalDirectory = "";//美术资源路径
        public string AssetsDirectory = "";//项目资源路径
    }

    [System.Serializable]
    private class ResourceListWrapper
    {
        public List<ResourceEntry> Entries = new List<ResourceEntry>();
    }


    private string ConfigJsonPath = "ResourceSettings/ResourceConfig.json";

    private List<ResourceEntry> resourceEntries = new List<ResourceEntry>();

    private static string[] typeOptions;// = Enum.GetNames(typeof(ResourceType));
    private static List<string[]> typeSubOptions = new List<string[]>();

    private double timePassed = 0f;  
    private bool waitingForDelay = false;
    public ResourceConfigPreferences(string path, SettingsScope scope) : base(path, scope)
    {
        LoadSettings();
      
    }

    private void Update()
    {
        if (waitingForDelay)
        {

            if (EditorApplication.timeSinceStartup - timePassed >= 2f) 
            {
                waitingForDelay = false; 

                Excute();

                AssetDatabase.Refresh();

                Debug.Log("所有文件更新成功------------------>");
            }
        }
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

            if (!waitingForDelay)
            {

                timePassed = EditorApplication.timeSinceStartup;

                foreach (var entry in resourceEntries)
                {
                    //从美术目录拷贝到资源目录
                    ResourceEntryOperationMode1.Copy(entry, true);
                }

                AssetDatabase.Refresh();
                waitingForDelay = true;

            }
           

          
        }

        EditorGUILayout.Space();

        for (int i = 0; i < resourceEntries.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            var entry = resourceEntries[i];

            entry.ResourceName = EditorGUILayout.TextField("资源名称", entry.ResourceName);
            entry.NamingPrefix = EditorGUILayout.TextField("命名规则前缀", entry.NamingPrefix);
            entry.TypeIndex = EditorGUILayout.Popup("主类型", entry.TypeIndex, typeOptions);
            entry.SubTypeIndex = EditorGUILayout.Popup("子类型", entry.SubTypeIndex, typeSubOptions[entry.TypeIndex]);
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

    private void Excute()
    {
        string[] loadPaths = new string[this.resourceEntries.Count];
        Enum[] enumTypes = new Enum[this.resourceEntries.Count];
        int index = 0;
        foreach (var entry in this.resourceEntries)
        {
            loadPaths[index] = "Assets/" + entry.AssetsDirectory;
            enumTypes[index] = ResourceConfigPreferences.GetValueByIndex(entry.TypeIndex, entry.SubTypeIndex);
            Debug.Log(loadPaths[index]);
            Debug.Log(enumTypes[index]);
        }

        ImportAssetInfo info = new ImportAssetInfo(loadPaths, enumTypes);
        AssetImportTool.ImportAssetsAndSetUp(info);
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
        if (!File.Exists(directoryPath)) CreateDirectory(directoryPath);
   
        var wrapper = new ResourceListWrapper { Entries = resourceEntries };
        string json = JsonUtility.ToJson(wrapper, true);
    
        if (!Directory.Exists("ResourceSettings")) Directory.CreateDirectory("ResourceSettings");
        File.WriteAllText(ConfigJsonPath, json);
        //EditorPrefs.SetString("ResourceConfig", json);
        Debug.Log($"资源配置第 {index + 1} 项已保存: " + json);
    }
    
    private void LoadSettings()
    {
        if (Directory.Exists("ResourceSettings"))   //EditorPrefs.HasKey("ResourceConfig"))
        {
            string json = File.ReadAllText(ConfigJsonPath);
            var wrapper = JsonUtility.FromJson<ResourceListWrapper>(json);

            if (wrapper != null && wrapper.Entries != null)
            {
                resourceEntries = wrapper.Entries;
            }
        }

        EditorApplication.update += Update;

        typeSubOptions.Clear();
        var enumTypes = GetEnumTypes(typeof(AssetEnum));
        typeOptions = new string[enumTypes.Length];

        for (int indexRow = 0; indexRow < enumTypes.Length; indexRow++)
        {
            var enumType = enumTypes[indexRow];
            typeOptions[indexRow] = enumType.Name;

            Array subEnumType = Enum.GetValues(enumType);
            string[] subEnum = new string[subEnumType.Length];

            for (int i = 0; i < subEnumType.Length; i++) subEnum[i] = subEnumType.GetValue(i).ToString();

            typeSubOptions.Add(subEnum);
        }

    }



    public static Enum GetValueByIndex(int type, int subType)
    {
        var enumTypes = GetEnumTypes(typeof(AssetEnum));
        if (type < 0 || type >= enumTypes.Length)
        {
            return AssetEnum.TextureType.Default; 
        }
        Array subEnumType = Enum.GetValues(enumTypes[type]);
        if (subType < 0 || subType >= subEnumType.Length)
        {
            return AssetEnum.TextureType.Default; 
        }

        return (Enum)subEnumType.GetValue(subType);
    }

    private static void CreateDirectory(string directoryPath)
    {
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        AssetDatabase.Refresh();
    }


    private static Type[] GetEnumTypes(Type type)
    {
        // 获取指定类型中的所有枚举类型
        return type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                   .Where(t => t.IsEnum)
                   .ToArray();
    }



}



public class ResourceEntryOperationMode1
{
    public static Dictionary<string, ResourceEntry> OperationResourceEntrys = new Dictionary<string, ResourceEntry>();
    public static void Copy(ResourceEntry Entry, bool includeChildren = false) 
    {
        CopyFilesFromDirectory(Entry, includeChildren);
    }

    private static void CopyFilesFromDirectory(ResourceEntry Entry, bool includeChildren = false)
    {
        OperationResourceEntrys.Clear();
        string sourceDirectory = Entry.ExternalDirectory;  // 外部目录路径
        string targetDirectory = "Assets\\" + Entry.AssetsDirectory;   // 目标目录路径
        if (Directory.Exists(sourceDirectory))
        {
            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);  // 如果目标目录不存在则创建它
            List<string> allfiles = new List<string>();
            string[] files = null;
            if (includeChildren)
            {
       
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

                    string targetFilePath = Path.Combine(targetDirectory, fileName);

                    // 复制文件到目标目录，true 表示覆盖已存在的文件
                    File.Copy(file, targetFilePath, true);
                    OperationResourceEntrys.Add(file, Entry);
                }
                else if(!includeChildren) 
                {
                    Debug.LogWarning($"资源{file}不包含前缀{Entry.NamingPrefix}");
                }

             
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

