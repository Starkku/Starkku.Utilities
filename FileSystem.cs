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

using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Starkku.Utilities
{
    /// <summary>
    /// A class containing various filesystem-related utility methods.
    /// </summary>
    public static class FileSystem
    {
        /// <summary>
        /// Get filenames from a specific directory matching certain file extensions.
        /// </summary>
        /// <param name="directoryPath">Path of directory to search in.</param>
        /// <param name="fileExtensions">File extensions to check for when searching for files.</param>
        /// <param name="recursiveSearch">If true, recursively searchs through all subdirectories.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetFilesMatchingExtensions(string directoryPath, IEnumerable<string> fileExtensions, bool recursiveSearch = false)
        {
            SearchOption searchoption = SearchOption.AllDirectories;

            if (!recursiveSearch)
                searchoption = SearchOption.TopDirectoryOnly;

            if (string.IsNullOrEmpty(directoryPath))
                return Enumerable.Empty<string>();

            return Directory.GetFiles(directoryPath, "*.*", searchoption)
                .Where(file => fileExtensions
                .Contains(Path.GetExtension(file)));
        }
    }
}
