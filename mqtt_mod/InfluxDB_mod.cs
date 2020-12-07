
using AdysTech.InfluxDB.Client.Net;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;



namespace mqtt_mod
{
    static class param_cache
    {
        public static Dictionary<string, object> cache;
    }
    static class InfluxDB_mod
    {
        private const string InfluxUrl = "http://192.168.100.7:8086";
        //private const string InfluxUrl = "http://localhost:8086";


        public static async Task Go(MQTTnet.Client.IMqttClient mqttClient, string farm, string animal, farm1_type datapack_val)
        {
            var curTime = DateTime.UtcNow;
            var client = new InfluxDBClient(InfluxUrl, "farms", "farms");
            var rr = await client.QueryMultiSeriesAsync("iot", $"SELECT LAST(vbat) FROM farms WHERE \"animal\"='{animal}' AND \"farm\"='{farm}'");
            //Если данные еще не грузились то грузим за последний час
            DateTime lastTime;

            // дефолтные значения
            UInt16 period = 60 * 8; // 8 часов
            UInt16 actmax = 1400;


            if (param_cache.cache.ContainsKey($"{farm}_period"))
            {
                period = (UInt16)param_cache.cache[$"{farm}_period"];
            }
            else
            {
                try
                {
                    var rp = await client.QueryMultiSeriesAsync("iot", $"SELECT LAST(period) FROM farms WHERE \"farm\"='{farm}'");
                    period = (UInt16)rp[0].Entries[0].period;
                    param_cache.cache[$"{farm}_period"] = period;
                }
                catch (Exception)
                {

                }
            }

            if (param_cache.cache.ContainsKey($"{farm}_actmax"))
            {
                actmax = (UInt16)param_cache.cache[$"{farm}_actmax"];
            }
            else
            {
                try
                {
                    var ra = await client.QueryMultiSeriesAsync("iot", $"SELECT LAST(actmax) FROM farms WHERE \"farm\"='{farm}'");
                    actmax = (UInt16)ra[0].Entries[0].actmax;
                    param_cache.cache[$"{farm}_actmax"] = actmax;
                }
                catch (Exception)
                {

                }
            }

            try
            {
                lastTime = (DateTime)rr[0].Entries[0].Time;
            }
            catch (Exception)
            {
                lastTime = curTime.AddHours(-1);
            }
            var deltaTime = (curTime - lastTime) / datapack_val.Activity.Length;
            DateTime timeStamp = lastTime;

            for (int i = 0; i < datapack_val.Activity.Length; i++)
            {
                timeStamp = lastTime + deltaTime * (i + 1);

                var valMixeda = new InfluxDatapoint<InfluxValueField>();
                valMixeda.UtcTimestamp = timeStamp;
                valMixeda.Tags.Add("farm", farm);
                valMixeda.Tags.Add("animal", animal);
                valMixeda.Fields.Add("activity", new InfluxValueField(datapack_val.Activity[i]));
                valMixeda.MeasurementName = "farms";
                valMixeda.Precision = TimePrecision.Seconds;
                valMixeda.Retention = new InfluxRetentionPolicy() { Name = "autogen" };

                var ra = await client.PostPointAsync("iot", valMixeda);
            }

            var valMixed = new InfluxDatapoint<InfluxValueField>();
            valMixed.UtcTimestamp = timeStamp;
            valMixed.Tags.Add("farm", farm);
            valMixed.Tags.Add("animal", animal);
            valMixed.Fields.Add("MSG_num", new InfluxValueField(datapack_val.MSG_num));
            valMixed.Tags.Add("IMEI", datapack_val.IMEI);
            valMixed.Tags.Add("IMSI", datapack_val.IMSI);
            valMixed.Fields.Add("EARFCN", new InfluxValueField(datapack_val.EARFCN));
            valMixed.Fields.Add("PCI", new InfluxValueField(datapack_val.PCI));
            valMixed.Tags.Add("CellID", datapack_val.CellID);
            valMixed.Fields.Add("RSRP", new InfluxValueField((int)datapack_val.RSRP));
            valMixed.Fields.Add("RSRQ", new InfluxValueField((int)datapack_val.RSRQ));
            valMixed.Fields.Add("RSSI", new InfluxValueField((int)datapack_val.RSSI));
            valMixed.Fields.Add("SNR", new InfluxValueField((int)datapack_val.SNR));
            valMixed.Fields.Add("ECL", new InfluxValueField((int)datapack_val.ECL));
            valMixed.Fields.Add("TX_POW", new InfluxValueField((int)datapack_val.TX_POW));
            valMixed.Fields.Add("Core_temp", new InfluxValueField((int)datapack_val.Core_temp));
            valMixed.Fields.Add("VBAT_int", new InfluxValueField(datapack_val.VBAT_int));

            valMixed.MeasurementName = "farms";
            valMixed.Precision = TimePrecision.Seconds;
            valMixed.Retention = new InfluxRetentionPolicy() { Name = "autogen" };

            var r = await client.PostPointAsync("iot", valMixed);
            
            //Ответ
            var period_actmax = BitConverter.GetBytes(period).Concat(BitConverter.GetBytes(actmax));
            await Task.Run(() => mqttClient.PublishAsync($"response/{farm}/{animal}", period_actmax));

        }


        public static async Task Go_period(string farm, UInt16 period)
        {
            param_cache.cache[$"{farm}_period"] = period;
            var curTime = DateTime.UtcNow;
            var client = new InfluxDBClient(InfluxUrl, "farms", "farms");
            var valMixed = new InfluxDatapoint<InfluxValueField>();
            valMixed.UtcTimestamp = curTime;
            valMixed.Tags.Add("farm", farm);
            valMixed.Fields.Add("period", new InfluxValueField(period));
            valMixed.MeasurementName = "farms";
            valMixed.Precision = TimePrecision.Seconds;
            valMixed.Retention = new InfluxRetentionPolicy() { Name = "autogen" };
            var r = await client.PostPointAsync("iot", valMixed);
        }

        public static async Task Go_actmax(string farm, UInt16 actmax)
        {
            param_cache.cache[$"{farm}_actmax"] = actmax;
            var curTime = DateTime.UtcNow;
            var client = new InfluxDBClient(InfluxUrl, "farms", "farms");
            var valMixed = new InfluxDatapoint<InfluxValueField>();
            valMixed.UtcTimestamp = curTime;
            valMixed.Tags.Add("farm", farm);
            valMixed.Fields.Add("actmax", new InfluxValueField(actmax));
            valMixed.MeasurementName = "farms";
            valMixed.Precision = TimePrecision.Seconds;
            valMixed.Retention = new InfluxRetentionPolicy() { Name = "autogen" };
            var r = await client.PostPointAsync("iot", valMixed);
        }

        public static async Task Go_f2(string farm, string animal, datapack_type datapack_val)
        {
            var curTime = DateTime.UtcNow;

            var client = new InfluxDBClient(InfluxUrl, "farms", "farms");

            var valMixed = new InfluxDatapoint<InfluxValueField>();
            valMixed.UtcTimestamp = curTime;
            valMixed.Tags.Add("farm", farm);
            valMixed.Tags.Add("animal", animal);
            valMixed.Fields.Add("Vbat", new InfluxValueField(datapack_val.cpu_val.Vbat));
            valMixed.Fields.Add("VbatCPU", new InfluxValueField(datapack_val.cpu_val.VbatCPU));
            valMixed.Fields.Add("TempCPU", new InfluxValueField(datapack_val.cpu_val.TempCPU));
            valMixed.Fields.Add("sc_earfcn", new InfluxValueField(datapack_val.net_val.sc_earfcn));
            valMixed.Fields.Add("sc_earfc_offset", new InfluxValueField(datapack_val.net_val.sc_earfcn_offset));
            valMixed.Fields.Add("sc_pci", new InfluxValueField(datapack_val.net_val.sc_pci));
            valMixed.Tags.Add("sc_cellid", datapack_val.net_val.sc_cellid.ToString());
            valMixed.Fields.Add("sc_rsrp", new InfluxValueField(datapack_val.net_val.sc_rsrp));
            valMixed.Fields.Add("sc_rsrq", new InfluxValueField(datapack_val.net_val.sc_rsrq));
            valMixed.Fields.Add("sc_rssi", new InfluxValueField(datapack_val.net_val.sc_rssi));
            valMixed.Fields.Add("sc_snr", new InfluxValueField(datapack_val.net_val.sc_snr));
            valMixed.Fields.Add("sc_band", new InfluxValueField(datapack_val.net_val.sc_band));
            valMixed.Fields.Add("sc_tac", new InfluxValueField(datapack_val.net_val.sc_tac));
            valMixed.Fields.Add("sc_ecl", new InfluxValueField(datapack_val.net_val.sc_ecl));
            valMixed.Fields.Add("sc_tx_pwr", new InfluxValueField(datapack_val.net_val.sc_tx_pwr));
            valMixed.Fields.Add("sc_re_rsrp", new InfluxValueField(datapack_val.net_val.sc_re_rsrp));
            //valMixed.Tags.Add("valid", datapack_val.gps_val.valid.ToString());
            //valMixed.Fields.Add("latitude", new InfluxValueField(datapack_val.gps_val.latitude));
            //valMixed.Fields.Add("longitude", new InfluxValueField(datapack_val.gps_val.longitude));
            //valMixed.Fields.Add("speed", new InfluxValueField(datapack_val.gps_val.speed));
            //valMixed.Fields.Add("course", new InfluxValueField(datapack_val.gps_val.course));
            //valMixed.Tags.Add("fix_type", datapack_val.gps_val.fix_type.ToString());
            //valMixed.Fields.Add("pdop", new InfluxValueField(datapack_val.gps_val.pdop));
            //valMixed.Fields.Add("hdop", new InfluxValueField(datapack_val.gps_val.hdop));
            valMixed.MeasurementName = "locators";
            valMixed.Precision = TimePrecision.Seconds;
            valMixed.Retention = new InfluxRetentionPolicy() { Name = "autogen" };

            var r = await client.PostPointAsync("iot", valMixed);
        }
    }
}
