using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ModdingTool.Assets.Editor.Util
{
    public class NameUtil
    {
        /// <summary>
        /// ! Assuming Dump Json File From UABEA
        /// ! Only Work If Dump Json File Name Has Not Been Changed
        /// Read the base name and the path id of dump.
        /// </summary>
        /// <param name="dumpName">The file name of dump json</param>
        /// <returns>
        /// A tuple with item0 = dump base name, item1 = dump path id
        /// </returns>
        public static (string, long) GetNamePathIDFromDumpName(string dumpName)
        {
            // Just in case some crazy dev named assets containing "-CAB-"
            (string, string) splitCab = SplitByLastRegex(Path.GetFileName(dumpName).Replace(".json", ""), "-CAB-");
            string name = splitCab.Item1;
            string[] splitted = splitCab.Item2.Split("-");
            long absPathID = long.Parse(splitted[^1].ToString());
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

        /// <summary>
        /// ! Assuming Dump Json File From UABEA
        /// ! Only Work If Dump Json File Name Has Not Been Changed
        /// Read the base name and the name of dump, split by "-CAB-"
        /// </summary>
        /// <param name="dumpName">The file name of dump json</param>
        /// <returns>
        /// A tuple with item0 = dump base name, item1 = dump name
        /// </returns>
        public static (string, string) GetBaseNameFullNameSplitCab(string dumpName)
        {
            // Just in case some crazy dev named assets containing "-CAB-"
            (string, string) splitCab = SplitByLastRegex(Path.GetFileName(dumpName).Replace(".json", ""), "-CAB-");
            return new(splitCab.Item1, dumpName);
        }

        public static (string, string) GetBaseNameFullNameSplitHashtag(string fileName)
        {
            (string, string) splitHashtag = SplitByLastRegex(fileName, " #");
            return new(splitHashtag.Item1, splitHashtag.Item2);
        }

        private static (string, string) SplitByLastRegex(string input, string pattern)
        {
            MatchCollection matches = Regex.Matches(input, pattern);
            if (matches.Count == 0)
                return (input, ""); // No match, return full string and empty

            Match lastMatch = matches[^1];
            int splitIndex = lastMatch.Index;

            return (input[..splitIndex], input[splitIndex..]);
        }
    }
}