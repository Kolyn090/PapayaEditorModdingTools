using System.Collections.Generic;
using System.IO;
using ModdingTool.Assets.Editor.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ModdingTool.Assets.Editor.SpriteDumpFixer
{
    public class SpriteDumpFixerMain : EditorWindow
    {
        private string _owningSpriteDumpsFolderPath = "";
        private string _sourceSpriteDumpsFolderPath = "";

        [MenuItem("Tools/04 Fix Dump Files (Sprites)", priority = 4)]
        public static void ShowWindow()
        {
            GetWindow<SpriteDumpFixerMain>("Fix Dump Files");
        }

        private void OnGUI()
        {
            GUIStyle style = new(GUI.skin.label)
            {
                wordWrap = true,
            };
            _owningSpriteDumpsFolderPath = DragAndDropUtil.DragAndDropFolderField("My Sprite Dumps Folder*\n我的图像导出文件夹*", _owningSpriteDumpsFolderPath);
            _sourceSpriteDumpsFolderPath = DragAndDropUtil.DragAndDropFolderField("Source Sprite Dumps Folder*\n源图像导出文件夹*", _sourceSpriteDumpsFolderPath);
            GUILayout.Space(5);

            GUI.enabled = !string.IsNullOrWhiteSpace(_owningSpriteDumpsFolderPath) &&
                            !string.IsNullOrWhiteSpace(_sourceSpriteDumpsFolderPath);

            if (GUILayout.Button("Run (运行)"))
            {
                FixNamesOfSpriteDumps();
                FixPathID();
            }

            GUI.enabled = true;

            GUILayout.Space(35);

            string messageEn = "";
            string messageZh = "";

            GUILayout.Label(messageEn, style);
            GUILayout.Label(messageZh, style);
        }

        private void FixNamesOfSpriteDumps()
        {
            int numOfSuccess = 0;
            int numOfError = 0;
            Dictionary<string, string> sourceBaseFullName = new();
            string[] sourceJsonFiles = Directory.GetFiles(_sourceSpriteDumpsFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string jsonFile in sourceJsonFiles)
            {
                JObject spriteJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (spriteJson.ContainsKey("m_Rect")) // Make sure it's a Sprite Dump File
                {
                    string fileName = Path.GetFileName(jsonFile);
                    (string, string) baseNameFullName = NameUtil.GetBaseNameFullNameSplitCab(fileName);
                    string baseName = baseNameFullName.Item1;
                    string fullName = baseNameFullName.Item2;
                    sourceBaseFullName.Add(baseName, fullName);
                }
            }

            string[] owningJsonFiles = Directory.GetFiles(_owningSpriteDumpsFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string jsonFile in owningJsonFiles)
            {
                JObject spriteJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (spriteJson.ContainsKey("m_Rect")) // Make sure it's a Sprite Dump File
                {
                    string fileName = Path.GetFileName(jsonFile);
                    (string, string) baseNameFullName = NameUtil.GetBaseNameFullNameSplitCab(fileName);
                    string baseName = baseNameFullName.Item1;
                    if (sourceBaseFullName.ContainsKey(baseName))
                    {
                        numOfSuccess++;
                        AssetDatabase.RenameAsset(jsonFile, sourceBaseFullName[baseName]);
                    }
                    else
                    {
                        numOfError++;
                        Debug.LogError($"Missing Source Sprite Dump File for {baseName}.");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Fixed {numOfSuccess} names of Sprite Dump Files in {_owningSpriteDumpsFolderPath}. Found {numOfError} error(s).");
        }

        private void FixPathID()
        {
            string[] sourceJsonFiles = Directory.GetFiles(_sourceSpriteDumpsFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            long? replacePathID = null;
            foreach (string jsonFile in sourceJsonFiles)
            {
                JObject spriteJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (spriteJson.ContainsKey("m_Rect")) // Make sure it's a Sprite Dump File
                {
                    replacePathID = long.Parse(spriteJson["m_RD"]["texture"]["m_PathID"].ToString());
                    break;
                }
            }

            if (replacePathID == null)
            {
                Debug.LogError($"No Source Sprite Dump File found in {_sourceSpriteDumpsFolderPath}");
                return;
            }

            int numOfSuccess = 0;
            string[] jsonFiles = Directory.GetFiles(_owningSpriteDumpsFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string jsonFile in jsonFiles)
            {
                JObject spriteJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (spriteJson.ContainsKey("m_Rect")) // Make sure it's a Sprite Dump File
                {
                    spriteJson["m_RD"]["texture"]["m_PathID"] = replacePathID;
                    numOfSuccess++;
                }
                File.WriteAllText(jsonFile, JsonConvert.SerializeObject(spriteJson, Formatting.Indented));
            }

            Debug.Log($"Fixed Path ID of {numOfSuccess} Sprite Dump Files in {_owningSpriteDumpsFolderPath}.");
        }
    }
}