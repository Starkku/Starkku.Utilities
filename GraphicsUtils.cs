/*
 * Copyright © 2020 Starkku
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Starkku.Utilities
{
    /// <summary>
    /// A class containing graphics-related utility methods.
    /// </summary>
    public static class GraphicsUtils
    {
        /// <summary>
        /// Creates a bitmap based on provided dimensions and raw image data.
        /// <paramref name="pixelFormat"/> can be defined to explicitly set pixel format used for the created bitmap.
        /// If not set, it will attempt to guess it based on data length & provided dimensions which can be inaccurate in some cases.
        /// </summary>
        /// <param name="width">Width of the bitmap.</param>
        /// <param name="height">Height of the bitmap.</param>
        /// <param name="imageData">Raw image data.</param>
        /// <param name="pixelFormat">Pixel format to use for created bitmap.</param>
        /// <returns>Bitmap based on the provided dimensions and raw image data, or null if length of image data does not match the provided dimensions or pixel format.</returns>
        public static Bitmap CreateBitmapFromImageData(int width, int height, byte[] imageData, PixelFormat pixelFormat = PixelFormat.Undefined)
        {
            PixelFormat pxFormat = pixelFormat;

            if (pxFormat == PixelFormat.Undefined)
            {
                pxFormat = GetPixelFormat(imageData.Length / (width * height) * 8);

                if (pxFormat == PixelFormat.Undefined)
                    return null;
            }
            else
            {
                if (imageData.Length != width * height * (GetBitsPerPixel(pxFormat) / 8))
                    return null;
            }

            Bitmap bitmap = new Bitmap(width, height, pxFormat);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, pxFormat);
            IntPtr scan0 = bitmapData.Scan0;
            byte[] data = GetAdjustedImageData(imageData, pxFormat);
            Marshal.Copy(data, 0, scan0, data.Length);
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }

        /// <summary>
        /// Gets raw image data for given bitmap.
        /// </summary>
        /// <param name="bitmap">Bitmap.</param>
        /// <returns>Raw image data. If bitmap is invalid, null is returned instead.</returns>
        public static byte[] GetRawImageDataFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                return null;

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            IntPtr scan0 = bitmapData.Scan0;
            byte[] data = new byte[Math.Abs(bitmapData.Stride) * bitmapData.Height];
            Marshal.Copy(scan0, data, 0, data.Length);
            bitmap.UnlockBits(bitmapData);

            return GetAdjustedImageData(data, bitmap.PixelFormat);
        }

        private static byte[] GetAdjustedImageData(byte[] imageData, PixelFormat pixelFormat)
        {
            int bitsPerPixel = GetBitsPerPixel(pixelFormat);

            if (bitsPerPixel == 24 || bitsPerPixel == 32)
            {
                byte[] data = new byte[imageData.Length];
                int bytesPerPixel = bitsPerPixel / 8;
                int value1Offset = 2;
                int value2Offset = 1;
                int value3Offset = 0;

                for (int i = 0; i < data.Length; i += bytesPerPixel)
                {
                    data[i] = imageData[i + value1Offset];
                    data[i + 1] = imageData[i + value2Offset];
                    data[i + 2] = imageData[i + value3Offset];

                    if (bitsPerPixel == 32)
                        data[i + 3] = imageData[i + 3];
                }

                return data;
            }
            else
                return imageData;
        }

        /// <summary>
        /// Attempts to convert bitmap to another pixel format.
        /// </summary>
        /// <param name="bitmap">Bitmap to convert.</param>
        /// <param name="convertedBitmap">If conversion was successful, will be set to the converted bitmap, otherwise null.</param>
        /// <param name="pixelFormat">Pixel format to convert to.</param>
        /// <returns>True if conversion was successful, otherwise false.</returns>
        public static bool TryConvertBitmap(Bitmap bitmap, out Bitmap convertedBitmap, PixelFormat pixelFormat)
        {
            convertedBitmap = null;

            if (bitmap.PixelFormat == pixelFormat)
            {
                convertedBitmap = bitmap;
                return true;
            }

            try
            {
                convertedBitmap = new Bitmap(bitmap.Width, bitmap.Height, pixelFormat);

                using (Graphics gr = Graphics.FromImage(convertedBitmap))
                {
                    gr.DrawImage(bitmap, new Rectangle(0, 0, convertedBitmap.Width, convertedBitmap.Height));
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Replaces bitmaps palette with given list of colors.
        /// Wíll fail if provided list of colors contains more or less colors than the existing bitmap palette.
        /// </summary>
        /// <param name="bitmap">Bitmap.</param>
        /// <param name="colors">List of colors.</param>
        /// <returns>True if successfully replaced the palette, otherwise false.</returns>
        public static bool SetBitmapPaletteColors(Bitmap bitmap, IList<Color> colors)
        {
            ColorPalette palette = bitmap.Palette;
            Color[] entries = palette.Entries;

            if (colors.Count != entries.Length)
                return false;

            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = colors[i];
            }

            bitmap.Palette = palette;
            return true;
        }

        /// <summary>
        /// Gets bitmap's palette colors as an array.
        /// </summary>
        /// <param name="bitmap">Bitmap.</param>
        /// <returns>Array of colors from bitmap's palette.</returns>
        public static Color[] GetBitmapPaletteColors(Bitmap bitmap) => bitmap?.Palette?.Entries;

        /// <summary>
        /// Gets number of bits per pixel for given pixel format.
        /// </summary>
        /// <param name="pixelFormat">Pixel format.</param>
        /// <returns>Number of bits per pixel for a given pixel format. Returns 0 if given pixel format does not contain flags to determine number of bits per pixel.</returns>
        public static int GetBitsPerPixel(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed:
                    return 1;
                case PixelFormat.Format4bppIndexed:
                    return 4;
                case PixelFormat.Format8bppIndexed:
                    return 8;
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                case PixelFormat.Format16bppArgb1555:
                    return 16;
                case PixelFormat.Format24bppRgb:
                    return 24;
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    return 32;
                case PixelFormat.Format48bppRgb:
                    return 48;
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    return 64;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets pixel format for given number of bits per pixel.
        /// Makes an assumption that 16 bits per pixel means 565 RGB format and that 32 / 64 bits per pixel means alpha and non-premultiplied colors.
        /// </summary>
        /// <param name="bitsPerPixel">Number of bits per pixel.</param>
        /// <returns></returns>
        public static PixelFormat GetPixelFormat(int bitsPerPixel)
        {
            switch (bitsPerPixel)
            {
                case 1:
                    return PixelFormat.Format1bppIndexed;
                case 4:
                    return PixelFormat.Format4bppIndexed;
                case 8:
                    return PixelFormat.Format8bppIndexed;
                case 16:
                    return PixelFormat.Format16bppRgb565;
                case 24:
                    return PixelFormat.Format24bppRgb;
                case 32:
                    return PixelFormat.Format32bppArgb;
                case 48:
                    return PixelFormat.Format48bppRgb;
                case 64:
                    return PixelFormat.Format64bppArgb;
                default:
                    return PixelFormat.Undefined;
            }
        }
    }
}
