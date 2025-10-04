using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ModdingTool.Assets.Editor.SpritesheetFromDumps
{
    public class DumpsImporter
    {
        private readonly string _sourceAtlasFilePath;
        private readonly string _sourceDumpsFolderPath;

        public DumpsImporter(string sourceAtlasFilePath, string sourceDumpsFolderPath)
        {
            _sourceAtlasFilePath = sourceAtlasFilePath;
            _sourceDumpsFolderPath = sourceDumpsFolderPath;
        }

        public List<SpriteMetaData> MetasFromDumps(long texturePathID)
        {
            // If atlas is used, read texture rect from Atlas Dump.
            // Otherwise, read both texture rect and offset (pivot) from Sprite Dumps.
            List<SpriteMetaData> result = new();

            Dictionary<int, int> index2ActualRenderDataKeyIndex = null;
            Dictionary<long, int> pathID2Index = null;
            JToken renderDataMaps = null;

            // Using SpriteAtlas
            if (!string.IsNullOrWhiteSpace(_sourceAtlasFilePath))
            {
                index2ActualRenderDataKeyIndex = GetIndex2ActualRenderDataKeyIndex();
                pathID2Index = GetPathID2Index();
                JObject sourceAtlasFileJson = JObject.Parse(File.ReadAllText(_sourceAtlasFilePath));
                renderDataMaps = sourceAtlasFileJson["m_RenderDataMap"]["Array"];
            }

            int counter = 0;
            HashSet<string> seenNames = new();
            string[] jsonFiles = Directory.GetFiles(_sourceDumpsFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string jsonFile in jsonFiles)
            {
                JObject spriteJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (!spriteJson.ContainsKey("m_Rect")) // Not a Sprite Dump File
                    continue;

                (string, long) namePathID = GetNamePathIDFromDumpName(jsonFile);
                // This is the name for sprite meta.
                // It must be unique within an Altas.
                string name = namePathID.Item1;
                if (seenNames.Contains(name))
                {
                    name = name + " #" + counter.ToString();
                }
                counter++;
                seenNames.Add(name);
                long pathID = namePathID.Item2;
                float pivotX = float.Parse(spriteJson["m_Pivot"]["x"].ToString());
                float pivotY = float.Parse(spriteJson["m_Pivot"]["y"].ToString());
                Rect rect;

                if (HasSpriteAtlas())
                {
                    Rect? _rect = ReadSpriteRectInAtlas(pathID2Index, index2ActualRenderDataKeyIndex, renderDataMaps, pathID, texturePathID);
                    if (_rect != null)
                        rect = (Rect)_rect;
                    else
                        continue;
                }
                else
                {
                    rect = ReadSpriteRectNoAtlas(spriteJson);
                }

                SpriteMetaData newMeta = new()
                {
                    name = name,
                    rect = rect,
                    pivot = new(pivotX, pivotY),
                    border = new(
                        float.Parse(spriteJson["m_Border"]["x"].ToString()),
                        float.Parse(spriteJson["m_Border"]["y"].ToString()),
                        float.Parse(spriteJson["m_Border"]["z"].ToString()),
                        float.Parse(spriteJson["m_Border"]["w"].ToString())
                    ),
                    alignment = (int)SpriteAlignment.Custom
                };
                Debug.Log($"Loaded new Sprite {name}.");

                result.Add(newMeta);
            }

            return result;
        }

        private Rect ReadSpriteRectNoAtlas(JObject spriteJson)
        {
            float x = float.Parse(spriteJson["m_Rect"]["x"].ToString());
            float y = float.Parse(spriteJson["m_Rect"]["y"].ToString());
            float width = float.Parse(spriteJson["m_Rect"]["width"].ToString());
            float height = float.Parse(spriteJson["m_Rect"]["height"].ToString());
            return new(x, y, width, height);
        }

        private Rect? ReadSpriteRectInAtlas(Dictionary<long, int> pathID2Index,
                                            Dictionary<int, int> index2ActualRenderDataKeyIndex,
                                            JToken renderDataMaps,
                                            long pathID,
                                            long texturePathID)
        {
            // Is using Atlas
            if (!pathID2Index.ContainsKey(pathID))
            {
                Debug.LogWarning($"Cannot convert to metas because path id {pathID} is not found in pathID2Index - Missing Index");
                return null;
            }
            int indexInAtlas = pathID2Index[pathID];
            // A.K.A. index in folder
            int actualRenderDataKeyIndex = index2ActualRenderDataKeyIndex[indexInAtlas];

            if (actualRenderDataKeyIndex < 0)
            {
                Debug.LogWarning($"Sprite dump with path id {pathID} is not in target Texture");
                return null;
            }

            var renderDataMapSource = renderDataMaps[actualRenderDataKeyIndex];

            // !!! Filter out dumps that don't belong to the current texture
            // Verify with the path id of the current texture
            long secondTexturePathID = long.Parse(renderDataMapSource["second"]["texture"]["m_PathID"].ToString());
            if (secondTexturePathID != texturePathID)
            {
                return null;
            }

            float x = float.Parse(renderDataMapSource["second"]["textureRect"]["x"].ToString());
            float y = float.Parse(renderDataMapSource["second"]["textureRect"]["y"].ToString());
            float width = float.Parse(renderDataMapSource["second"]["textureRect"]["width"].ToString());
            float height = float.Parse(renderDataMapSource["second"]["textureRect"]["height"].ToString());
            return new(x, y, width, height);
        }

        private bool HasSpriteAtlas()
        {
            return !string.IsNullOrWhiteSpace(_sourceAtlasFilePath);
        }

        // Atlas
        /// <summary>
        /// Map sprite's index in folder to index in SpriteAtlas file.
        /// </summary>
        /// <returns>
        /// A dict with key = folder index, val = SpriteAtlas index.
        /// </returns>
        private Dictionary<int, int> GetIndex2ActualRenderDataKeyIndex()
        {
            Dictionary<int, int> result = new();
            // Indexed by position in folder
            Dictionary<int, RenderDataKey> index2RenderDataKey = GetIndex2RenderDataKey();
            JObject sourceAtlasFileJson = JObject.Parse(File.ReadAllText(_sourceAtlasFilePath));
            // Indexed by index in SpriteAtlas 
            List<RenderDataKey> renderDataKeys = GetRenderDataKeysFromJObject(sourceAtlasFileJson);

            foreach (int index in index2RenderDataKey.Keys)
            {
                if (index2RenderDataKey.TryGetValue(index, out RenderDataKey rdk))
                {
                    result[index] = SearchIndexOfRenderDataKey(renderDataKeys, rdk);
                }
            }

            return result;
        }

        // Atlas
        /// <summary>
        /// Get the list of sprites' RDK from SpriteAtlas file.
        /// </summary>
        /// <param name="spriteAtlasJObject">The jObject of entire SpriteAtlas dump</param>
        /// <returns>
        /// A list of RDKs of all sprites in SpriteAtlas.
        /// </returns>
        private List<RenderDataKey> GetRenderDataKeysFromJObject(JObject spriteAtlasJObject)
        {
            var renderDataMap = spriteAtlasJObject["m_RenderDataMap"]["Array"];
            List<RenderDataKey> renderDataKeys = new();

            int success = 0;
            for (int i = 0; i < renderDataMap.Count(); i++)
            {
                var rdk = renderDataMap[i];
                uint firstData0 = uint.Parse(rdk["first"]["first"]["data[0]"].ToString());
                uint firstData1 = uint.Parse(rdk["first"]["first"]["data[1]"].ToString());
                uint firstData2 = uint.Parse(rdk["first"]["first"]["data[2]"].ToString());
                uint firstData3 = uint.Parse(rdk["first"]["first"]["data[3]"].ToString());
                long second = long.Parse(rdk["first"]["second"].ToString());
                RenderDataKey renderDataKey = new(firstData0, firstData1, firstData2, firstData3, second);
                renderDataKeys.Add(renderDataKey);
                success++;
            }

            Debug.Log($"Success: {success}; Failed: {renderDataMap.Count() - success}; Total: {renderDataMap.Count()}");

            return renderDataKeys;
        }

        // Atlas
        /// <summary>
        /// Read from the dump folder, assign index (the position in folder) and RDK of sprite.
        /// </summary>
        /// <returns>
        /// A dict with key = index, val = RDK
        /// </returns>
        private Dictionary<int, RenderDataKey> GetIndex2RenderDataKey()
        {
            Dictionary<long, int> pathID2Index = GetPathID2Index();
            Dictionary<int, RenderDataKey> result = new();
            string[] jsonFiles = Directory.GetFiles(_sourceDumpsFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            int failed = 0;
            foreach (string jsonFile in jsonFiles)
            {
                string jsonFileName = Path.GetFileName(jsonFile);
                (string, long) namePathId = GetNamePathIDFromDumpName(jsonFileName);
                long pathID = namePathId.Item2;
                JObject spriteJson = JObject.Parse(File.ReadAllText(jsonFile));
                if (spriteJson.ContainsKey("m_Rect"))
                {
                    var rdk = spriteJson["m_RenderDataKey"];
                    uint firstData0 = uint.Parse(rdk["first"]["data[0]"].ToString());
                    uint firstData1 = uint.Parse(rdk["first"]["data[1]"].ToString());
                    uint firstData2 = uint.Parse(rdk["first"]["data[2]"].ToString());
                    uint firstData3 = uint.Parse(rdk["first"]["data[3]"].ToString());
                    long second = long.Parse(rdk["second"].ToString());
                    RenderDataKey renderDataKey = new(firstData0, firstData1, firstData2, firstData3, second);

                    if (pathID2Index.ContainsKey(pathID))
                    {
                        result[pathID2Index[pathID]] = renderDataKey;
                    }
                    else
                    {
                        failed++;
                        Debug.LogWarning($"Path ID {pathID} not found in Atlas.");
                        continue;
                    }
                }
            }
            Debug.LogWarning($"Successfully processed {jsonFiles.Length - failed} number of json files; Failed to process {failed} number of json dumps; Total: {jsonFiles.Length}");
            return result;
        }

        /// <summary>
        /// ! Assuming Dump Json File From UABEA
        /// ! Only Work If Dump Json File Name Has Not Been Changed
        /// Read the base name and the path id of dump.
        /// </summary>
        /// <param name="dumpName">The file name of dump json</param>
        /// <returns>
        /// A tuple with item0 = dump base name, item1 = dump path id
        /// </returns>
        private (string, long) GetNamePathIDFromDumpName(string dumpName)
        {
            string[] splitCab = Path.GetFileName(dumpName).Replace(".json", "").Split("-CAB-");
            string name = splitCab[0];
            string[] splitted = splitCab[1].Split("-");
            long absPathID = long.Parse(splitted[splitted.Length - 1].ToString());
            long pathID = absPathID;
            if (splitted.Length == 3)
            {  // This Path ID is negative
                pathID = -absPathID;
            }
            else if (splitted.Length != 2)
            {
                Debug.LogError($"{name} does not fit format.");
                pathID = -1;
            }
            return new(name, pathID);
        }

        // Atlas
        /// <summary>
        /// Get a dict mapping path id to index.
        /// </summary>
        /// <returns>
        /// A dict with key = sprite's path id, val = index
        /// </returns>
        private Dictionary<long, int> GetPathID2Index()
        {
            Dictionary<int, long> index2PathID = GetIndex2PathID();
            return index2PathID.ToDictionary(pair => pair.Value, pair => pair.Key);
        }

        // Atlas
        /// <summary>
        /// Read from source SpriteAtlas file, for each sprite, read its path id.
        /// The index is the same as the index of sprite in SpriteAtlas.
        /// </summary>
        /// <returns>
        /// A dict with key = index, val = sprite's path id
        /// </returns>
        private Dictionary<int, long> GetIndex2PathID()
        {
            Dictionary<int, long> result = new();
            JObject sourceAtlasFileJson = JObject.Parse(File.ReadAllText(_sourceAtlasFilePath));
            var packedSpritesSource = sourceAtlasFileJson["m_PackedSprites"]["Array"];
            for (int i = 0; i < packedSpritesSource.Count(); i++)
            {
                var pathID = long.Parse(packedSpritesSource[i]["m_PathID"].ToString());
                result[i] = pathID;
            }
            return result;
        }

        // Atlas
        /// <summary>
        /// Find the index of the given RDK.
        /// </summary>
        /// <param name="lst"> RDK list </param>
        /// <param name="target"> The target RDK </param>
        /// <returns> 
        /// The index of target RDK, or -1 if not found in list.
        /// </returns>
        private static int SearchIndexOfRenderDataKey(List<RenderDataKey> lst, RenderDataKey target)
        {
            int counter = 0;
            foreach (RenderDataKey rdk in lst)
            {
                if (rdk.Equals(target))
                {
                    return counter;
                }
                counter++;
            }
            return -1;
        }
    }
}
