using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ModdingTool.Assets.Editor.Util;
using UnityEditor;
using UnityEngine;

using GameOption = ModdingTool.Assets.Editor.SpritesheetFromDumps.GamePPU.GameOption;

namespace ModdingTool.Assets.Editor.SpritesheetFromDumps
{
    public class SpritesheetFromDumpsMain : EditorWindow
    {
        private string _sourceDumpsFolderPath = "";
        private Texture2D _targetTexture;
        private string _sourceAtlasFilePath = ""; // if used
        private readonly GamePPU _gamePPU = new();

        [MenuItem("Tools/01 Build Spritesheet From Dumps", priority = 1)]
        public static void ShowWindow()
        {
            GetWindow<SpritesheetFromDumpsMain>("Update build Spritesheet From Dumps");
        }

        private void OnEnable()
        {
            _gamePPU.Option = (GameOption)EditorPrefs.GetInt("MyEnumWindow_Selected", (int)GameOption.战魂铭人);
        }

        private void OnDisable()
        {
            EditorPrefs.SetInt("MyEnumWindow_Selected", (int)_gamePPU.Option);
        }

        private void OnGUI()
        {
            GUIStyle style = new(GUI.skin.label)
            {
                wordWrap = true
            };

            GUILayout.Label("Required \n必填", EditorStyles.boldLabel);

            GUILayout.Label("Target Texture*");
            _targetTexture = (Texture2D)EditorGUILayout.ObjectField("材质文件*", _targetTexture, typeof(Texture2D), false);
            _sourceDumpsFolderPath = DragAndDropUtil.DragAndDropFolderField("Source Sprite Dump Folder (Json files)* \n源图像导出文件夹 (Json文件)*", _sourceDumpsFolderPath);

            GUILayout.Space(35);

            GUILayout.Label("Optional \n选填", EditorStyles.boldLabel);

            GUILayout.Label("Don't fill if Source Sprite Dump Folder contains this file. Also don't fill if not using Atlas.", style);
            GUILayout.Label("如果源图像导出文件夹中含有该文件则不用填。如果没有使用自动图集也不要填。", style);

            GUILayout.Space(5);

            _sourceAtlasFilePath = DragAndDropUtil.DragAndDropFileField("Source SpriteAtlas File \n源自动图集的导出文件", _sourceAtlasFilePath, "json");

            _gamePPU.Option = (GameOption)EditorGUILayout.EnumPopup("游戏选项", _gamePPU.Option);

            GUILayout.Space(5);

            GUI.enabled = _targetTexture != null && !string.IsNullOrWhiteSpace(_sourceDumpsFolderPath);

            if (GUILayout.Button("Run (运行)") && _sourceDumpsFolderPath != null)
            {
                _sourceAtlasFilePath = GetSourceAtlasFilePath();
                if (string.IsNullOrWhiteSpace(_sourceAtlasFilePath))
                    Debug.LogError("Could not find Source Atlas Dump File. Please make sure you assign one.");

                Import();
            }

            GUI.enabled = true;

            GUILayout.Space(35);

            string messageEn = "";
            string messageZh = "";
            GUILayout.Label(messageEn, style);
            GUILayout.Label(messageZh, style);
        }

        private string GetSourceAtlasFilePath()
        {
            if (!string.IsNullOrWhiteSpace(_sourceAtlasFilePath))
            {
                return _sourceAtlasFilePath;
            }
            return GetFileUtil.GetFileByFieldKey(_sourceDumpsFolderPath, "m_PackedSprites");
        }

        private void Import()
        {
            DumpsImporter dumpsImporter = new(_sourceAtlasFilePath, _sourceDumpsFolderPath);
            long texturePathID = GetTexturePathID();
            List<SpriteMetaData> metas = dumpsImporter.MetasFromDumps(texturePathID);
            SpriteMetaData[] newMetas = metas.ToArray();
            string assetPath = AssetDatabase.GetAssetPath(_targetTexture);
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti != null)
            {
                ti = AutoConfigureInspector(ti);
                ti.spriteImportMode = SpriteImportMode.Multiple;
                ti.spritesheet = newMetas;
                Debug.Log($"Current number of Sprites in the sheet: {metas.Count}.");
                EditorUtility.SetDirty(ti);
                ti.SaveAndReimport();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                CleanMetaNameFileIdTable(assetPath);
            }
            else
            {
                Debug.LogError($"Texture not found in {_targetTexture}.");
            }
        }

        private TextureImporter AutoConfigureInspector(TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;

            TextureImporterSettings textureSettings = new();
            ti.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            textureSettings.spriteGenerateFallbackPhysicsShape = false;

            ti.SetTextureSettings(textureSettings);
            ti.isReadable = true;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti = ChangeTexturePPU(ti);

            return ti;
        }

        private TextureImporter ChangeTexturePPU(TextureImporter ti)
        {
            int newPPU = _gamePPU.GetPPU(_gamePPU.Option);
            ti.spritePixelsPerUnit = newPPU;
            return ti;
        }

        private static void CleanMetaNameFileIdTable(string assetPath)
        {
            string metaPath = assetPath + ".meta";

            if (!File.Exists(metaPath))
            {
                Debug.LogError("Meta file does not exist: " + metaPath);
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogError("TextureImporter not valid or not in Multiple mode.");
                return;
            }

            // Get current sprite names
            HashSet<string> validNames = new();
            foreach (var meta in importer.spritesheet)
            {
                validNames.Add(meta.name);
            }

            // Read and process meta file lines
            string[] lines = File.ReadAllLines(metaPath);
            List<string> newLines = new();
            bool insideTable = false;
            int removed = 0;

            foreach (var line in lines)
            {
                if (line.Trim() == "nameFileIdTable:")
                {
                    insideTable = true;
                    newLines.Add(line);
                    continue;
                }

                if (insideTable)
                {
                    Match match = Regex.Match(line, @"^\s+(.+?):\s*(-?\d+)");
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value;
                        if (validNames.Contains(key))
                            newLines.Add(line);
                        else
                            removed++;
                    }
                    else if (line.StartsWith("  ")) // Still possibly inside block
                    {
                        newLines.Add(line);
                    }
                    else
                    {
                        insideTable = false;
                        newLines.Add(line);
                    }
                }
                else
                {
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(metaPath, newLines);
            Debug.Log($"Cleaned {removed} dangling entries from nameFileIdTable in: {metaPath}");

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private long GetTexturePathID()
        {
            static long? GetPathIDFromDumpName(string nameNotExt)
            {
                string input = nameNotExt;
                Match match = Regex.Match(input, @"-(?<num>-?\d+)$");
                if (match.Success)
                {
                    if (long.TryParse(match.Groups["num"].Value, out long number))
                    {
                        return number;
                    }
                }
                return null;
            }

            string assetPath = AssetDatabase.GetAssetPath(_targetTexture);
            string noExt = Path.GetFileNameWithoutExtension(assetPath);
            long? _pathID = GetPathIDFromDumpName(noExt);
            if (_pathID == null)
            {
                Debug.LogError("You should put texture from UABEA and no renaming!");
                return 0;
            }
            long pathID = (long)_pathID;
            return pathID;
        }
    }
}
