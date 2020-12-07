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
using System.IO;
using System.Linq;
using Starkku.Utilities.DataStructures;

namespace Starkku.Utilities.FileTypes
{
    /// <summary>
    /// Palette file class for a generic 256-color palette / JASC palette.
    /// </summary>
    public class PaletteFile
    {
        private const string JASCPALID = "JASC-PAL";
        private const string JASCPALEXTRA = "0100";

        /// <summary>
        /// Filename of the palette file.
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// True if palette file has been properly initialized, otherwise false.
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// Gets or sets whether or not this palette uses 6 bits per color channel instead of the usual 8 bits.
        /// </summary>
        public bool Is6BitRGBPalette { get; private set; }

        /// <summary>
        /// Amount of palette colors this palette contains.
        /// </summary>
        public int ColorCount => colors.Length;

        private PaletteColor[] colors = new PaletteColor[0];

        /// <summary>
        /// Create a new palette from a file.
        /// </summary>
        /// <param name="filename">Filename to load the palette data from.</param>
        public PaletteFile(string filename) : this(filename, false)
        {
        }

        /// <summary>
        /// Create a new palette from a file.
        /// </summary>
        /// <param name="filename">Filename to load the palette data from.</param>
        /// <param name="use6BitRGBColor">If set to true and palette is a generic 256-color palette, use 6 bits per color channel instead of the usual 8 bits.</param>
        public PaletteFile(string filename, bool use6BitRGBColor)
        {
            Filename = filename;
            Is6BitRGBPalette = use6BitRGBColor;

            if (string.IsNullOrEmpty(Filename) || !File.Exists(Filename))
                return;

            string[] lines = File.ReadAllLines(Filename);

            if (lines != null && lines.Length > 0 && lines[0].Equals(JASCPALID))
            {
                ParseJASCPalette(lines);
            }
            else
            {
                LoadGenericPalette();
            }
        }

        /// <summary>
        /// Create a new palette with default color data.
        /// </summary>
        public PaletteFile() : this(false)
        {
        }

        /// <summary>
        /// Create a new palette with default color data.
        /// </summary>
        /// <param name="use6BitRGBColor">If set to true, use 6 bits per color channel instead of the usual 8 bits.</param>
        public PaletteFile(bool use6BitRGBColor)
        {
            Is6BitRGBPalette = use6BitRGBColor;
            PaletteColor defaultcolor = new PaletteColor();
            colors = Enumerable.Repeat(defaultcolor, 256).ToArray();
            Initialized = true;
        }

        /// <summary>
        /// Load a generic 256-color palette from file.
        /// </summary>
        private void LoadGenericPalette()
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(Filename, FileMode.Open);

                if (fs.Length != 768)
                    return;

                byte[] values = new byte[768];
                fs.Read(values, 0, values.Length);
                int j = 0;
                colors = new PaletteColor[256];
                for (int i = 0; i < ColorCount; i++)
                {
                    if (Is6BitRGBPalette)
                        colors[i] = new PaletteColor((byte)(values[j++] * 4), (byte)(values[j++] * 4), (byte)(values[j++] * 4));
                    else
                        colors[i] = new PaletteColor(values[j++], values[j++], values[j++]);
                }
            }
            catch (Exception)
            {
                return;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            Initialized = true;
        }

        /// <summary>
        /// Parse JASC palette file from text file lines.
        /// </summary>
        private void ParseJASCPalette(string[] lines)
        {
            int c = 2;
            int colorCount = Conversion.GetIntFromString(lines[c++], 0);

            if (colorCount < 1)
                return;

            colors = new PaletteColor[colorCount];

            for (int i = c; i < colorCount + c; i++)
            {
                colors[i - c] = ParsePaletteColorFromString(lines[i]);
            }

            Initialized = true;
        }

        /// <summary>
        /// Saves the palette as a generic 256-color palette to a file.
        /// </summary>
        /// <param name="filename">Filename to save to. If not set, current filename is used.</param>
        /// <returns>True if saved successfully, false otherwise.</returns>
        public bool SaveGenericPalette(string filename = null)
        {
            if (string.IsNullOrEmpty(filename) || !Initialized)
                return false;

            FileStream fs = null;
            try
            {
                fs = new FileStream(filename, FileMode.Create);
                for (int i = 0; i < ColorCount; i++)
                {
                    PaletteColor c = colors[i];
                    if (c == null)
                        return false;
                    if (Is6BitRGBPalette)
                    {
                        fs.WriteByte((byte)(c.Red / 4));
                        fs.WriteByte((byte)(c.Green / 4));
                        fs.WriteByte((byte)(c.Blue / 4));
                    }
                    else
                    {
                        fs.WriteByte(c.Red);
                        fs.WriteByte(c.Green);
                        fs.WriteByte(c.Blue);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            return true;
        }

        /// <summary>
        /// Saves the palette as JASC palette to a file.
        /// </summary>
        /// <param name="filename">Filename to save to. If not set, current filename is used.</param>
        /// <param name="saveAlpha">If set to true, alpha values are saved in the output file.</param> 
        /// <returns>True if saved successfully, false otherwise.</returns>
        public bool SaveJASCPalette(string filename = null, bool saveAlpha = true)
        {
            if (!Initialized)
                return false;

            string fileout = Filename;

            if (filename != null)
                fileout = filename;

            if (string.IsNullOrEmpty(fileout) || !Initialized)
                return false;

            string[] lines = new string[ColorCount + 3];
            lines[0] = JASCPALID;
            lines[1] = JASCPALEXTRA;
            lines[2] = ColorCount.ToString();

            for (int i = 3; i < lines.Length; i++)
            {
                lines[i] = GetColorString(colors[i - 3], saveAlpha);
            }

            try
            {
                File.WriteAllLines(fileout, lines);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates alpha value for each palette color based on average of RGB values and a separately defined divisor.
        /// </summary>
        /// <param name="divisor">Divisor used in calculating the average. Defaults to 3.0 as per the 3 color channels.</param>
        public void CalculateAverageAlpha(double divisor = 3.0)
        {
            if (!Initialized)
                return;

            for (int i = 0; i < ColorCount; i++)
            {
                colors[i].Alpha = (byte)Math.Max(Math.Min(Convert.ToInt32((colors[i].Red + colors[i].Green + colors[i].Blue) / divisor), 255), 0);
            }
        }

        /// <summary>
        /// Sets alpha value for each palette color based on a comparison threshold value. 
        /// Type of value compared to the threshold can be set using <paramref name="thresholdType"/>.
        /// </summary>
        /// <param name="thresholdType">Type of value the threshold is compared to.</param>
        /// <param name="thresholdValue">Threshold value.</param>
        /// <param name="alphaValue">Alpha value to set.</param>
        public void AlphaByThreshold(PaletteColorSortMode thresholdType, int thresholdValue, byte alphaValue)
        {
            if (!Initialized)
                return;

            for (int i = 0; i < ColorCount; i++)
            {
                HSLColor hsl = colors[i].GetHSLColor();

                int compare;
                switch (thresholdType)
                {
                    case PaletteColorSortMode.Hue:
                        compare = (int)Math.Round(hsl.Hue * 255);
                        break;
                    case PaletteColorSortMode.Light:
                        compare = (int)Math.Round(hsl.Light * 255);
                        break;
                    case PaletteColorSortMode.Red:
                        compare = colors[i].Red;
                        break;
                    case PaletteColorSortMode.Green:
                        compare = colors[i].Green;
                        break;
                    case PaletteColorSortMode.Blue:
                        compare = colors[i].Blue;
                        break;
                    case PaletteColorSortMode.Alpha:
                        compare = colors[i].Alpha;
                        break;
                    case PaletteColorSortMode.RGB:
                        compare = (colors[i].Red + colors[i].Green + colors[i].Blue) / 3;
                        break;
                    default:
                        compare = (int)Math.Round(hsl.Saturation * 255);
                        break;
                }

                if (compare >= thresholdValue)
                {
                    colors[i].Alpha = alphaValue;
                }
            }

        }

        /// <summary>
        /// Gets a concatenated string of the individual color component values for a palette color.
        /// </summary>
        /// <param name="color">Palette color.</param>
        /// <param name="useAlpha">If true, alpha value is included in the concatenated string.</param>
        /// <returns>Concatenated string of the individual color component values of given palette color.</returns>
        private string GetColorString(PaletteColor color, bool useAlpha = true)
        {
            string alpha = "";

            if (useAlpha && color.Alpha >= 0)
                alpha = " " + color.Alpha;

            return color.Red + " " + color.Green + " " + color.Blue + alpha;
        }

        /// <summary>
        /// Attempts to parse a palette color from a string.
        /// </summary>
        /// <param name="colorString">A string of color values, separated by the provided separator character.</param>
        /// <param name="separator">Separator character to look for in the string when parsing color values. Defaults to whitespace.</param>
        /// <returns>Palette color if parsing was successful, otherwise null.</returns>
        private PaletteColor ParsePaletteColorFromString(string colorString, char separator = ' ')
        {
            if (string.IsNullOrEmpty(colorString))
                return default;

            string[] sp = colorString.Split(separator);

            if (sp.Length < 3)
                return null;

            PaletteColor color = new PaletteColor
            {
                Red = Conversion.GetByteFromString(sp[0], 0),
                Green = Conversion.GetByteFromString(sp[1], 0),
                Blue = Conversion.GetByteFromString(sp[2], 0)
            };

            if (sp.Length >= 4)
                color.Alpha = Conversion.GetByteFromString(sp[3], 0);
            else
                color.Alpha = 255;

            return color;
        }

        /// <summary>
        /// Sorts the palette colors based on given sorting mode.
        /// </summary>
        /// <param name="sortMode">Sorting mode used when sorting colors.</param>
        public void SortColors(PaletteColorSortMode sortMode)
        {
            if (colors == null || ColorCount < 1 || !Initialized)
                return;

            PaletteColor[] colors_sorted = new PaletteColor[255];
            Array.Copy(colors, 1, colors_sorted, 0, 255);

            switch (sortMode)
            {
                case PaletteColorSortMode.Hue:
                    Array.Sort(colors_sorted, new PaletteColorHueComparer());
                    break;
                case PaletteColorSortMode.Saturation:
                    Array.Sort(colors_sorted, new PaletteColorSaturationComparer());
                    break;
                case PaletteColorSortMode.Light:
                    Array.Sort(colors_sorted, new PaletteColorLightComparer());
                    break;
                case PaletteColorSortMode.Red:
                    Array.Sort(colors_sorted, new PaletteColorRedComparer());
                    break;
                case PaletteColorSortMode.Green:
                    Array.Sort(colors_sorted, new PaletteColorGreenComparer());
                    break;
                case PaletteColorSortMode.Blue:
                    Array.Sort(colors_sorted, new PaletteColorBlueComparer());
                    break;
                case PaletteColorSortMode.Alpha:
                    Array.Sort(colors_sorted, new PaletteColorAlphaComparer());
                    break;
                case PaletteColorSortMode.RGB:
                    Array.Sort(colors_sorted, new PaletteColorRGBComparer());
                    break;
                default:
                    break;
            }

            for (int i = 1; i < ColorCount; i++)
            {
                colors[i] = colors_sorted[i - 1];
            }
        }

        /// <summary>
        /// Multiplies palette color component values by the given multiplier.
        /// </summary>
        public void MultiplyColors(double multiplier)
        {
            if (!Initialized)
                return;

            for (int i = 0; i < ColorCount; i++)
            {
                if (colors[i] == null)
                    continue;

                colors[i].Red = MultiplyColor(colors[i].Red, multiplier);
                colors[i].Green = MultiplyColor(colors[i].Green, multiplier);
                colors[i].Blue = MultiplyColor(colors[i].Blue, multiplier);
            }
        }

        /// <summary>
        /// Multiplies palette color component values by the alpha component value.
        /// </summary>
        public void MultiplyColorsByAlpha()
        {
            if (!Initialized)
                return;

            for (int i = 0; i < ColorCount; i++)
            {
                if (colors[i].Alpha < 0 || colors[i] == null)
                    continue;

                colors[i].Red = MultiplyColor(colors[i].Red, colors[i].Alpha / 255.0);
                colors[i].Green = MultiplyColor(colors[i].Green, colors[i].Alpha / 255.0);
                colors[i].Blue = MultiplyColor(colors[i].Blue, colors[i].Alpha / 255.0);
            }
        }

        /// <summary>
        /// Multiplies an individual color component value by given multiplier.
        /// </summary>
        /// <param name="color">Color component value.</param>
        /// <param name="multiplier">Given multiplier value.</param>
        /// <returns>Multiplied color component value.</returns>
        private byte MultiplyColor(byte color, double multiplier)
        {
            int multiplied = (int)(color * multiplier);
            return Convert.ToByte(Math.Min(Math.Max(multiplied, 0), 255));
        }

        /// <summary>
        /// Gets a palette color based on index.
        /// </summary>
        /// <param name="index">Palette color index. Only values between 0 and palette color count minus one are valid.</param>
        /// <returns>Returns palette color if it was found, otherwise null.</returns>
        public PaletteColor GetColor(int index)
        {
            if (index < 0 || index > ColorCount - 1 || !Initialized)
                return null;

            return colors[index];
        }

        /// <summary>
        /// Sets a palette color based on index.
        /// </summary>
        /// <param name="index">Palette color index. Only values between 0 and palette color count minus one are valid</param>
        /// <param name="color">Palette color to set for the specified index.</param>
        public void SetColor(int index, PaletteColor color)
        {
            if (index < 0 || index > ColorCount - 1 || !Initialized)
                return;

            colors[index] = color;
        }

        /// <summary>
        /// Gets all unique colors in the palette.
        /// </summary>
        /// <returns>Array containing unique palette colors. If palette is not initialized, returns null.</returns>
        public PaletteColor[] GetDistinctColors()
        {
            if (!Initialized)
                return null;

            return colors.Distinct(new PaletteColorEqualityComparer()).ToArray();
        }
    }

    /// <summary>
    /// Palette color sort mode.
    /// </summary>
    public enum PaletteColorSortMode { Hue, Saturation, Light, Red, Green, Blue, Alpha, RGB };
}
