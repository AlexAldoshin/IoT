using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoT.DataPackets
{
    public class NBIoT
    {
        public bool DataOk = false;
        public Guid KeyAPI;  //16b
        public ushort LineID; //2b
        public ushort LinesCount; //2b       
        public byte[] Data;
        public NBIoT(byte[] packet)
        {
            if (packet.Length > 20)
            {
                var StrGUID = BitConverter.ToString(packet, 0, 16);
                //Ключ пользователя
                KeyAPI = new Guid(StrGUID.Replace("-", ""));
                //Номер строки в кадре (от 1)
                LineID = BitConverter.ToUInt16(packet, 16);
                //Всего строк в кадре
                LinesCount = BitConverter.ToUInt16(packet, 18);
                //Данные строки в RLE 5 бит яркость 3 бита кол-во повторов
                Data = new byte[packet.Length - 20]; 
                Array.Copy(packet, 20, Data, 0, Data.Length);
                DataOk = true;
            }
        }
    }
}
