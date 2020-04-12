using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IoT.Users.DataFormats
{
    public class Image
    {
        public int Width = 0;
        public int Height = 0;
        private Dictionary<int, byte[]> ImageBin;
        public bool imageLoadOk = false;
        public Image()
        {
            ImageBin = new Dictionary<int, byte[]>();
        }

        public void AddPixelsRLE(int row, int maxRow, byte[] pixelData)
        {
            row = Math.Min(row, 768);
            maxRow = Math.Min(maxRow, 768);
            int px = 0;
            List<byte> lb = new List<byte>();
            foreach (var color_val in pixelData)
            {
                if (px < 1024) //Ограничение ширины картинки
                {
                    var colb = color_val % 8;
                    byte valb = (byte)(color_val - colb);
                    for (int i = 0; i < colb; i++)
                    {
                        if (px < 1024) //Ограничение ширины картинки
                        {
                            lb.Add(valb);
                            px++;
                        }
                    }
                }
            }
            ImageBin[row] = lb.ToArray();
            Width = Math.Max(Width, px);
            Height = Math.Max(Height, maxRow);
            if (row == maxRow)
            {
                imageLoadOk = true;
            }
        }
        public byte[] GetPNG()
        {
            var bmp = new Bitmap(Width, Height);
            foreach (var row_data in ImageBin)
            {
                var lineNum = row_data.Key;
                var lineData = row_data.Value;
                if (lineNum < bmp.Height)
                {
                    int px = 0;
                    foreach (var color_val in lineData)
                    {
                        if (px < bmp.Width)
                        {
                            byte b1 = (byte)(color_val);
                            var col = Color.FromArgb(b1, b1, b1);
                            bmp.SetPixel(px++, lineNum, col);
                        }
                    }
                }
            }
            byte[] byteArray;
            using (MemoryStream memStream = new MemoryStream())
            {
                bmp.Save(memStream, System.Drawing.Imaging.ImageFormat.Png);
                byteArray = memStream.ToArray();
            }
            return byteArray;
        }
    }
}
