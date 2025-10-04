using System.IO;
using UnityEditor;
using UnityEngine;

namespace ModdingTool.Assets.Editor.Util
{
    public class DragAndDropUtil
    {
        public static string DragAndDropFileField(string label, string path, string requiredExtension = null)
        {
            GUILayout.Label(label);
            Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, string.IsNullOrEmpty(path) ? "Drag file here or browse... \n拖放文件或搜索..." : path);

            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                    {
                        string draggedPath = AssetDatabase.GetAssetPath(obj);
                        string fullPath = Path.GetFullPath(draggedPath);

                        if (File.Exists(fullPath))
                        {
                            if (requiredExtension == null ||
                            fullPath.ToLower().EndsWith("." + requiredExtension))
                            {
                                path = fullPath;
                                GUI.changed = true;
                                break;
                            }
                        }
                    }
                }
                evt.Use();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse (搜索)"))
            {
                string directory = string.IsNullOrEmpty(path) ? "" : Path.GetDirectoryName(path);
                string selected = EditorUtility.OpenFilePanel("Select " + label, directory, "");
                if (!string.IsNullOrEmpty(selected))
                    path = selected;
            }
            GUILayout.EndHorizontal();

            return path;
        }

        public static string DragAndDropFolderField(string label, string path)
        {
            GUILayout.Label(label);
            Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, string.IsNullOrEmpty(path) ? "Drag folder here or browse... \n拖放文件夹或搜索..." : path);

            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var draggedPath in DragAndDrop.paths)
                    {
                        if (Directory.Exists(draggedPath))
                        {
                            path = draggedPath;
                            GUI.changed = true;
                            break;
                        }
                    }
                }
                evt.Use();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse (搜索)"))
            {
                string selected = EditorUtility.OpenFolderPanel("Select " + label, path, "");
                if (!string.IsNullOrEmpty(selected))
                    path = selected;
            }
            GUILayout.EndHorizontal();

            return path;
        }
    }
}