using System.IO;
using ModdingTool.Assets.Editor.Util;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ModdingTool.Assets.Editor.AudioReplacer
{
    public class AudioReplacerMain : EditorWindow
    {
        private string _owningDumpPath = "";
        private string _sourceDumpPath = "";

        [MenuItem("Tools/08 Replace Audio", priority = 8)]
        public static void ShowWindow()
        {
            GetWindow<AudioReplacerMain>("Audio Replacer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Required \n必填", EditorStyles.boldLabel);

            _sourceDumpPath = DragAndDropUtil.DragAndDropFileField("Source Dump File* \n源导出文件*", _sourceDumpPath, "json");
            GUILayout.Space(5);
            _owningDumpPath = DragAndDropUtil.DragAndDropFileField("My Dump File* \n我的导出文件*", _owningDumpPath, "json");

            GUILayout.Space(35);

            GUI.enabled = !string.IsNullOrWhiteSpace(_sourceDumpPath) && !string.IsNullOrWhiteSpace(_owningDumpPath);
            if (GUILayout.Button("Run (运行)"))
            {
                ReplaceDump();
            }
            GUI.enabled = true;
        }

        private void ReplaceDump()
        {
            /*
            "m_Name": source,
            "m_LoadType": source,
            "m_Channels": my,
            "m_Frequency": my,
            "m_BitsPerSample": my,
            "m_Length": my,
            "m_IsTrackerFormat": source,
            "m_Ambisonic": source,
            "m_SubsoundIndex": my,
            "m_PreloadAudioData": source,
            "m_LoadInBackground": source,
            "m_Legacy3D": source,
            "m_Resource": {
                "m_Source": "archive:/CAB-[source]/[my].resource",
                "m_Offset": my,
                "m_Size": my
            },
            "m_CompressionFormat": source
            */

            JObject sourceDumpJson = JObject.Parse(File.ReadAllText(_sourceDumpPath));
            JObject owningDumpJson = JObject.Parse(File.ReadAllText(_owningDumpPath));

            owningDumpJson["m_Name"] = sourceDumpJson["m_Name"];
            owningDumpJson["m_LoadType"] = sourceDumpJson["m_LoadType"];
            owningDumpJson["m_IsTrackerFormat"] = sourceDumpJson["m_IsTrackerFormat"];
            owningDumpJson["m_Ambisonic"] = sourceDumpJson["m_Ambisonic"];
            owningDumpJson["m_PreloadAudioData"] = sourceDumpJson["m_PreloadAudioData"];
            owningDumpJson["m_LoadInBackground"] = sourceDumpJson["m_LoadInBackground"];
            owningDumpJson["m_Legacy3D"] = sourceDumpJson["m_Legacy3D"];
            owningDumpJson["m_CompressionFormat"] = sourceDumpJson["m_CompressionFormat"];
            owningDumpJson["m_Resource"]["m_Source"] = GetNewReS(
                Path.GetFileNameWithoutExtension(sourceDumpJson["m_Resource"]["m_Source"].ToString()),
                Path.GetFileNameWithoutExtension(owningDumpJson["m_Resource"]["m_Source"].ToString())
            );

            File.WriteAllText(_owningDumpPath, owningDumpJson.ToString());
        }

        private string GetNewReS(string sourceCabString, string owningCabString)
        {
            return $"archive:/{sourceCabString}/{owningCabString}.resource";
        }
    }
}