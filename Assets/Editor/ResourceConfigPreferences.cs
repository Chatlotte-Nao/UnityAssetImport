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
        public string ResourceName = "";//���÷�������
        public string NamingPrefix = "";//ǰ׺
        public int TypeIndex = 0;//����
        public string ExternalDirectory = "";//������Դ·��
        public string AssetsDirectory = "";//��Ŀ��Դ·��
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
        GUILayout.Label("������Դ����", EditorStyles.boldLabel);

        if (GUILayout.Button("���"))
        {
            resourceEntries.Add(new ResourceEntry());
        }

        if (GUILayout.Button("ִ��"))
        {
            foreach (var entry in resourceEntries) 
            {
                //������Ŀ¼��������ԴĿ¼
                ResourceEntryOperationMode1.Copy(entry, true);
            }

            AssetDatabase.Refresh();
            Debug.Log("�����ļ����³ɹ�");
        }

        EditorGUILayout.Space();

        for (int i = 0; i < resourceEntries.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            var entry = resourceEntries[i];

            entry.ResourceName = EditorGUILayout.TextField("��Դ����", entry.ResourceName);
            entry.NamingPrefix = EditorGUILayout.TextField("��������ǰ׺", entry.NamingPrefix);
            entry.TypeIndex = EditorGUILayout.Popup("����", entry.TypeIndex, typeOptions);

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

            /*if (GUILayout.Button("���ù���"))
            {
                Debug.Log($"���ù���: {entry.ResourceName}");
            }*/

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
        if (!File.Exists(directoryPath))
        {
            // �ļ�������ʱ�������ļ���
            CreateDirectory(directoryPath);
        }

        var wrapper = new ResourceListWrapper { Entries = resourceEntries };
        string json = JsonUtility.ToJson(wrapper, true);
        EditorPrefs.SetString("ResourceConfig", json);
        Debug.Log($"��Դ���õ� {index + 1} ���ѱ���: " + json);
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

        // ԴĿ¼���ⲿĿ¼��
        string sourceDirectory = Entry.ExternalDirectory;  // �ⲿĿ¼·��
        // Ŀ��Ŀ¼��Assets Ŀ¼�£�
        string targetDirectory = "Assets\\" + Entry.AssetsDirectory;   // Ŀ��Ŀ¼·��

        // ���ԴĿ¼�Ƿ����
        if (Directory.Exists(sourceDirectory))
        {
            // ȷ��Ŀ��Ŀ¼����
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);  // ���Ŀ��Ŀ¼�������򴴽���
            }

            List<string> allfiles = new List<string>();
            string[] files = null;
            if (includeChildren)
            {
                // ���õݹ鷽����ȡ�����ļ�
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

                    //FileCheck(file, Entry);
                    // ����Ŀ���ļ�·��
                    string targetFilePath = Path.Combine(targetDirectory, fileName);

                    // �����ļ���Ŀ��Ŀ¼��true ��ʾ�����Ѵ��ڵ��ļ�
                    File.Copy(file, targetFilePath, true);
                }
                else if(!includeChildren) 
                {
                    Debug.LogWarning($"��Դ{file}������ǰ׺{Entry.NamingPrefix}");
                }
                // ��ȡ�ļ�����������·����
             
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

