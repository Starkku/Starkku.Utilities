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

namespace Starkku.Utilities.DataStructures
{
    /// <summary>
    /// Generic HSL color struct.
    /// </summary>
    public struct HSLColor
    {
        /// <summary>
        /// Hue component of the HSL color.
        /// </summary>
        public double Hue { get; set; }

        /// <summary>
        /// Saturation component of the HSL color.
        /// </summary>
        public double Saturation { get; set; }

        /// <summary>
        /// Light component of the HSL color.
        /// </summary>
        public double Light { get; set; }
    }

    /// <summary>
    /// Generic 8-bit RGB(A) palette color class.
    /// </summary>
    public class PaletteColor
    {
        /// <summary>
        /// Red component of the RGB(A) palette color.
        /// </summary>
        public byte Red { get; set; }

        /// <summary>
        /// Green component of the RGB(A) palette color.
        /// </summary>
        public byte Green { get; set; }

        /// <summary>
        /// Blue component of the RGB(A) palette color.
        /// </summary>
        public byte Blue { get; set; }

        /// <summary>
        /// Alpha component of the RGBA palette color.
        /// </summary>
        public byte Alpha { get; set; } = 255;

        /// <summary>
        /// Create new palette color with default values.
        /// </summary>
        public PaletteColor()
        {
        }

        /// <summary>
        /// Create new palette color with specified values.
        /// </summary>
        /// <param name="red">Red component in RGB(A) color.</param>
        /// <param name="green">Green component in RGB(A) color.</param>
        /// <param name="blue">Blue component in RGB(A) color.</param>
        /// <param name="alpha">Optional alpha component in RGBA color.</param>
        public PaletteColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        /// <summary>
        /// Get a HSL color space equivalent of this RGB(A) palette color.
        /// </summary>
        /// <returns>HSL color matching this RGB(A) palette color.</returns>
        public HSLColor GetHSLColor()
        {
            HSLColor hsl = new HSLColor();

            double red = Red / 255.0;
            double green = Green / 255.0;
            double blue = Blue / 255.0;

            double min = Math.Min(red, Math.Min(green, blue));
            double max = Math.Max(red, Math.Max(green, blue));

            hsl.Light = (max + min) / 2.0;

            if (max != min)
            {
                double difference = max - min;
                hsl.Saturation = hsl.Light > 0.5 ? difference / (2.0 - max - min) : difference / (max + min);

                if (max == red)
                    hsl.Hue = (green - blue) / difference + (green < blue ? 6 : 0);
                else if (max == green)
                    hsl.Hue = (blue - red) / difference + 2;
                else if (max == blue)
                    hsl.Hue = (red - green) / difference + 4;

                hsl.Hue /= 6;
            }

            return hsl;
        }
    }

    /// <summary>
    /// RGB(A) palette color red component comparer class.
    /// </summary>
    public class PaletteColorRedComparer : IComparer<PaletteColor>
    {
        public int Compare(PaletteColor x, PaletteColor y)
        {

            if (x.Red < y.Red)
                return -1;
            else if (x.Red == y.Red)
                return 0;
            else
                return 1;
        }
    }

    /// <summary>
    /// RGB(A) palette color green component comparer class.
    /// </summary>
    public class PaletteColorGreenComparer : IComparer<PaletteColor>
    {
        public int Compare(PaletteColor x, PaletteColor y)
        {

            if (x.Green < y.Green)
                return -1;
            else if (x.Green == y.Green)
                return 0;
            else
                return 1;
        }
    }

    /// <summary>
    /// RGB(A) palette color blue component comparer class.
    /// </summary>
    public class PaletteColorBlueComparer : IComparer<PaletteColor>
    {
        public int Compare(PaletteColor x, PaletteColor y)
        {

            if (x.Blue < y.Blue)
                return -1;
            else if (x.Blue == y.Blue)
                return 0;
            else
                return 1;
        }
    }

    /// <summary>
    /// RGBA palette color alpha component comparer class.
    /// </summary>
    public class PaletteColorAlphaComparer : IComparer<PaletteColor>
    {
        public int Compare(PaletteColor x, PaletteColor y)
        {
            int rgb = x.Red + x.Green + x.Blue;
            int rgbOther = y.Red + y.Green + y.Blue;

            if (x.Alpha < y.Alpha)
                return -1;
            else if (x.Alpha == y.Alpha)
            {
                if (rgb < rgbOther)
                    return -1;
                else if (rgb > rgbOther)
                    return 1;
                else
                    return 0;
            }
            else
                return 1;
        }
    }

    /// <summary>
    /// RGB(A) palette color RGB component comparer class.
    /// </summary>
    public class PaletteColorRGBComparer : IComparer<PaletteColor>
    {
        public int Compare(PaletteColor x, PaletteColor y)
        {
            int rgb = x.Red + x.Green + x.Blue;
            int rgbOther = y.Red + y.Green + y.Blue;

            if (rgb < rgbOther)
                return -1;
            else if (rgb == rgbOther)
            {
                if (x.Alpha < y.Alpha)
                    return -1;
                else if (x.Alpha > y.Alpha)
                    return 1;
                else
                    return 0;
            }
            else
                return 1;
        }
    }

    /// <summary>
    /// RGB(A) palette color hue comparer class.
    /// </summary>
    public class PaletteColorHueComparer : IComparer<PaletteColor>
    {
        public int Compare(PaletteColor x, PaletteColor y)
        {
            HSLColor x_hsl = x.GetHSLColor();
            HSLColor y_hsl = y.GetHSLColor();

            if (x_hsl.Hue < y_hsl.Hue)
                return -1;
            else if (x_hsl.Hue > y_hsl.Hue)
                return 1;
            else
            {
                int rgb = x.Red + x.Green + x.Blue;
                int rgbOther = y.Red + y.Green + y.Blue;

                if (rgb < rgbOther)
                    return -1;
                else if (rgb == rgbOther)
                    return 0;
                else
                    return 1;
            }
        }
    }

    /// <summary>
    /// RGB(A) palette color saturation comparer class.
    /// </summary>
    public class PaletteColorSaturationComparer : IComparer<PaletteColor>
    {
        public int Compare(PaletteColor x, PaletteColor y)
        {
            HSLColor x_hsl = x.GetHSLColor();
            HSLColor y_hsl = y.GetHSLColor();

            if (x_hsl.Saturation < y_hsl.Saturation)
                return -1;
            else if (x_hsl.Saturation > y_hsl.Saturation)
                return 1;
            else
            {
                int rgb = x.Red + x.Green + x.Blue;
                int rgbOther = y.Red + y.Green + y.Blue;

                if (rgb < rgbOther)
                    return -1;
                else if (rgb == rgbOther)
                    return 0;
                else
                    return 1;
            }
        }
    }

    /// <summary>
    /// RGB(A) palette color light comparer class.
    /// </summary>
    public class PaletteColorLightComparer : IComparer<PaletteColor>
    {
        public int Compare(PaletteColor x, PaletteColor y)
        {
            HSLColor x_hsl = x.GetHSLColor();
            HSLColor y_hsl = y.GetHSLColor();

            if (x_hsl.Light < y_hsl.Light)
                return -1;
            else if (x_hsl.Light > y_hsl.Light)
                return 1;
            else
            {
                int rgb = x.Red + x.Green + x.Blue;
                int rgbOther = y.Red + y.Green + y.Blue;

                if (rgb < rgbOther)
                    return -1;
                else if (rgb == rgbOther)
                    return 0;
                else
                    return 1;
            }
        }
    }

    /// <summary>
    /// Palette color equality comparer.
    /// </summary>
    public class PaletteColorEqualityComparer : IEqualityComparer<PaletteColor>
    {
        public bool Equals(PaletteColor x, PaletteColor y)
        {
            return x.Red == y.Red && x.Green == y.Green && x.Blue == y.Blue && x.Alpha == y.Alpha;
        }

        public int GetHashCode(PaletteColor obj)
        {
            return obj.Red + obj.Green + obj.Blue + obj.Alpha;
        }
    }
}