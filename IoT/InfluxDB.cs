
using AdysTech.InfluxDB.Client.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace IoT
{
    public static class InfluxDB
    {
        private const string InfluxUrl = "http://192.168.100.7:8086";
        //private const string InfluxUrl = "http://localhost:8086";
        public static async Task Go(DataPackets.NBIoT_Start datapack_val)
        {
            var curTime = DateTime.UtcNow;
            var client = new InfluxDBClient(InfluxUrl, "farms", "farms");
            var valMixed = new InfluxDatapoint<InfluxValueField>();
            valMixed.UtcTimestamp = curTime;
            // Device_type
            valMixed.Tags.Add("KeyAPI", datapack_val.Packet.device_val.KeyAPI.ToString());
            valMixed.Tags.Add("IMEI", datapack_val.Packet.device_val.IMEI.ToString());
            valMixed.Tags.Add("IMSI", datapack_val.Packet.device_val.IMSI.ToString());
            valMixed.Fields.Add("Vbat", new InfluxValueField(datapack_val.Packet.device_val.Vbat));
            valMixed.Fields.Add("VbatCPU", new InfluxValueField(datapack_val.Packet.device_val.VbatCPU));
            valMixed.Fields.Add("TempCPU", new InfluxValueField(datapack_val.Packet.device_val.TempCPU));
            // Net_type
            valMixed.Fields.Add("sc_earfcn", new InfluxValueField((int)datapack_val.Packet.net_val.sc_earfcn));
            valMixed.Fields.Add("sc_earfc_offset", new InfluxValueField((int)datapack_val.Packet.net_val.sc_earfcn_offset));
            valMixed.Tags.Add("sc_pci", datapack_val.Packet.net_val.sc_pci.ToString());
            valMixed.Tags.Add("sc_cellid", datapack_val.Packet.net_val.sc_cellid.ToString());
            valMixed.Fields.Add("sc_rsrp", new InfluxValueField((int)datapack_val.Packet.net_val.sc_rsrp));
            valMixed.Fields.Add("sc_rsrq", new InfluxValueField((int)datapack_val.Packet.net_val.sc_rsrq));
            valMixed.Fields.Add("sc_rssi", new InfluxValueField((int)datapack_val.Packet.net_val.sc_rssi));
            valMixed.Fields.Add("sc_snr", new InfluxValueField((int)datapack_val.Packet.net_val.sc_snr));
            valMixed.Fields.Add("sc_band", new InfluxValueField((int)datapack_val.Packet.net_val.sc_band));
            valMixed.Tags.Add("sc_tac", datapack_val.Packet.net_val.sc_tac.ToString());
            valMixed.Fields.Add("sc_ecl", new InfluxValueField((int)datapack_val.Packet.net_val.sc_ecl));
            valMixed.Fields.Add("sc_tx_pwr", new InfluxValueField((int)datapack_val.Packet.net_val.sc_tx_pwr));
            valMixed.Fields.Add("sc_re_rsrp", new InfluxValueField((int)datapack_val.Packet.net_val.sc_re_rsrp));

            valMixed.MeasurementName = "foto";
            valMixed.Precision = TimePrecision.Seconds;
            valMixed.Retention = new InfluxRetentionPolicy() { Name = "autogen" };
            var r = await client.PostPointAsync("iot", valMixed);
        }

        public static async Task Go_after_OCR(DataPackets.NBIoT_Start datapack_val, string OCR, string filename)
        {
            var curTime = DateTime.UtcNow;
            var client = new InfluxDBClient(InfluxUrl, "farms", "farms");
            var valMixed = new InfluxDatapoint<InfluxValueField>();
            valMixed.UtcTimestamp = curTime;
            // Device_type
            valMixed.Tags.Add("KeyAPI", datapack_val.Packet.device_val.KeyAPI.ToString());
            valMixed.Tags.Add("IMEI", datapack_val.Packet.device_val.IMEI.ToString());
            //valMixed.Tags.Add("IMSI", datapack_val.Packet.device_val.IMSI.ToString());
            //// Net_type         
            //valMixed.Tags.Add("sc_pci", datapack_val.Packet.net_val.sc_pci.ToString());
            //valMixed.Tags.Add("sc_cellid", datapack_val.Packet.net_val.sc_cellid.ToString());

            //valMixed.Tags.Add("sc_tac", datapack_val.Packet.net_val.sc_tac.ToString());
           
            valMixed.Fields.Add("OCR_String", new InfluxValueField(OCR));
            valMixed.Fields.Add("filename", new InfluxValueField(filename));

            float OCR_float;            
            if (float.TryParse(OCR, out OCR_float))
            {
                valMixed.Fields.Add("OCR_Float", new InfluxValueField(OCR_float));
            }

            valMixed.MeasurementName = "foto";
            valMixed.Precision = TimePrecision.Seconds;
            valMixed.Retention = new InfluxRetentionPolicy() { Name = "autogen" };
            var r = await client.PostPointAsync("iot", valMixed);
        }
    }
}
