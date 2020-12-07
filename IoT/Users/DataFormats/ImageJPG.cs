using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IoT.DataPackets;

namespace IoT.Users.DataFormats
{
    public class ImageJPG
    {
        public NBIoT_Start nBIoT_Start;
        public DateTime createTime;
        private Dictionary<int, byte[]> ImageBin;
        public bool imageLoadOk = false;        
        public ImageJPG()
        {
            ImageBin = new Dictionary<int, byte[]>();
            createTime = DateTime.Now;
        }

        public void AddPartJPG(int block, int maxBlocks, byte[] JPGData)
        {
            ImageBin[block] = JPGData;
            if (block == maxBlocks)
            {
                imageLoadOk = true;               
            }
        }
        public byte[] GetJPG()
        {
            if (imageLoadOk)
            {
                IEnumerable<byte> IEJPG = ImageBin[0];
                if (ImageBin.Count() > 1)
                {
                    for (int i = 1; i < ImageBin.Count(); i++)
                    {
                        IEJPG = IEJPG.Concat(ImageBin[i]);
                    }
                }
                var JPG = IEJPG.ToArray();
                return JPG;
            }
            return null;
        }
    }

}
