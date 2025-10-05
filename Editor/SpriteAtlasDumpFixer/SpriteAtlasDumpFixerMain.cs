using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModdingTool.Assets.Editor.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ModdingTool.Assets.Editor.SpriteAtlasDumpFixer
{
    public class SpriteAtlasDumpFixerMain : EditorWindow
    {
        private string _sourceAtlasFilePath = "";
        private string _owningAtlasFilePath = "";
        private string _owningFolderPath = "";
        private string _sourceFolderPath = "";

        [MenuItem("Tools/06 Fix Dump Files (Atlas)", priority = 6)]
        public static void ShowWindow()
        {
            GetWindow<SpriteAtlasDumpFixerMain>("SpriteAtlas Dump Fixer");
        }

        private void OnGUI()
        {
            GUIStyle style = new(GUI.skin.label)
            {
                wordWrap = true
            };
            GUIStyle boldWrapStyle = new(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            _owningFolderPath = DragAndDropUtil.DragAndDropFolderField("My Sprite Dump Folder*\n我的图像导出文件夹*", _owningFolderPath); // Required
            _sourceFolderPath = DragAndDropUtil.DragAndDropFolderField("Source Sprite Dump Folder*\n源图像导出文件夹*", _sourceFolderPath);
            GUILayout.Space(35);

            GUILayout.Label("Don't fill if Source Sprite Dump Folder contains this file\n如果源图像导出文件夹里包含这个文件则不用填", boldWrapStyle);
            _sourceAtlasFilePath = DragAndDropUtil.DragAndDropFileField("Source Atlas Dump\n源自动图集导出", _sourceAtlasFilePath, "json");
            GUILayout.Label("Don't fill if My Sprite Dump Folder contains this file\n如果我的图像导出文件夹里包含这个文件则不用填", boldWrapStyle);
            _owningAtlasFilePath = DragAndDropUtil.DragAndDropFileField("My Atlas Dump\n我的自动图集导出", _owningAtlasFilePath, "json");
            GUILayout.Space(5);

            GUI.enabled = !string.IsNullOrWhiteSpace(_owningFolderPath) && !string.IsNullOrWhiteSpace(_sourceFolderPath);

            if (GUILayout.Button("Run (运行)"))
            {
                _owningAtlasFilePath = GetOwningAtlasFilePath();
                if (string.IsNullOrWhiteSpace(_owningAtlasFilePath))
                    Debug.LogError("Could not find My Atlas Dump File. Please make sure you assign one.");

                _sourceAtlasFilePath = GetSourceAtlasFilePath();
                if (string.IsNullOrWhiteSpace(_sourceAtlasFilePath))
                    Debug.LogError("Could not find Source Atlas Dump File. Please make sure you assign one.");

                ReplaceRenderDataMapPathID();
                FixSpriteAtlasPathIDInDump();
                FixSpriteDumpNames();
                CopyIDNameFromSource();
            }

            GUI.enabled = true;

            GUILayout.Space(35);

            string messageEn = "";
            string messageZh = "";

            GUILayout.Label(messageEn, style);
            GUILayout.Label(messageZh, style);
        }

        private string GetOwningAtlasFilePath()
        {
            if (!string.IsNullOrWhiteSpace(_owningAtlasFilePath))
            {
                return _owningAtlasFilePath;
            }
            return GetFileUtil.GetFileByFieldKey(_owningFolderPath, "m_PackedSprites");
        }

        private string GetSourceAtlasFilePath()
        {
            if (!string.IsNullOrWhiteSpace(_sourceAtlasFilePath))
            {
                return _sourceAtlasFilePath;
            }
            return GetFileUtil.GetFileByFieldKey(_sourceFolderPath, "m_PackedSprites");
        }

        /// <summary>
        /// * SpriteAtlas
        /// Copy fields "m_PackedSprites" and "m_PackedSpriteNamesToIndex" from source to owning.
        /// </summary>
        private void CopyIDNameFromSource()
        {
            JObject sourceAtlasFileJson = JObject.Parse(File.ReadAllText(_sourceAtlasFilePath));
            JObject owningAtlasFileJson = JObject.Parse(File.ReadAllText(_owningAtlasFilePath));
            owningAtlasFileJson["m_PackedSprites"] = sourceAtlasFileJson["m_PackedSprites"];
            owningAtlasFileJson["m_PackedSpriteNamesToIndex"] = sourceAtlasFileJson["m_PackedSpriteNamesToIndex"];
            File.WriteAllText(_owningAtlasFilePath, JsonConvert.SerializeObject(owningAtlasFileJson, Formatting.Indented));
            Debug.Log($"Finished copying m_PackedSprites and m_PackedSpriteNamesToIndex fields from {_owningAtlasFilePath} to {_sourceAtlasFilePath}");
        }

        /// <summary>
        /// * SpriteAtlas
        /// Assign path id to all RenderDataMap in owning. The path id should be the same (hopefully).
        /// Can one SpriteAtlas control more than one Texture2D?
        /// </summary>
        private void ReplaceRenderDataMapPathID()
        {
            JObject sourceAtlasFileJson = JObject.Parse(File.ReadAllText(_sourceAtlasFilePath));
            JObject owningAtlasFileJson = JObject.Parse(File.ReadAllText(_owningAtlasFilePath));

            var renderDataMapSource = sourceAtlasFileJson["m_RenderDataMap"]["Array"];
            var pathIDToChange = renderDataMapSource[0]["second"]["texture"]["m_PathID"];

            var renderDataMapOwning = owningAtlasFileJson["m_RenderDataMap"]["Array"];
            for (int i = 0; i < renderDataMapOwning.Count(); i++)
            {
                renderDataMapOwning[i]["second"]["texture"]["m_PathID"] = pathIDToChange;
            }

            owningAtlasFileJson["m_RenderDataMap"]["Array"] = renderDataMapOwning;
            File.WriteAllText(_owningAtlasFilePath, JsonConvert.SerializeObject(owningAtlasFileJson, Formatting.Indented));
            Debug.Log($"Fixed Render Data Map Path ID in {_owningAtlasFilePath}.");
        }

        /// <summary>
        /// Change all sprite dump's m_SpriteAtlas path id to source SpriteAtlas path id
        /// </summary>
        private void FixSpriteAtlasPathIDInDump()
        {
            (string, long) sourceAtlasNamePathID = NameUtil.GetNamePathIDFromDumpName(Path.GetFileName(_sourceAtlasFilePath));
            long sourceAtlasPathID = sourceAtlasNamePathID.Item2;
            string[] jsonFiles = Directory.GetFiles(_owningFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            int counter = 0;
            foreach (string jsonFile in jsonFiles)
            {
                JObject spriteJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (spriteJson.ContainsKey("m_Rect")) // Make sure it's a Sprite Dump File
                {
                    spriteJson["m_SpriteAtlas"]["m_PathID"] = sourceAtlasPathID;
                    counter++;
                }
                File.WriteAllText(jsonFile, JsonConvert.SerializeObject(spriteJson, Formatting.Indented));
            }
            Debug.Log($"Fixed SpriteAtlas Path ID in {counter} dump files");
        }

        // Known bug: Cannot handle Special situation: multiple Sprites with the same name
        /// <summary>
        /// Fix the file name of dump in owning dump folder. This is necessary because we 
        /// want to do batch import dumps in UABEA.
        /// </summary>
        private void FixSpriteDumpNames()
        {
            int numOfSuccess = 0;
            int numOfError = 0;

            // Issue: two json files might have the same basename
            // (Some base names have multiple full names)
            // Need to make sure all full names get assigned to file
            Dictionary<string, List<string>> sourceBaseFullNames = new();
            string[] sourceJsonFiles = Directory.GetFiles(_sourceFolderPath, "*.json", SearchOption.TopDirectoryOnly);

            foreach (string jsonFile in sourceJsonFiles)
            {
                JObject spriteJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (!spriteJson.ContainsKey("m_Rect")) // Not a Sprite Dump File
                    continue;
                string fileName = Path.GetFileName(jsonFile);
                (string, string) baseNameFullName = NameUtil.GetBaseNameFullNameSplitCab(fileName);
                string baseName = baseNameFullName.Item1;
                string fullName = baseNameFullName.Item2;
                if (!sourceBaseFullNames.ContainsKey(baseName))
                {
                    sourceBaseFullNames.Add(baseName, new() { fullName });
                }
                else
                {
                    sourceBaseFullNames[baseName].Add(fullName);
                }
            }

            string[] owningJsonFiles = Directory.GetFiles(_owningFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string jsonFile in owningJsonFiles)
            {
                JObject spriteJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (!spriteJson.ContainsKey("m_Rect")) // Not a Sprite Dump File
                    continue;
                string jsonFileName = Path.GetFileName(jsonFile);
                (string, long) namePathId = NameUtil.GetNamePathIDFromDumpName(jsonFileName);
                string owningName = namePathId.Item1;
                long owningPathID = namePathId.Item2;

                // ? Potential bug zone
                // Remember, the dump files we have here are from our custom bundle
                // We can potentially end up with multiple sprites having the same meta name
                // In that case, we add hashtag to the duplicates to distinguish them
                // We want to work with the basename without hashtag to match files in source dump folder
                // HOWEVER, there is a chance that in the original basename, there is hashtag...
                // Although very unlikely to happen, if it did, it will fail to update this file name
                // so you will have to restore to manual importing instead.
                (string, string) baseNameFullName = NameUtil.GetBaseNameFullNameSplitHashtag(owningName);
                string baseName = baseNameFullName.Item1;

                if (sourceBaseFullNames.ContainsKey(baseName))
                {
                    string newFileName = sourceBaseFullNames[baseName][0];
                    sourceBaseFullNames[baseName].RemoveAt(0);
                    numOfSuccess++;
                    AssetDatabase.RenameAsset(jsonFile, newFileName);
                }
                else
                {
                    Debug.LogError($"Failed to process {owningName}, Path ID: {owningPathID} (my Sprite Dump).");
                    numOfError++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Fixed {numOfSuccess} names of Sprite Dump Files in {_owningFolderPath}. Found {numOfError} error(s).");
        }
    }
}