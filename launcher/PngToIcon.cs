using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class PngToIcon
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: PngToIcon <input.png> <output.ico>");
            return;
        }

        string pngPath = args[0];
        string icoPath = args[1];

        using (Bitmap src = new Bitmap(pngPath))
        {
            int iconSize = 256;

            // Create a 256x256 canvas with transparent background
            using (Bitmap canvas = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;

                    // Scale to fit within 256x256, maintain aspect ratio, center
                    float scale = Math.Min((float)iconSize / src.Width, (float)iconSize / src.Height);
                    int destW = (int)(src.Width * scale);
                    int destH = (int)(src.Height * scale);
                    int destX = (iconSize - destW) / 2;
                    int destY = (iconSize - destH) / 2;

                    g.DrawImage(src, destX, destY, destW, destH);
                }

                // Save as PNG
                byte[] pngData;
                using (MemoryStream pngMs = new MemoryStream())
                {
                    canvas.Save(pngMs, ImageFormat.Png);
                    pngData = pngMs.ToArray();
                }
                Console.WriteLine("PNG data: " + pngData.Length + " bytes");

                // Build ICO with PNG compressed entry
                using (FileStream fs = new FileStream(icoPath, FileMode.Create))
                {
                    // ICO header: 6 bytes
                    fs.WriteByte(0); fs.WriteByte(0);  // reserved
                    fs.WriteByte(1); fs.WriteByte(0);  // type: ICO = 1
                    fs.WriteByte(1); fs.WriteByte(0);  // 1 image

                    // Directory entry: 16 bytes
                    fs.WriteByte(0);                   // width: 0 = 256px
                    fs.WriteByte(0);                   // height: 0 = 256px
                    fs.WriteByte(0);                   // color palette
                    fs.WriteByte(0);                   // reserved
                    fs.WriteByte(1); fs.WriteByte(0);  // color planes: 1
                    fs.WriteByte(32); fs.WriteByte(0); // bpp: 32
                    // Image size (little-endian)
                    WriteLE32(fs, pngData.Length);
                    // Image offset: 6 + 16 = 22
                    WriteLE32(fs, 22);

                    // Image data: raw PNG
                    fs.Write(pngData, 0, pngData.Length);
                }
            }
        }

        // Verify
        try
        {
            using (FileStream fs = new FileStream(icoPath, FileMode.Open))
            {
                byte[] header = new byte[6];
                fs.Read(header, 0, 6);
                bool isIco = (header[2] == 1 && header[3] == 0);
                Console.WriteLine("ICO header valid: " + isIco);
            }
            using (Icon test = new Icon(icoPath))
            {
                Console.WriteLine("Icon valid: " + test.Width + "x" + test.Height);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Verify failed: " + ex.Message);
            Environment.Exit(1);
        }

        Console.WriteLine("Icon created: " + icoPath);
        Console.WriteLine("Size: " + new FileInfo(icoPath).Length + " bytes");
    }

    static void WriteLE32(Stream s, int v)
    {
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)((v >> 8) & 0xFF));
        s.WriteByte((byte)((v >> 16) & 0xFF));
        s.WriteByte((byte)((v >> 24) & 0xFF));
    }
}