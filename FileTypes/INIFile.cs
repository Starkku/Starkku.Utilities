/*
 * Copyright © 2017-2020 Starkku
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Starkku.Utilities.ExtensionMethods;

namespace Starkku.Utilities.FileTypes
{
    /// <summary>
    /// INI file class.
    /// </summary>
    public class INIFile
    {
        /// <summary>
        /// Gets or sets filename for the INI file.
        /// </summary>
        public string Filename { get; set; }

        protected bool _altered = false;

        /// <summary>
        /// Gets whether or not any INI sections, keys or values have been altered since last load or save.
        /// </summary>
        public bool Altered => _altered;

        private readonly List<INISection> iniSections = new List<INISection>();

        /// <summary>
        /// Creates a new INI file and attempts to parse it from the specified file.
        /// </summary>
        /// <param name="filename">Filename of INI file to load.</param>
        public INIFile(string filename)
        {
            Filename = filename;
            Parse(Filename);
        }

        /// <summary>
        /// Reload the INI file using currently set filename, resetting all current section and key data to as they are in the loaded file.
        /// </summary>
        /// <returns>Error message if something went wrong, null otherwise.</returns>
        public virtual string Reload()
        {
            _altered = false;
            return Parse(Filename, true);
        }

        /// <summary>
        /// Load the INI file from specified filename, resetting all current section and key data to as they are in the loaded file.
        /// </summary>
        /// <returns>Error message if something went wrong, null otherwise.</returns>
        public virtual string Load(string filename)
        {
            _altered = false;
            return Parse(filename, true);
        }

        /// <summary>
        /// Attempts to parse the INI file from a file with specified filename.
        /// </summary>
        /// <param name="filename">Filename of the INI file to parse.</param>
        /// <param name="clearExistingSections">If set, removes all existing INI sections & keys before parsing INI file.</param>
        /// <returns>Error message if something went wrong, null otherwise.</returns>
        private string Parse(string filename, bool clearExistingSections = false)
        {
            string[] lines = null;

            try
            {
                lines = File.ReadAllLines(filename);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            if (clearExistingSections)
                iniSections.Clear();

            INISection currentSection = null;
            INIKeyValuePair lastLine = null;
            bool lastLineWasWhiteSpace = false;
            bool lastLineWasSection = false;
            List<INIComment> comments = new List<INIComment>();

            foreach (string line in lines)
            {
                string lineTrimmed = line.Trim();

                if (string.IsNullOrEmpty(lineTrimmed))
                {
                    lastLineWasWhiteSpace = true;

                    if (lastLineWasSection)
                        currentSection.EmptyLineCount++;
                    else if (lastLine != null)
                        lastLine.EmptyLineCount++;

                    continue;
                }

                if (lineTrimmed.StartsWith(";"))
                {
                    int whiteSpaceCount = line.Length - line.TrimStart().Length;
                    if (lastLineWasWhiteSpace || currentSection == null)
                        comments.Add(new INIComment(lineTrimmed.ReplaceFirst(";", ""), INICommentPosition.Before, whiteSpaceCount));
                    else if (lastLineWasSection)
                        currentSection.AddComment(lineTrimmed.ReplaceFirst(";", ""), INICommentPosition.After, whiteSpaceCount);
                    else if (lastLine != null)
                        lastLine.AddComment(lineTrimmed.ReplaceFirst(";", ""), INICommentPosition.After, whiteSpaceCount);

                    continue;
                }

                if (lineTrimmed.StartsWith("[") && lineTrimmed.Contains("]"))
                {
                    currentSection = new INISection();

                    int start = lineTrimmed.IndexOf('[') + 1;
                    int end = lineTrimmed.IndexOf(']') - 1;
                    currentSection.Name = lineTrimmed.Substring(start, end);
                    INISection sectionMatch = iniSections.Find(x => x.Name == currentSection.Name);

                    if (lineTrimmed.Contains(";"))
                    {
                        string commentLine = line.Replace("[" + currentSection.Name + "]", "");
                        int whiteSpaceCount = commentLine.Length - commentLine.TrimStart().Length;
                        string comment = commentLine.Trim().TrimStart(';');
                        currentSection.AddComment(comment, INICommentPosition.Middle, whiteSpaceCount);
                    }

                    if (sectionMatch == null)
                        iniSections.Add(currentSection);
                    else
                        currentSection = sectionMatch;

                    foreach (INIComment comment in comments)
                    {
                        if (!currentSection.HasComment(comment))
                            currentSection.AddComment(comment);
                    }

                    comments.Clear();
                    lastLineWasSection = true;
                    lastLine = null;
                }
                else
                {
                    lastLineWasSection = false;

                    // Ignore non-comment content before the first section.
                    if (currentSection != null)
                        lastLine = AddKeyValuePairFromLine(currentSection, line.Trim());
                    else
                        lastLine = null;
                }

                lastLineWasWhiteSpace = false;
            }

            _altered = false;
            return null;
        }

        /// <summary>
        /// Saves the INI file with currently set filename.
        /// </summary>
        /// <param name="preserveEmptyLines">If set, any empty lines in the original INI file are preserved.</param>
        /// <param name="saveComments">If set, comments are saved in INI file.</param>
        /// <returns>Error message if something went wrong, null otherwise.</returns>
        public virtual string Save(bool preserveEmptyLines = true, bool saveComments = true)
        {
            return Save(Filename, preserveEmptyLines, saveComments);
        }

        /// <summary>
        /// Saves the INI file with specified filename.
        /// </summary>
        /// <param name="filename">Filename to save the INI file to.</param>
        /// <param name="preserveEmptyLines">If set, any empty lines in the original INI file are preserved.</param>
        /// <param name="saveComments">If set, comments are saved in INI file.</param>
        /// <returns>Error message if something went wrong, null otherwise.</returns>
        public virtual string Save(string filename, bool preserveEmptyLines = true, bool saveComments = true)
        {
            return Save(filename, preserveEmptyLines ? null : new string[0], saveComments);
        }

        /// <summary>
        /// Saves the INI file with specified filename.
        /// </summary>
        /// <param name="filename">Filename to save the INI file to.</param>
        /// <param name="preserveEmptyLinesSectionNames">Collection of INI file section names for which empty lines will be preserved. If set to null, all will be preserved.</param>
        /// <param name="saveComments">If set, comments are saved in INI file.</param>
        /// <returns>Error message if something went wrong, null otherwise.</returns>
        public virtual string Save(string filename, IEnumerable<string> preserveEmptyLinesSectionNames, bool saveComments = true)
        {
            if (string.IsNullOrEmpty(filename))
                filename = Filename;

            List<string> lines = new List<string>();

            foreach (INISection sec in iniSections)
            {
                if (saveComments)
                {
                    var sectionCommentsBefore = sec.GetAllCommentsAtPosition(INICommentPosition.Before);
                    foreach (INIComment comment in sectionCommentsBefore)
                        lines.Add(comment.GetINIText());
                }

                var sectionCommentsMiddle = sec.GetAllCommentsAtPosition(INICommentPosition.Middle);
                string sectionComment = sectionCommentsMiddle.Count > 0 && saveComments ? sectionCommentsMiddle[0].GetINIText() : "";

                lines.Add("[" + sec.Name + "]" + sectionComment);

                if (saveComments)
                {
                    var sectionCommentsAfter = sec.GetAllCommentsAtPosition(INICommentPosition.After);
                    foreach (INIComment comment in sectionCommentsAfter)
                        lines.Add(comment.GetINIText());
                }

                if (preserveEmptyLinesSectionNames == null || preserveEmptyLinesSectionNames.Contains(sec.Name))
                {
                    for (int i = 0; i < sec.EmptyLineCount; i++)
                    {
                        lines.Add("");
                    }
                }

                INIKeyValuePair lastLine = null;

                for (int index = 0; index < sec.KeyValuePairs.Count; index++)
                {
                    INIKeyValuePair kvp = sec.KeyValuePairs[index];
                    lastLine = kvp;

                    if (saveComments)
                    {
                        var commentsBefore = kvp.GetAllCommentsAtPosition(INICommentPosition.Before);
                        foreach (INIComment comment in commentsBefore)
                            lines.Add(comment.GetINIText());
                    }

                    var commentsMiddle = kvp.GetAllCommentsAtPosition(INICommentPosition.Middle);
                    string lineComment = commentsMiddle.Count > 0 && saveComments ? commentsMiddle[0].GetINIText() : "";

                    if (kvp.Value != null)
                        lines.Add(kvp.Key + "=" + kvp.Value + lineComment);
                    else
                        lines.Add(kvp.Key + lineComment);

                    if (saveComments)
                    {
                        var commentsAfter = kvp.GetAllCommentsAtPosition(INICommentPosition.After);
                        foreach (INIComment comment in commentsAfter)
                            lines.Add(comment.GetINIText());
                    }

                    if (preserveEmptyLinesSectionNames == null || preserveEmptyLinesSectionNames.Contains(sec.Name))
                    {
                        for (int i = 0; i < kvp.EmptyLineCount; i++)
                        {
                            lines.Add("");
                        }
                    }
                    else if (preserveEmptyLinesSectionNames != null && !preserveEmptyLinesSectionNames.Contains(sec.Name) &&
                         index == sec.KeyValuePairs.Count - 1)
                    {
                        lines.Add("");
                    }
                }
            }

            try
            {
                File.WriteAllLines(filename, lines.ToArray());
                _altered = false;
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        /// <summary>
        /// Gets INI key-value pair from line of text and adds it to the specified section.
        /// </summary>
        /// <param name="section">INI section to add the key-value pair to.</param>
        /// <param name="line">Line of text.</param>
        /// <returns>INI key-value pair that was added to the section.</returns>
        private INIKeyValuePair AddKeyValuePairFromLine(INISection section, string line)
        {
            string key = null;
            string value = null;
            string comments = null;
            int whiteSpaceCount = 0;
            string lineMod = line;

            if (lineMod.Contains(";"))
            {
                int index = lineMod.IndexOf(';');
                comments = lineMod.Substring(index, line.Length - index);
                lineMod = line.Replace(comments, "").Trim();
                whiteSpaceCount = line.Replace(lineMod, "").Replace(comments, "").Length;
                comments = comments.ReplaceFirst(";", "");
            }

            if (line.Contains("="))
            {
                key = lineMod.Substring(0, lineMod.IndexOf('=')).Trim();
                value = lineMod.Substring(lineMod.IndexOf('=') + 1, lineMod.Length - lineMod.IndexOf('=') - 1).Trim();
            }
            else
                key = lineMod.Trim();

            INIKeyValuePair kvp = new INIKeyValuePair(key, value);
            INIKeyValuePair kvpMatch = section.KeyValuePairs.Find(x => x.Key == kvp.Key);

            if (kvpMatch == null)
            {
                section.KeyValuePairs.Add(kvp);
            }
            else
            {
                kvpMatch.Value = kvp.Value;
                kvp = kvpMatch;
            }

            if (comments != null)
            {
                INIComment iniComment = new INIComment(comments, INICommentPosition.Middle, whiteSpaceCount);

                if (!kvp.HasComment(iniComment))
                    kvp.AddComment(iniComment);
            }

            return kvp;
        }

        /// <summary>
        /// Get value of INI key as a string from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="defaultValue">Default value returned if section, key or value was not found.</param>
        /// <returns>Returns the value of the key as a string if successful, otherwise <paramref name="defaultValue"/> is returned.</returns>
        public string GetKey(string sectionName, string keyName, string defaultValue)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return defaultValue;

            INIKeyValuePair kvp = section.KeyValuePairs.Find(i => i.Key == keyName);

            if (kvp == null)
                return defaultValue;

            return kvp.Value;
        }

        /// <summary>
        /// Get value of INI key as a boolean from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="defaultValue">Default value returned if section, key or value was not found.</param>
        /// <returns>Returns the value of the key as a boolean if successful, otherwise <paramref name="defaultValue"/> is returned.</returns>
        public bool GetKeyAsBool(string sectionName, string keyName, bool defaultValue)
        {
            return Conversion.GetBoolFromString(GetKey(sectionName, keyName, defaultValue.ToString()), defaultValue);
        }

        /// <summary>
        /// Get value of INI key as a byte from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="defaultValue">Default value returned if section, key or value was not found.</param>
        /// <returns>Returns the value of the key as a byte if successful, otherwise <paramref name="defaultValue"/> is returned.</returns>
        public byte GetKeyAsByte(string sectionName, string keyName, byte defaultValue)
        {
            return Conversion.GetByteFromString(GetKey(sectionName, keyName, defaultValue.ToString()), defaultValue);
        }

        /// <summary>
        /// Get value of INI key as a short integer from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="defaultValue">Default value returned if section, key or value was not found.</param>
        /// <returns>Returns the value of the key as a short integer if successful, otherwise <paramref name="defaultValue"/> is returned.</returns>
        public short GetKeyAsShort(string sectionName, string keyName, short defaultValue)
        {
            return Conversion.GetShortFromString(GetKey(sectionName, keyName, defaultValue.ToString()), defaultValue);
        }

        /// <summary>
        /// Get value of INI key as an integer from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="defaultValue">Default value returned if section, key or value was not found.</param>
        /// <returns>Returns the value of the key as an integer if successful, otherwise <paramref name="defaultValue"/> is returned.</returns>
        public int GetKeyAsInt(string sectionName, string keyName, int defaultValue)
        {
            return Conversion.GetIntFromString(GetKey(sectionName, keyName, defaultValue.ToString()), defaultValue);
        }

        /// <summary>
        /// Get value of INI key as a long integer from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="defaultValue">Default value returned if section, key or value was not found.</param>
        /// <returns>Returns the value of the key as a long integer if successful, otherwise <paramref name="defaultValue"/> is returned.</returns>
        public long GetKeyAsLong(string sectionName, string keyName, long defaultValue)
        {
            return Conversion.GetLongFromString(GetKey(sectionName, keyName, defaultValue.ToString()), defaultValue);
        }

        /// <summary>
        /// Get value of INI key as a single-precision floating point number from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="defaultValue">Default value returned if section, key or value was not found.</param>
        /// <returns>Returns the value of the key as a single-precision floating point number if successful, otherwise <paramref name="defaultValue"/> is returned.</returns>
        public float GetKeyAsFloat(string sectionName, string keyName, float defaultValue)
        {
            return Conversion.GetFloatFromString(GetKey(sectionName, keyName, defaultValue.ToString()), defaultValue);
        }

        /// <summary>
        /// Get value of INI key as a double-precision floating point number from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="defaultValue">Default value returned if section, key or value was not found.</param>
        /// <returns>Returns the value of the key as a double-precision floating point number if successful, otherwise <paramref name="defaultValue"/> is returned.</returns>
        public double GetKeyAsDouble(string sectionName, string keyName, double defaultValue)
        {
            return Conversion.GetDoubleFromString(GetKey(sectionName, keyName, defaultValue.ToString()), defaultValue);
        }

        /// <summary>
        /// Set value of INI key in a INI file section. If section or key do not exist, they are created.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="value">The new value of the key.</param>
        public void SetKey(string sectionName, string keyName, string value)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
            {
                AddSection(sectionName);
                section = iniSections.Find(i => i.Name == sectionName);
            }

            INIKeyValuePair kvp = section.KeyValuePairs.Find(i => i.Key == keyName);

            if (kvp == null)
                section.KeyValuePairs.Add(new INIKeyValuePair(keyName, value));
            else
                kvp.Value = value;

            _altered = true;
        }

        /// <summary>
        /// Removes INI key from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <returns>True if key was removed, otherwise false.</returns>
        public bool RemoveKey(string sectionName, string keyName)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return false;

            INIKeyValuePair kvp = section.KeyValuePairs.Find(i => i.Key == keyName);

            if (section.KeyValuePairs.Remove(kvp))
            {
                _altered = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Renames INI key in a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="newKeyName">New name of the INI key.</param>
        /// <returns>True if key was renamed, otherwise false.</returns>
        /// 
        public bool RenameKey(string sectionName, string keyName, string newKeyName)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return false;

            INIKeyValuePair kvp = section.KeyValuePairs.Find(i => i.Key == keyName);

            if (kvp != null)
            {
                kvp.Key = newKeyName;
                _altered = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if section with a specific name exists.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <returns>True if section with name exists, false if not or if the INI file has not been initialized.</returns>
        public bool SectionExists(string sectionName)
        {
            return iniSections.Find(i => i.Name == sectionName) != null;
        }

        /// <summary>
        /// Returns an array of strings containing values of all keys in a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <returns>Array of strings containing values of the keys in INI file section. Null if INI file has not been initialized or section was not found.</returns>
        public string[] GetValues(string sectionName)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return null;

            string[] values = new string[section.KeyValuePairs.Count];
            int c = 0;

            foreach (INIKeyValuePair kvp in section.KeyValuePairs)
            {
                values[c++] = kvp.Value;
            }

            return values;
        }

        /// <summary>
        /// Returns an array of strings containing names of all keys in a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <returns>Array of strings containing names of the keys in INI file section. Null if INI file has not been initialized or section was not found.</returns>
        public string[] GetKeys(string sectionName)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return null;

            string[] keys = new string[section.KeyValuePairs.Count];
            int c = 0;

            foreach (INIKeyValuePair kvp in section.KeyValuePairs)
            {
                keys[c++] = kvp.Key;
            }

            return keys;
        }

        /// <summary>
        /// Gets whether or not INI file section contains specified key.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <returns>True if INI file section exists and contains specified key, otherwise false.</returns>
        /// 
        public bool SectionContainsKey(string sectionName, string keyName)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return false;



            return section.KeyValuePairs.FindIndex(x => x.Key == keyName) != -1;
        }


        /// <summary>
        /// Gets all names of keys from INI section matching given regex pattern..
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="pattern">Regex pattern to match key names against.</param>
        /// <returns>Array of strings containing names of all keys matching with the specified pattern.</returns>
        /// 
        public string[] GetMatchingKeyNames(string sectionName, string pattern)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return null;

            List<string> keys = new List<string>();

            foreach (var kvp in section.KeyValuePairs)
            {
                if (Regex.IsMatch(kvp.Key, pattern))
                    keys.Add(kvp.Key);
            }

            return keys.ToArray();
        }

        /// <summary>
        /// Gets INI key name based on its position.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="positionIndex">Position of the INI key.</param>
        /// <returns>INI key name if key in that position is found, otherwise null.</returns>
        public string GetKeyByPosition(string sectionName, int positionIndex)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section != null && section.KeyValuePairs.Count > positionIndex && positionIndex >= 0)
            {
                return section.KeyValuePairs[positionIndex].Key;
            }

            return null;
        }

        /// <summary>
        /// Moves a key in INI file section matching specific name to a specific position in order in INI file.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="keyName">Name of the INI key.</param>
        /// <param name="positionIndex">The position to move the key into. Values higher than lower than the currently available amount of keys get constricted into that range.</param>
        /// <returns>True if position was changed, otherwise false.</returns>
        public bool SetKeyPosition(string sectionName, string keyName, int positionIndex)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section != null)
            {
                INIKeyValuePair kvp = section.KeyValuePairs.Find(x => x.Key == keyName);

                if (kvp == null)
                    return false;

                section.KeyValuePairs.Remove(kvp);
                section.KeyValuePairs.Insert(positionIndex, kvp);
                _altered = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sorts keys in the INI file section based on a list of regex patterns matching key names.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="sortPatterns">Regex patterns to match to key names used to determine sorting order.</param>
        public void SortSectionKeys(string sectionName, string[] sortPatterns)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section != null)
            {
                for (int i = 0; i < sortPatterns.Length; i++)
                {
                    foreach (INIKeyValuePair kvp in section.KeyValuePairs)
                    {
                        int index = Regex.IsMatch(kvp.Key, sortPatterns[i]) ? i : -1;

                        if (index != -1)
                        {
                            var pattern = sortPatterns[i];
                            kvp.SortIndex = index;
                        }
                    }
                }

                section.KeyValuePairs = section.KeyValuePairs.OrderBy(x => x.SortIndex).ToList();

                foreach (INIKeyValuePair kvp in section.KeyValuePairs)
                {
                    kvp.SortIndex = int.MaxValue;
                }

                _altered = true;
            }
        }

        /// <summary>
        /// Returns an array of string key-value pairs of key names and values from a INI file section.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <returns>Array of string key-value pairs of key names and values from a INI file section. Null if INI file has not been initialized or section was not found.</returns>
        public KeyValuePair<string, string>[] GetKeyValuePairs(string sectionName)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return null;

            KeyValuePair<string, string>[] kvps = new KeyValuePair<string, string>[section.KeyValuePairs.Count];
            int c = 0;

            foreach (INIKeyValuePair kvp in section.KeyValuePairs)
            {
                kvps[c++] = new KeyValuePair<string, string>(kvp.Key, kvp.Value);
            }

            return kvps;
        }

        /// <summary>
        /// Merges an another INI file into this one. Other INI file's values are picked over current INI file's.
        /// </summary>
        /// <param name="iniFileToMerge">INI file to merge into this one.</param>
        public void Merge(INIFile iniFileToMerge)
        {
            foreach (INISection newSection in iniFileToMerge.iniSections)
            {
                if (newSection == null)
                    continue;

                INISection section = iniSections.Find(i => i.Name == newSection.Name);

                if (section == null)
                {
                    iniSections.Add(newSection);
                    _altered = true;
                    continue;
                }

                foreach (INIKeyValuePair newKvp in newSection.KeyValuePairs)
                {
                    if (newKvp.Key == null)
                        continue;

                    INIKeyValuePair kvp = section.KeyValuePairs.Find(i => i.Key == newKvp.Key);

                    if (kvp != null)
                    {
                        kvp.Value = newKvp.Value;

                        List<INIComment> comments = newKvp.GetAllCommentsAtPosition(INICommentPosition.Before);
                        comments.AddRange(newKvp.GetAllCommentsAtPosition(INICommentPosition.Middle));
                        comments.AddRange(newKvp.GetAllCommentsAtPosition(INICommentPosition.After));

                        foreach (INIComment comment in comments)
                        {
                            if (!kvp.HasComment(comment))
                                kvp.AddComment(comment);
                        }

                        _altered = true;
                    }
                    else
                    {
                        section.KeyValuePairs.Add(newKvp);
                        _altered = true;
                    }
                }
            }
        }

        /// <summary>
        /// Removes INI file section matching a name.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <returns>True if section was removed, otherwise false.</returns>
        public bool RemoveSection(string sectionName)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return false;

            if (iniSections.Remove(section))
            {
                _altered = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Renames INI file section matching a name.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <returns>True if section was renamed, otherwise false.</returns>
        public bool RenameSection(string sectionName, string newSectionName)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null)
                return false;

            section.Name = newSectionName;
            _altered = true;
            return true;
        }

        /// <summary>
        /// Adds a new INI file section with a specific name, unless one already exists with same name.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <returns>True if a new section was added, otherwise false.</returns>
        public bool AddSection(string sectionName)
        {
            if (!SectionExists(sectionName))
            {
                INISection section = new INISection
                {
                    Name = sectionName,
                    KeyValuePairs = new List<INIKeyValuePair>()
                };

                iniSections.Add(section);
                _altered = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a string array containing names of all the sections in INI file.
        /// </summary>
        /// <returns>String array of sections in the INI file. Null if INI file has not been initialized.</returns>
        public string[] GetSections()
        {
            string[] sections = new string[iniSections.Count];
            int c = 0;

            foreach (INISection section in iniSections)
            {
                sections[c++] = section.Name;
            }

            return sections;
        }

        /// <summary>
        /// Moves INI file section matching specific name to first in order in INI file.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <returns>True if position was changed, otherwise false.</returns>
        public bool MoveSectionToFirst(string sectionName)
        {
            return SetSectionPosition(sectionName, 0);
        }

        /// <summary>
        /// Moves INI file section matching specific name to last in order in INI file.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <returns>True if position was changed, otherwise false.</returns>
        public bool MoveSectionToLast(string sectionName)
        {
            return SetSectionPosition(sectionName, iniSections.Count - 1);
        }

        /// <summary>
        /// Moves INI file section matching specific name to a specific position in order in INI file.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="positionIndex">The position to move the section into. Values higher than lower than the currently available amount of sections get constricted into that range.</param>
        /// <returns>True if position was changed, otherwise false.</returns>
        public bool SetSectionPosition(string sectionName, int positionIndex)
        {
            positionIndex = Math.Max(Math.Min(positionIndex, iniSections.Count - 1), 0);
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section != null)
            {
                iniSections.Remove(section);
                iniSections.Insert(positionIndex, section);
                _altered = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Replaces all values in INI file section with a collection of strings. Unique keys are generated in numerical order starting from 0.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="strings">A collection of strings to replace the section's values with.</param>
        /// <param name="createSection">If true, section will be created if it does not already exist.</param>
        public void ReplaceSectionWithStrings(string sectionName, IEnumerable<string> strings, bool createSection = true)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null && createSection)
                AddSection(sectionName);
            else if (section == null)
                return;

            section.KeyValuePairs.Clear();
            int c = 0;

            foreach (string line in strings)
            {
                section.KeyValuePairs.Add(new INIKeyValuePair(c++.ToString(), line));
            }

            _altered = true;
        }

        /// <summary>
        /// Replaces all keys & values in INI file section with a collection of key-value pairs.
        /// </summary>
        /// <param name="sectionName">Name of the INI file section.</param>
        /// <param name="kvps">A collection of key-value pairs to replace the section with.</param>
        /// <param name="createSection">If true, section will be created if it does not already exist.</param>
        public void ReplaceSectionWithKeyValuePairs(string sectionName, IEnumerable<KeyValuePair<string, string>> kvps, bool createSection = true)
        {
            INISection section = iniSections.Find(i => i.Name == sectionName);

            if (section == null && createSection)
                AddSection(sectionName);
            else if (section == null)
                return;

            section.KeyValuePairs.Clear();

            foreach (KeyValuePair<string, string> kvp in kvps)
            {
                section.KeyValuePairs.Add(new INIKeyValuePair(kvp.Key, kvp.Value));
            }

            _altered = true;
        }
    }

    /// <summary>
    /// INI section class.
    /// </summary>
    internal class INISection
    {
        /// <summary>
        /// INI section name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Number of empty lines below this section.
        /// </summary>
        public int EmptyLineCount = 0;

        /// <summary>
        /// Key-value pairs in this INI section.
        /// </summary>
        public List<INIKeyValuePair> KeyValuePairs = new List<INIKeyValuePair>();

        private List<INIComment> attachedComments = new List<INIComment>();

        /// <summary>
        /// Adds a comment to this section, created from text and position setting.
        /// </summary>
        /// <param name="commentText">Text of the comment.</param>
        /// <param name="position">Position relative to the section.</param>
        /// <param name="whiteSpaceCount">Number of whitespace characters between comment and rest of the line.</param>
        public void AddComment(string commentText, INICommentPosition position, int whiteSpaceCount)
        {
            attachedComments.Add(new INIComment(commentText, position, whiteSpaceCount));
        }

        /// <summary>
        /// Adds a comment to this section.
        /// </summary>
        /// <param name="comment">Comment to add.</param>
        public void AddComment(INIComment comment)
        {
            attachedComments.Add(comment);
        }

        /// <summary>
        /// Checks whether or not this section already contains a matching comment.
        /// </summary>
        /// <returns>True if section contains a matching comment, otherwise false.</returns>
        public bool HasComment(INIComment comment)
        {
            return attachedComments.Find(x => x.CommentText == comment.CommentText && x.Position == comment.Position) != null;
        }

        /// <summary>
        /// Returns a list of all comments that belong to this section at specific INI comment position.
        /// </summary>
        /// <param name="position">The INI comment position.</param>
        /// <returns>List of all commits belonging to this section in the specified INI comment position.</returns>
        public List<INIComment> GetAllCommentsAtPosition(INICommentPosition position)
        {
            return attachedComments.FindAll(x => x.Position == position);
        }
    }

    /// <summary>
    /// INI key-value pair class.
    /// </summary>
    internal class INIKeyValuePair
    {
        /// <summary>
        /// INI key.
        /// </summary>
        public string Key = null;

        /// <summary>
        /// INI value.
        /// </summary>
        public string Value = null;

        /// <summary>
        /// Sorting index for key-value pair.
        /// </summary>
        public int SortIndex = int.MaxValue;

        /// <summary>
        /// Number of empty lines below this key-value pair.
        /// </summary>
        public int EmptyLineCount = 0;

        private List<INIComment> attachedComments = new List<INIComment>();

        /// <summary>
        /// Creates new INI key-value pair.
        /// </summary>
        /// <param name="key">Name of the key.</param>
        /// <param name="value">Value of the key.</param>
        public INIKeyValuePair(string key, string value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Adds a comment to this key-value pair, created from text and position setting.
        /// </summary>
        /// <param name="commentText">Text of the comment.</param>
        /// <param name="position">Position relative to the key-value pair.</param>
        /// <param name="whiteSpaceCount">Number of whitespace characters between comment and rest of the line.</param>
        public void AddComment(string commentText, INICommentPosition position, int whiteSpaceCount)
        {
            attachedComments.Add(new INIComment(commentText, position, whiteSpaceCount));
        }

        /// <summary>
        /// Adds a comment to this key-value pair.
        /// </summary>
        /// <param name="comment">Comment to add.</param>
        public void AddComment(INIComment comment)
        {
            attachedComments.Add(comment);
        }

        /// <summary>
        /// Checks whether or not this key-value pair already contains a matching comment.
        /// </summary>
        /// <returns>True if key-value pair contains a matching comment, otherwise false.</returns>
        public bool HasComment(INIComment comment)
        {
            return attachedComments.Find(x => x.CommentText == comment.CommentText && x.Position == comment.Position) != null;
        }

        /// <summary>
        /// Returns a list of all comments that belong to this key-value pair at specific INI comment position.
        /// </summary>
        /// <param name="position">The INI comment position.</param>
        /// <returns>List of all commits belonging to this key-value pair in the specified INI comment position.</returns>
        public List<INIComment> GetAllCommentsAtPosition(INICommentPosition position)
        {
            return attachedComments.FindAll(x => x.Position == position);
        }
    }

    /// <summary>
    /// INI comment class.
    /// </summary>
    internal class INIComment
    {
        /// <summary>
        /// Comment text.
        /// </summary>
        public string CommentText { get; set; }

        /// <summary>
        /// Comment position.
        /// </summary>
        public INICommentPosition Position { get; set; }

        /// <summary>
        /// Number of whitespace characters between comment and rest of the line.
        /// </summary>
        public int WhitespaceCount { get; set; } = 0;

        /// <summary>
        /// Creates a new INI comment.
        /// </summary>
        /// <param name="commentText">Text of the comment.</param>
        /// <param name="position">Position relative to the section or a line it is attached to.</param>
        /// <param name="whiteSpaceCount">Number of whitespace characters between comment and rest of the line.</param>
        public INIComment(string commentText, INICommentPosition position, int whiteSpaceCount)
        {
            CommentText = commentText;
            Position = position;
            WhitespaceCount = whiteSpaceCount;
        }

        /// <summary>
        /// Get comment text to save to INI file.
        /// </summary>
        /// <returns></returns>
        public string GetINIText()
        {
            return ";".PadLeft(WhitespaceCount + 1, ' ') + CommentText;
        }
    }

    /// <summary>
    /// INI comment position.
    /// </summary>
    internal enum INICommentPosition { Before, Middle, After };

}