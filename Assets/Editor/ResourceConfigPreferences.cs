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
        public string ResourceName = "";//���÷�������
        public string NamingPrefix = "";//ǰ׺
        public int TypeIndex = 0;//����
        public int SubTypeIndex = 0;//����
        public string ExternalDirectory = "";//������Դ·��
        public string AssetsDirectory = "";//��Ŀ��Դ·��
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

                Debug.Log("�����ļ����³ɹ�------------------>");
            }
        }
    }




    public override void OnGUI(string searchContext)
    {

       

        GUILayout.Label("������Դ����", EditorStyles.boldLabel);

        if (GUILayout.Button("���"))
        {
            resourceEntries.Add(new ResourceEntry());
        }

        if (GUILayout.Button("ִ��"))
        {

            if (!waitingForDelay)
            {

                timePassed = EditorApplication.timeSinceStartup;

                foreach (var entry in resourceEntries)
                {
                    //������Ŀ¼��������ԴĿ¼
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

            entry.ResourceName = EditorGUILayout.TextField("��Դ����", entry.ResourceName);
            entry.NamingPrefix = EditorGUILayout.TextField("��������ǰ׺", entry.NamingPrefix);
            entry.TypeIndex = EditorGUILayout.Popup("������", entry.TypeIndex, typeOptions);
            entry.SubTypeIndex = EditorGUILayout.Popup("������", entry.SubTypeIndex, typeSubOptions[entry.TypeIndex]);
            // �ⲿĿ¼
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("�ⲿĿ¼", GUILayout.Width(80));
            entry.ExternalDirectory = EditorGUILayout.TextField(entry.ExternalDirectory, GUILayout.Width(500));

            if (GUILayout.Button("ѡ��Ŀ¼", GUILayout.Width(100)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("ѡ���ⲿĿ¼", entry.ExternalDirectory, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    entry.ExternalDirectory = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            // ��ĿĿ¼
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetsĿ¼", GUILayout.Width(80));
            entry.AssetsDirectory = EditorGUILayout.TextField(entry.AssetsDirectory, GUILayout.Width(380));
            EditorGUILayout.EndHorizontal();

            // �������浱ǰ��
            if (GUILayout.Button("����", GUILayout.Width(80)))
            {
                SaveSingleEntry(i);
            }

            if (GUILayout.Button("ɾ��", GUILayout.Width(80)))
            {
                resourceEntries.RemoveAt(i);
                DeleteSettings(); // ɾ���󱣴�
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
        Debug.Log("��Դ�����ѱ���: " + json);
    }

    private void SaveSingleEntry(int index)
    {
        if (index < 0 || index >= resourceEntries.Count) return;

        if (resourceEntries[index].NamingPrefix == "") 
        {
            EditorUtility.DisplayDialog("��ʾ", "������������ǰ׺δ����", "ȷ��");
            return;
        }

        if (resourceEntries[index].ExternalDirectory == "")
        {
            EditorUtility.DisplayDialog("��ʾ", "������Դ·��δ����", "ȷ��");
            return;
        }

        if (resourceEntries[index].AssetsDirectory == "")
        {
            EditorUtility.DisplayDialog("��ʾ", "AssetsĿ¼δ����", "ȷ��");
            return;
        }


        if (resourceEntries[index].ResourceName == "")
        {
            EditorUtility.DisplayDialog("��ʾ", "��������δ����", "ȷ��");
            return;
        }
        
        string directoryPath = "Assets/" + resourceEntries[index].AssetsDirectory;
        if (!File.Exists(directoryPath)) CreateDirectory(directoryPath);
   
        var wrapper = new ResourceListWrapper { Entries = resourceEntries };
        string json = JsonUtility.ToJson(wrapper, true);
    
        if (!Directory.Exists("ResourceSettings")) Directory.CreateDirectory("ResourceSettings");
        File.WriteAllText(ConfigJsonPath, json);
        //EditorPrefs.SetString("ResourceConfig", json);
        Debug.Log($"��Դ���õ� {index + 1} ���ѱ���: " + json);
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
        // ��ȡָ�������е�����ö������
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
        string sourceDirectory = Entry.ExternalDirectory;  // �ⲿĿ¼·��
        string targetDirectory = "Assets\\" + Entry.AssetsDirectory;   // Ŀ��Ŀ¼·��
        if (Directory.Exists(sourceDirectory))
        {
            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);  // ���Ŀ��Ŀ¼�������򴴽���
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
            // ���������ļ���Ŀ��Ŀ¼
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                //�ж��Ƿ����ǰ׺�����û�а����򲻿���
                if (fileName.ToLower().StartsWith(Entry.NamingPrefix.ToLower()))
                {
                    //�鿴����Ŀ¼�Ƿ����ļ������C2C�� �ж��Ƿ�һ�£������һ��,��Ҫ���µ���Ŀ��
                    string inAssetsNamePath = "Assets\\" + Entry.AssetsDirectory + "\\" + Path.GetFileName(file);
                    if (File.Exists(inAssetsNamePath)) 
                    {
                        string crc32_out = CRC32.GetFileCrc32(file);
                        string crc32_in = CRC32.GetFileCrc32(inAssetsNamePath);
                        if (crc32_out == crc32_in) continue;
                        Debug.Log($"��Դ�б仯{inAssetsNamePath}�Ѹ���");
                    }

                    string targetFilePath = Path.Combine(targetDirectory, fileName);

                    // �����ļ���Ŀ��Ŀ¼��true ��ʾ�����Ѵ��ڵ��ļ�
                    File.Copy(file, targetFilePath, true);
                    OperationResourceEntrys.Add(file, Entry);
                }
                else if(!includeChildren) 
                {
                    Debug.LogWarning($"��Դ{file}������ǰ׺{Entry.NamingPrefix}");
                }

             
            }
        }
        
    }



    static void GetFilesRecursively(string directoryPath, List<string> fileList)
    {
        try
        {
            // ��ȡ��ǰĿ¼�µ������ļ��������б�
            string[] files = Directory.GetFiles(directoryPath);
            fileList.AddRange(files);

            // ��ȡ��ǰĿ¼�µ�������Ŀ¼
            string[] subdirectories = Directory.GetDirectories(directoryPath);

            // �ݹ����ÿ����Ŀ¼
            foreach (var subdirectory in subdirectories)
            {
                GetFilesRecursively(subdirectory, fileList);
            }
        }
        catch (Exception ex)
        {
            // �쳣��������Ȩ�����⣬��Ŀ¼�����ڵ�
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

 

}

