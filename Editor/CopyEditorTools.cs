using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class CopyEditorTools
{
    [MenuItem("Tools/Update Papaya Editor Modding Tools", priority = 0)]
    public static void ForceUpdate()
    {
        CopyFiles();
    }

    // static CopyEditorTools()
    // {
    //     // Run after domain reload (on project load)
    //     EditorApplication.delayCall += CopyFiles;
    // }

    private static void CopyFiles()
    {
        string packageCache = Path.Combine(Application.dataPath, "../Library/PackageCache");
        string packagePrefix = "com.kolyn090.papaya-editor-modding-tools";

        // Find the folder that starts with the package name
        string[] matches = Directory.GetDirectories(packageCache, packagePrefix + "*", SearchOption.TopDirectoryOnly);
        if (matches.Length == 0)
        {
            Debug.LogWarning("Package folder not found in PackageCache!");
            return;
        }
        string sourceFolder = Path.Combine(matches[0], "Editor");

        string targetFolder = Path.Combine(Application.dataPath, "Editor");

        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogWarning("Source Editor folder not found!");
            return;
        }

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        foreach (var file in Directory.GetFiles(sourceFolder, "*.cs", SearchOption.AllDirectories))
        {
            string relativePath = file.Substring(sourceFolder.Length + 1);
            string destPath = Path.Combine(targetFolder, relativePath);

            string destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            File.Copy(file, destPath, true);
        }

        AssetDatabase.Refresh();
        Debug.Log("Editor scripts copied from package to Assets/EditorTools/");
    }
}