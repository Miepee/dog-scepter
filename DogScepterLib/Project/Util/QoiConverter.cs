﻿using System;
using System.IO;

namespace DogScepterLib.Project.Util;

// Responsible for QOI image format conversions to Bitmaps
// Note that the QOI format used in GameMaker is modified significantly:
//  - It's little-endian and has a different header, with missing/added fields.
//  - The DIFF encoding works differently.
// Derived from official QOI source at https://github.com/phoboslab/qoi and QoiSharp at https://github.com/NUlliiON/QoiSharp.
public static class QoiConverter
{
    private const byte QOI_INDEX = 0x00;
    private const byte QOI_RUN_8 = 0x40;
    private const byte QOI_RUN_16 = 0x60;
    private const byte QOI_DIFF_8 = 0x80;
    private const byte QOI_DIFF_16 = 0xc0;
    private const byte QOI_DIFF_24 = 0xe0;

    private const byte QOI_COLOR = 0xf0;
    private const byte QOI_MASK_2 = 0xc0;
    private const byte QOI_MASK_3 = 0xe0;
    private const byte QOI_MASK_4 = 0xf0;

    public unsafe static DSImage GetImageFromStream(Stream s)
    {
        byte[] header = new byte[12];
        s.Read(header, 0, 12);
        if (header[0] != (byte)'f' || header[1] != (byte)'i' || header[2] != (byte)'o' || header[3] != (byte)'q')
            throw new Exception("Invalid little-endian QOIF image magic");

        int width = header[4] + (header[5] << 8);
        int height = header[6] + (header[7] << 8);
        int length = header[8] + (header[9] << 8) + (header[10] << 16) + (header[11] << 24);

        byte[] pixelData = new byte[length];
        s.Read(pixelData, 0, length);

        DSImage img = new(width, height);
        int imgPos = 0;

        int pos = 0;
        int run = 0;
        byte r = 0, g = 0, b = 0, a = 255;
        byte[] index = new byte[64 * 4];
        while (imgPos < img.Data.Length)
        {
            if (run > 0)
            {
                run--;
            }
            else if (pos < pixelData.Length)
            {
                int b1 = pixelData[pos++];

                if ((b1 & QOI_MASK_2) == QOI_INDEX)
                {
                    int indexPos = (b1 ^ QOI_INDEX) << 2;
                    r = index[indexPos];
                    g = index[indexPos + 1];
                    b = index[indexPos + 2];
                    a = index[indexPos + 3];
                }
                else if ((b1 & QOI_MASK_3) == QOI_RUN_8)
                {
                    run = b1 & 0x1f;
                }
                else if ((b1 & QOI_MASK_3) == QOI_RUN_16)
                {
                    int b2 = pixelData[pos++];
                    run = (((b1 & 0x1f) << 8) | b2) + 32;
                }
                else if ((b1 & QOI_MASK_2) == QOI_DIFF_8)
                {
                    r += (byte)(((b1 & 48) << 26 >> 30) & 0xff);
                    g += (byte)(((b1 & 12) << 28 >> 22 >> 8) & 0xff);
                    b += (byte)(((b1 & 3) << 30 >> 14 >> 16) & 0xff);
                }
                else if ((b1 & QOI_MASK_3) == QOI_DIFF_16)
                {
                    int b2 = pixelData[pos++];
                    int merged = b1 << 8 | b2;
                    r += (byte)(((merged & 7936) << 19 >> 27) & 0xff);
                    g += (byte)(((merged & 240) << 24 >> 20 >> 8) & 0xff);
                    b += (byte)(((merged & 15) << 28 >> 12 >> 16) & 0xff);
                }
                else if ((b1 & QOI_MASK_4) == QOI_DIFF_24)
                {
                    int b2 = pixelData[pos++];
                    int b3 = pixelData[pos++];
                    int merged = b1 << 16 | b2 << 8 | b3;
                    r += (byte)(((merged & 1015808) << 12 >> 27) & 0xff);
                    g += (byte)(((merged & 31744) << 17 >> 19 >> 8) & 0xff);
                    b += (byte)(((merged & 992) << 22 >> 11 >> 16) & 0xff);
                    a += (byte)(((merged & 31) << 27 >> 3 >> 24) & 0xff);
                }
                else if ((b1 & QOI_MASK_4) == QOI_COLOR)
                {
                    if ((b1 & 8) != 0)
                        r = pixelData[pos++];
                    if ((b1 & 4) != 0)
                        g = pixelData[pos++];
                    if ((b1 & 2) != 0)
                        b = pixelData[pos++];
                    if ((b1 & 1) != 0)
                        a = pixelData[pos++];
                }

                int indexPos2 = ((r ^ g ^ b ^ a) & 63) << 2;
                index[indexPos2] = r;
                index[indexPos2 + 1] = g;
                index[indexPos2 + 2] = b;
                index[indexPos2 + 3] = a;
            }

            img.Data[imgPos++] = b;
            img.Data[imgPos++] = g;
            img.Data[imgPos++] = r;
            img.Data[imgPos++] = a;
        }

        return img;
    }

    public unsafe static byte[] GetArrayFromImage(DSImage img)
    {
        byte[] res = new byte[(img.Width * img.Height * 4) + 12 + 4 + 128]; // default capacity (extra 128 bytes to be safe)
        res[0] = (byte)'f';
        res[1] = (byte)'i';
        res[2] = (byte)'o';
        res[3] = (byte)'q';
        res[4] = (byte)(img.Width & 0xff);
        res[5] = (byte)((img.Width >> 8) & 0xff);
        res[6] = (byte)(img.Height & 0xff);
        res[7] = (byte)((img.Height >> 8) & 0xff);

        int imgPos = 0;
        int imgEnd = img.Data.Length;

        int resPos = 12;
        byte r = 0, g = 0, b = 0, a = 255;
        int run = 0;
        int v = 0, vPrev = 0xff;
        int[] index = new int[64];
        while (imgPos < imgEnd)
        {
            b = img.Data[imgPos];
            g = img.Data[imgPos + 1];
            r = img.Data[imgPos + 2];
            a = img.Data[imgPos + 3];

            v = (r << 24) | (g << 16) | (b << 8) | a;
            if (v == vPrev)
                run++;
            if (run > 0 && (run == 0x2020 || v != vPrev || imgPos == imgEnd - 4))
            {
                if (run < 33)
                {
                    run -= 1;
                    res[resPos++] = (byte)(QOI_RUN_8 | run);
                }
                else
                {
                    run -= 33;
                    res[resPos++] = (byte)(QOI_RUN_16 | (run >> 8));
                    res[resPos++] = (byte)run;
                }
                run = 0;
            }
            if (v != vPrev)
            {
                int indexPos = (r ^ g ^ b ^ a) & 63;
                if (index[indexPos] == v)
                {
                    res[resPos++] = (byte)(QOI_INDEX | indexPos);
                }
                else
                {
                    index[indexPos] = v;

                    int vr = r - ((vPrev >> 24) & 0xff);
                    int vg = g - ((vPrev >> 16) & 0xff);
                    int vb = b - ((vPrev >> 8) & 0xff);
                    int va = a - (vPrev & 0xff);
                    if (vr > -17 && vr < 16 &&
                        vg > -17 && vg < 16 &&
                        vb > -17 && vb < 16 &&
                        va > -17 && va < 16)
                    {
                        if (va == 0 &&
                            vr > -3 && vr < 2 &&
                            vg > -3 && vg < 2 &&
                            vb > -3 && vb < 2)
                        {
                            res[resPos++] = (byte)(QOI_DIFF_8 | (vr << 4 & 48) | (vg << 2 & 12) | (vb & 3));
                        }
                        else if (va == 0 &&
                                    vg > -9 && vg < 8 &&
                                    vb > -9 && vb < 8)
                        {
                            res[resPos++] = (byte)(QOI_DIFF_16 | (vr & 31));
                            res[resPos++] = (byte)((vg << 4 & 240) | (vb & 15));
                        }
                        else
                        {
                            res[resPos++] = (byte)(QOI_DIFF_24 | (vr >> 1 & 15));
                            res[resPos++] = (byte)((vr << 7 & 128) | (vg << 2 & 124) | (vb >> 3 & 3));
                            res[resPos++] = (byte)((vb << 5 & 224) | (va & 31));
                        }
                    }
                    else
                    {
                        res[resPos++] = (byte)(QOI_COLOR | (vr != 0 ? 8 : 0) | (vg != 0 ? 4 : 0) | (vb != 0 ? 2 : 0) | (va != 0 ? 1 : 0));
                        if (vr != 0)
                            res[resPos++] = r;
                        if (vg != 0)
                            res[resPos++] = g;
                        if (vb != 0)
                            res[resPos++] = b;
                        if (va != 0)
                            res[resPos++] = a;
                    }
                }
            }

            vPrev = v;
            imgPos += 4;
        }

        // Add padding
        resPos += 4;
            
        // Write final length
        int length = resPos - 12;
        res[8] = (byte)(length & 0xff);
        res[9] = (byte)((length >> 8) & 0xff);
        res[10] = (byte)((length >> 16) & 0xff);
        res[11] = (byte)((length >> 24) & 0xff);

        return res[..resPos];
    }
}
