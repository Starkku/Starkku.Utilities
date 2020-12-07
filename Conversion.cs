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
using System.Globalization;

namespace Starkku.Utilities
{
    /// <summary>
    /// A class containing type conversion utility methods.
    /// </summary>
    public static class Conversion
    {
        /// <summary>
        /// Attempt to parse a byte from a string.
        /// </summary>
        /// <param name="str">String to attempt parsing from.</param>
        /// <param name="default_value">Default value returned if unsuccessful.</param>
        /// <returns>Value parsed from string if successful, otherwise default value is returned.</returns>
        public static byte GetByteFromString(string str, byte default_value)
        {
            if (string.IsNullOrEmpty(str))
                return default_value;

            if (byte.TryParse(str, out byte ret))
                return ret;
            else
                return default_value;
        }

        /// <summary>
        /// Attempt to parse a short integer from a string.
        /// </summary>
        /// <param name="str">String to attempt parsing from.</param>
        /// <param name="default_value">Default value returned if unsuccessful.</param>
        /// <returns>Value parsed from string if successful, otherwise default value is returned.</returns>
        public static short GetShortFromString(string str, short default_value)
        {
            if (string.IsNullOrEmpty(str))
                return default_value;

            if (short.TryParse(str, out short ret))
                return ret;
            else
                return default_value;
        }

        /// <summary>
        /// Attempt to parse an integer from a string.
        /// </summary>
        /// <param name="str">String to attempt parsing from.</param>
        /// <param name="default_value">Default value returned if unsuccessful.</param>
        /// <returns>Value parsed from string if successful, otherwise default value is returned.</returns>
        public static int GetIntFromString(string str, int default_value)
        {
            if (string.IsNullOrEmpty(str))
                return default_value;

            if (int.TryParse(str, out int ret))
                return ret;
            else
                return default_value;
        }

        /// <summary>
        /// Attempt to parse a long integer from a string.
        /// </summary>
        /// <param name="str">String to attempt parsing from.</param>
        /// <param name="default_value">Default value returned if unsuccessful.</param>
        /// <returns>Value parsed from string if successful, otherwise default value is returned.</returns>
        public static long GetLongFromString(string str, long default_value)
        {
            if (string.IsNullOrEmpty(str))
                return default_value;

            if (long.TryParse(str, out long ret))
                return ret;
            else
                return default_value;
        }

        /// <summary>
        /// Attempt to parse a single-precision floating point number from a string.
        /// </summary>
        /// <param name="str">String to attempt parsing from.</param>
        /// <param name="default_value">Default value returned if unsuccessful.</param>
        /// <returns>Value parsed from string if successful, otherwise default value is returned.</returns>
        public static float GetFloatFromString(string str, float default_value)
        {
            if (string.IsNullOrEmpty(str))
                return default_value;

            if (float.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out float ret))
                return ret;
            else
                return default_value;
        }

        /// <summary>
        /// Attempt to parse a double-precision floating point number from a string.
        /// </summary>
        /// <param name="str">String to attempt parsing from.</param>
        /// <param name="default_value">Default value returned if unsuccessful.</param>
        /// <returns>Value parsed from string if successful, otherwise default value is returned.</returns>
        public static double GetDoubleFromString(string str, double default_value)
        {
            if (string.IsNullOrEmpty(str))
                return default_value;

            if (double.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out double ret))
                return ret;
            else
                return default_value;
        }

        /// <summary>
        /// Attempt to parse a boolean value from a string.
        /// </summary>
        /// <param name="str">String to attempt parsing from.</param>
        /// <param name="default_value">Default value returned if unsuccessful.</param>
        /// <returns>Value parsed from string if successful, otherwise default value is returned.</returns>
        public static bool GetBoolFromString(string str, bool default_value)
        {
            if (string.IsNullOrEmpty(str))
                return default_value;

            if (str.Trim().Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
                str.Trim().Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
                str.Trim().Equals("1", StringComparison.InvariantCultureIgnoreCase))
                return true;
            else if (str.Trim().Equals("no", StringComparison.InvariantCultureIgnoreCase) ||
                str.Trim().Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
                str.Trim().Equals("0", StringComparison.InvariantCultureIgnoreCase))
                return false;
            else
                return default_value;
        }
    }
}
