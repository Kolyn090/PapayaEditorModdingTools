using System.IO;
using ModdingTool.Assets.Editor.Util;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ModdingTool.Assets.Editor.AudioSetter
{
    public class AudioSetterMain : EditorWindow
    {
        private AudioClip _clip;
        private string _sourceDumpPath = "";

        [MenuItem("Tools/07 Set Audio", priority = 7)]
        public static void ShowWindow()
        {
            GetWindow<AudioSetterMain>("Audio Setter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Required \n必填");

            _sourceDumpPath = DragAndDropUtil.DragAndDropFileField("Source Dump File* \n源导出文件*", _sourceDumpPath, "json");
            GUILayout.Space(5);
            GUILayout.Label("AudioClip*");
            _clip = (AudioClip)EditorGUILayout.ObjectField("音频文件*", _clip, typeof(AudioClip), false);

            GUI.enabled = !string.IsNullOrWhiteSpace(_sourceDumpPath) && _clip != null;

            GUILayout.Space(35);

            if (GUILayout.Button("Run (运行)"))
            {
                Config();
            }
            GUI.enabled = true;
        }

        private void Config()
        {
            /*
            "m_Name": source,
            "m_LoadType": source,
            "m_Channels": my,
            "m_Frequency": my,
            "m_BitsPerSample": my,
            "m_Length": my,
            "m_IsTrackerFormat": unapplicable,
            "m_Ambisonic": source,
            "m_SubsoundIndex": my,
            "m_PreloadAudioData": source,
            "m_LoadInBackground": source,
            "m_Legacy3D": unapplicable,
            "m_Resource": {
                "m_Source": "unapplicable",
                "m_Offset": unapplicable,
                "m_Size": unapplicable
            },
            "m_CompressionFormat": source
            */

            JObject sourceDumpJson = JObject.Parse(File.ReadAllText(_sourceDumpPath));
            _clip.name = sourceDumpJson["m_Name"].ToString();

            string path = AssetDatabase.GetAssetPath(_clip);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                importer.ambisonic = (bool)sourceDumpJson["m_Ambisonic"];
                importer.preloadAudioData = (bool)sourceDumpJson["m_PreloadAudioData"];
                importer.loadInBackground = (bool)sourceDumpJson["m_LoadInBackground"];

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = (AudioClipLoadType)int.Parse(sourceDumpJson["m_LoadType"].ToString());
                settings.compressionFormat = (AudioCompressionFormat)int.Parse(sourceDumpJson["m_CompressionFormat"].ToString());
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }
    }
}