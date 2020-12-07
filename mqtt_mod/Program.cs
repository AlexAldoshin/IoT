using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mqtt_mod
{
    public struct cpu_type
    {
        public Int32 Vbat;
        public Int32 VbatCPU;
        public Int32 TempCPU;
    };

    public struct net_type
    {
        public Int16 sc_earfcn;
        public Int16 sc_earfcn_offset;
        public Int16 sc_pci;
        public Int32 sc_cellid;
        public Int16 sc_rsrp;
        public Int16 sc_rsrq;
        public Int16 sc_rssi;
        public Int16 sc_snr;
        public Int16 sc_band;
        public Int16 sc_tac;
        public Int16 sc_ecl;
        public Int16 sc_tx_pwr;
        public Int16 sc_re_rsrp;
    };
    public struct gps_type
    {
        public bool valid;
        public float latitude;
        public float longitude;
        public float speed;
        public float course;
        public Int16 fix_type;
        public float pdop;
        public float hdop;
    };

    public struct img_type
    {
        public UInt16 img_size;
        public UInt16 img_x;
        public UInt16 img_y;
        public UInt16 img_width;
        public UInt16 img_height;

    };

    public struct datapack_type
    {
        public byte msg_type;
        public cpu_type cpu_val;
        public net_type net_val;
        public img_type img_val;
    }

    public struct farm1_type
    {
        public UInt16 MSG_num;
        public string IMEI;
        public string IMSI;
        public UInt16 EARFCN;
        public UInt16 PCI;
        public string CellID;
        public byte RSRP;
        public sbyte RSRQ;
        public byte RSSI;
        public sbyte SNR;
        public sbyte ECL;
        public byte TX_POW;
        public sbyte Core_temp;
        public UInt16 VBAT_int;
        public UInt16[] Activity;
    }
    public class recive_img
    {
        public int all_size;
        public int recive_size;
        public byte[] b_img;
        public recive_img(ushort isize)
        {
            all_size = isize;
            recive_size = 0;
            b_img = new byte[isize];
        }
    }

    class Program
    {
        private const string token = "1413334566:AAFgcGS-l0gkqimAoEJ1xGB02o4-5Nmr8mg";
        private const string user_chatid = "289880579";
        private const string Server = "192.168.100.7";
        //private const string Server = "localhost";

        static int recivedMessage;
        static ConcurrentDictionary<string, recive_img> recive_imgs = new ConcurrentDictionary<string, recive_img>();

        static void Main(string[] args)
        {

            param_cache.cache = new Dictionary<string, object>();


            recivedMessage = 0;
            Console.WriteLine("Hello MQTT Client!");
            // Create a new MQTT client.
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            // Use TCP connection.
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(Server, 1883) // localhost
                .WithCredentials("mqtt", "mqtt")
                .WithCleanSession()
                .Build();


            mqttClient.UseDisconnectedHandler(async e =>
            {
                Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    await mqttClient.ConnectAsync(options, CancellationToken.None); // Since 3.0.5 with CancellationToken
                }
                catch
                {
                    Console.WriteLine("### RECONNECTING FAILED ###");
                }
            });

            mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                recivedMessage++;
                Console.WriteLine($"### RECEIVED APPLICATION MESSAGE № {recivedMessage} ###");
                Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                //Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                //Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                //Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                var topicElements = e.ApplicationMessage.Topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (topicElements.Length == 3)
                {
                    if (topicElements[0] == "farms")
                    {
                        var farm = topicElements[1];
                        var animal = topicElements[2];

                        var rMsg = e.ApplicationMessage.Payload;
                        if (rMsg != null && farm == "f1" && rMsg.Length > 1)
                        {
                            if (animal == "period")
                            {
                                var period = BitConverter.ToUInt16(rMsg, 0);
                                Console.WriteLine($"New {farm} period={period}");
                                //Зальем данные в Influx
                                Task task = Task.Run(async () =>
                                {
                                    await InfluxDB_mod.Go_period(farm, period);
                                });
                                return;
                            }
                            if (animal == "actmax")
                            {
                                var actmax = BitConverter.ToUInt16(rMsg, 0);
                                Console.WriteLine($"New {farm} activity threshold={actmax}");
                                //Зальем данные в Influx
                                Task task = Task.Run(async () =>
                                {
                                    await InfluxDB_mod.Go_actmax(farm, actmax);
                                });
                                return;
                            }
                            if (animal.Contains("cow"))
                            {
                                farm1_type datapack_val;

                                datapack_val.MSG_num = BitConverter.ToUInt16(rMsg, 0);
                                datapack_val.IMEI = BitConverter.ToString(rMsg, 2, 8).Replace("-", "");
                                datapack_val.IMSI = BitConverter.ToString(rMsg, 10, 8).Replace("-", "");
                                datapack_val.EARFCN = BitConverter.ToUInt16(rMsg, 18);
                                datapack_val.PCI = BitConverter.ToUInt16(rMsg, 20);
                                datapack_val.CellID = BitConverter.ToString(rMsg, 22, 4).Replace("-", "");
                                datapack_val.RSRP = rMsg[26];
                                datapack_val.RSRQ = unchecked((sbyte)rMsg[27]);
                                datapack_val.RSSI = rMsg[28];
                                datapack_val.SNR = unchecked((sbyte)rMsg[29]);
                                datapack_val.ECL = unchecked((sbyte)rMsg[30]);
                                datapack_val.TX_POW = rMsg[31];
                                datapack_val.Core_temp = unchecked((sbyte)rMsg[32]);
                                datapack_val.VBAT_int = BitConverter.ToUInt16(rMsg, 33);

                                var ActLenght = (rMsg.Length - 35) / 2;
                                datapack_val.Activity = new UInt16[ActLenght];
                                int AllActivity = 0;
                                for (int i = 0; i < datapack_val.Activity.Length; i++)
                                {
                                    UInt16 iAct = BitConverter.ToUInt16(rMsg, 35 + i * 2);
                                    datapack_val.Activity[i] = iAct;
                                    AllActivity += iAct;
                                }
                                Console.WriteLine($"MSG_num={datapack_val.MSG_num}; All activity={AllActivity}:{ActLenght}");

                                //Зальем данные в Influx
                                Task task = Task.Run(async () =>
                                {
                                    await InfluxDB_mod.Go(mqttClient, farm, animal, datapack_val);
                                });
                            }
                        }
                        else if (farm == "f2")
                        {
                            if (rMsg.Length > 1)
                            {
                                if (rMsg[0] == 0)
                                {
                                    datapack_type datapack_val;
                                    datapack_val.msg_type = 0;
                                    datapack_val.cpu_val.Vbat = BitConverter.ToInt32(rMsg, 1);
                                    datapack_val.cpu_val.VbatCPU = BitConverter.ToInt32(rMsg, 5);
                                    datapack_val.cpu_val.TempCPU = BitConverter.ToInt32(rMsg, 9);
                                    datapack_val.net_val.sc_earfcn = BitConverter.ToInt16(rMsg, 13);
                                    datapack_val.net_val.sc_earfcn_offset = BitConverter.ToInt16(rMsg, 15);
                                    datapack_val.net_val.sc_pci = BitConverter.ToInt16(rMsg, 17);
                                    datapack_val.net_val.sc_cellid = BitConverter.ToInt32(rMsg, 19);
                                    datapack_val.net_val.sc_rsrp = BitConverter.ToInt16(rMsg, 23);
                                    datapack_val.net_val.sc_rsrq = BitConverter.ToInt16(rMsg, 25);
                                    datapack_val.net_val.sc_rssi = BitConverter.ToInt16(rMsg, 27);
                                    datapack_val.net_val.sc_snr = BitConverter.ToInt16(rMsg, 29);
                                    datapack_val.net_val.sc_band = rMsg[31];
                                    datapack_val.net_val.sc_tac = BitConverter.ToInt16(rMsg, 32);
                                    datapack_val.net_val.sc_ecl = BitConverter.ToInt16(rMsg, 34);
                                    datapack_val.net_val.sc_tx_pwr = rMsg[36];
                                    datapack_val.net_val.sc_re_rsrp = BitConverter.ToInt16(rMsg, 37);

                                    datapack_val.img_val.img_size = BitConverter.ToUInt16(rMsg, 39);
                                    datapack_val.img_val.img_x = BitConverter.ToUInt16(rMsg, 41);
                                    datapack_val.img_val.img_y = BitConverter.ToUInt16(rMsg, 43);
                                    datapack_val.img_val.img_width = BitConverter.ToUInt16(rMsg, 45);
                                    datapack_val.img_val.img_height = BitConverter.ToUInt16(rMsg, 47);


                                    var ri = new recive_img(datapack_val.img_val.img_size);
                                    recive_imgs[animal] = ri;


                                    //datapack_val.gps_val.valid = BitConverter.ToBoolean(rMsg, 38);
                                    //datapack_val.gps_val.latitude = BitConverter.ToSingle(rMsg, 39);
                                    //if (Single.IsNaN(datapack_val.gps_val.latitude))
                                    //{
                                    //    datapack_val.gps_val.latitude = 0;
                                    //}

                                    //datapack_val.gps_val.longitude = BitConverter.ToSingle(rMsg, 43);
                                    //if (Single.IsNaN(datapack_val.gps_val.longitude))
                                    //{
                                    //    datapack_val.gps_val.longitude = 0;
                                    //}
                                    //datapack_val.gps_val.speed = BitConverter.ToSingle(rMsg, 47);
                                    //if (Single.IsNaN(datapack_val.gps_val.speed))
                                    //{
                                    //    datapack_val.gps_val.speed = 0;
                                    //}
                                    //datapack_val.gps_val.course = BitConverter.ToSingle(rMsg, 51);
                                    //if (Single.IsNaN(datapack_val.gps_val.course))
                                    //{
                                    //    datapack_val.gps_val.course = 0;
                                    //}
                                    //datapack_val.gps_val.fix_type = rMsg[55];
                                    //datapack_val.gps_val.pdop = BitConverter.ToSingle(rMsg, 56);
                                    //if (Single.IsNaN(datapack_val.gps_val.pdop))
                                    //{
                                    //    datapack_val.gps_val.pdop = 0;
                                    //}
                                    //datapack_val.gps_val.hdop = BitConverter.ToSingle(rMsg, 60);
                                    //if (Single.IsNaN(datapack_val.gps_val.hdop))
                                    //{
                                    //    datapack_val.gps_val.hdop = 0;
                                    //}

                                    Console.WriteLine($"Start IMG_Size={datapack_val.img_val.img_size}");

                                    //Зальем данные в Influx
                                    Task task = Task.Run(async () =>
                                    {
                                        await InfluxDB_mod.Go_f2(farm, animal, datapack_val);
                                    });
                                }
                                else
                                {
                                    if (recive_imgs.ContainsKey(animal))
                                    {
                                        var img_block_id = rMsg[0];
                                        var ri = recive_imgs[animal];
                                        Array.Copy(rMsg, 1, ri.b_img, ri.recive_size, rMsg.Length - 1);
                                        ri.recive_size += rMsg.Length - 1;
                                        if (ri.recive_size >= ri.all_size)
                                        {
                                            try
                                            {
                                                var filename = DateTime.Now.ToString().Replace(":", "_").Replace(".", "_").Replace(" ", "_").Replace("/", "_") + ".jpg";
                                                var filepath = "/root/images/" + filename;
                                                System.IO.File.WriteAllBytes(filepath, ri.b_img);
                                                Task task = Task.Run(async () =>
                                                {
                                                    await SendPhoto(ri.b_img, user_chatid, filename, token);
                                                });   
                                            }
                                            catch (Exception ee)
                                            {
                                                Console.WriteLine($"Error {ee.Message}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Неверный топик");
                }
                Console.WriteLine();
            });

            mqttClient.UseConnectedHandler(async e =>
            {
                Console.WriteLine("### CONNECTED WITH SERVER ###");
                // Subscribe to a topic
                await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("farms/#").Build());
                Console.WriteLine("### SUBSCRIBED ###");
            });

            mqttClient.ConnectAsync(options, CancellationToken.None); // Since 3.0.5 with CancellationToken

            Thread.Sleep(Timeout.Infinite);
            Console.WriteLine("Success");
        }


        public async static Task SendPhoto(byte[] img, string chatId, string fileName, string token)
        {
            var url = string.Format("https://api.telegram.org/bot{0}/sendPhoto", token);
           

            using (var form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(chatId.ToString(), Encoding.UTF8), "chat_id");

                using (MemoryStream mStream = new MemoryStream(img))
                {
                    form.Add(new StreamContent(mStream), "photo", fileName);

                    using (var client = new HttpClient())
                    {
                        await client.PostAsync(url, form);
                    }
                }
            }
        }

    }
}
