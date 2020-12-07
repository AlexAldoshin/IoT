using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IoT.DataPackets
{
    public unsafe class NBIoT
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PacketStructure
        {
            public ushort LineID; //2b
            public ushort LinesCount; //2b                 
        }
        public bool DataOk = false;
        public PacketStructure Packet;   
        public byte[] Data;
        public NBIoT(byte[] packet)
        {
            var psize = Marshal.SizeOf(Packet);
            if (packet.Length > psize) //есть данные
            {
                fixed (byte* p = packet)
                {
                    Data = new byte[packet.Length - psize];
                    Packet = *(PacketStructure*)p;                    
                    Array.Copy(packet, 4, Data, 0, Data.Length);
                    DataOk = true;
                }
            }
        }
    }

    public unsafe class NBIoT_Start
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct Device_type
        {
            public Guid KeyAPI; //16 bytes
            public UInt64 IMEI; //8 bytes
            public UInt64 IMSI; //8 bytes
            public Int32 Vbat;
            public Int32 VbatCPU;
            public Int32 TempCPU;
        };
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct Net_type
        {
            public Int16 sc_earfcn;
            public Int16 sc_earfcn_offset;
            public Int16 sc_pci;
            public UInt32 sc_cellid;
            public Int16 sc_rsrp;
            public Int16 sc_rsrq;
            public Int16 sc_rssi;
            public Int16 sc_snr;
            public byte sc_band;
            public Int16 sc_tac;
            public Int16 sc_ecl;
            public byte sc_tx_pwr;
            public Int16 sc_re_rsrp;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct Datapack_type
        {
            public Device_type device_val;
            public Net_type net_val;
        };


        public Datapack_type Packet;
        public bool DataOk = false;
        
        public NBIoT_Start(byte[] packet)
        {
            var psize = Marshal.SizeOf(Packet);
            if (packet.Length == psize) //есть данные нужной длины
            {
                fixed (byte* p = packet)
                {
                    Packet = *(Datapack_type*)p;
                    DataOk = true;
                }
            }
        }
    }

}
