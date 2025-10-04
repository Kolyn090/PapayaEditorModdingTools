using System.IO;
using Newtonsoft.Json.Linq;

namespace ModdingTool.Assets.Editor.Util
{
    public class GetFileUtil
    {
        public static string GetFileByFieldKey(string searchFolder, string fieldKey)
        {
            string[] jsonFiles = Directory.GetFiles(searchFolder, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string jsonFile in jsonFiles)
            {
                JObject dumpJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (dumpJson.ContainsKey(fieldKey))
                {
                    return jsonFile;
                }
            }
            return "";
        }
    }
}
