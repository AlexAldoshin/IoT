using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IoT
{
    public class UDP_Service_Wocker : BackgroundService
    {
        int ServerPort = 3310;
        private readonly ILogger<UDP_Service_Wocker> _logger;
        static object locker = new object();
        static object lockerCom = new object();
        private UdpClient ServerIn;

        private static ConcurrentDictionary<string, Users.DataFormats.ImageJPG> AllUsersData;

        public UDP_Service_Wocker(ILogger<UDP_Service_Wocker> logger)
        {
            _logger = logger;
            Startup.udpService = this;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1);
            ServerIn = new UdpClient(ServerPort);
            _logger.LogInformation("ServerPort: {0}", ServerPort);
            if (AllUsersData == null)
            {
                AllUsersData = new ConcurrentDictionary<string, Users.DataFormats.ImageJPG>();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);                
                try
                {
                    IPEndPoint IpEP = null;
                    byte[] data = ServerIn.Receive(ref IpEP); // получаем данные
                    _logger.LogInformation("Recive UDP data {0} bytes from {1} at: {time}", data.Length, IpEP.ToString(), DateTimeOffset.Now);
                    _logger.LogInformation("Data {0}", BitConverter.ToString(data, 0, Math.Min(24, data.Length)));
                    await ToUserAsync(IpEP, data);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Error {0}", ex.Message);
                }
            }
        }

        public delegate void MethodContainer(Guid KeyAPI, string ImagePath);
        public event MethodContainer onNewImage;

        public delegate void ProgressContainer(Guid KeyAPI, int Progress, int All);
        public event ProgressContainer onLoadProgress;

        private async Task ToUserAsync(IPEndPoint IpEP, byte[] data)
        {
            var rok = System.Text.Encoding.UTF8.GetBytes("O_K\n\r");
            ServerIn.Send(rok, rok.Length, IpEP);
            var UserID = IpEP.ToString();

            if (AllUsersData.ContainsKey(UserID))
            {
                var lifeTime = DateTime.Now - AllUsersData[UserID].createTime;
                if (lifeTime.TotalMinutes > 1)
                {
                    Users.DataFormats.ImageJPG tmp;
                    AllUsersData.TryRemove(UserID, out tmp);
                }
            }

            if (!AllUsersData.ContainsKey(UserID))
            {
                var start_Packet = new DataPackets.NBIoT_Start(data);
                AllUsersData[UserID] = new Users.DataFormats.ImageJPG() { nBIoT_Start = start_Packet };

                //Зальем данные в Influx
                Task task = Task.Run(async () =>
                {
                    await InfluxDB.Go(start_Packet);
                    _logger.LogInformation("Send data to InfluxDB at: {time}", DateTimeOffset.Now);
                });

            }
            else
            {
                var ParsePacket = new DataPackets.NBIoT(data);
                if (ParsePacket.DataOk)
                {
                    var start_Packet = AllUsersData[UserID].nBIoT_Start;
                    var KeyAPI = start_Packet.Packet.device_val.KeyAPI;
                    var IMEI = start_Packet.Packet.device_val.IMEI;
                    var BinData = ParsePacket.Data;
                    var row = ParsePacket.Packet.LineID;
                    var maxrow = ParsePacket.Packet.LinesCount;

                    AllUsersData[UserID].AddPartJPG(row, maxrow, BinData);
                    onLoadProgress?.Invoke(KeyAPI, row, maxrow);
                    if (AllUsersData[UserID].imageLoadOk)
                    {
                        Users.DataFormats.ImageJPG imageOut;
                        AllUsersData.TryRemove(UserID, out imageOut);
                        var jpg = imageOut.GetJPG();
                        try
                        {
                            var path = Path.Combine(Environment.CurrentDirectory, "wwwroot", "UsersData");

                           // string TempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "Image1.png");
                            //string TempPath1 = Path.Combine(Environment.CurrentDirectory, "wwwroot", "img", "Image1.png");
                            //string TempPath2 = Path.Combine(hostingEnvironment.ContentRootPath, "wwwroot", "img", "Image1.png");

                            var dirInfo = new DirectoryInfo(path);
                            if (!dirInfo.Exists)
                            {
                                dirInfo.Create();
                            }
                            path = Path.Combine(path, KeyAPI.ToString(), IMEI.ToString());
                            dirInfo = new DirectoryInfo(path);
                            if (!dirInfo.Exists)
                            {
                                dirInfo.Create();                                
                            }
                            var filename = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss.ffff") + ".jpg";
                            path = Path.Combine(path, filename);
                            // создаем объект BinaryWriter
                            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
                            {
                                writer.Write(jpg);
                            }
                            _logger.LogInformation("Create new file {0} at: {time}", filename, DateTimeOffset.Now);
                            onNewImage?.Invoke(KeyAPI, path);
                            //Зальем данные в Influx
                            Task task = Task.Run(async () =>
                            {
                                await InfluxDB.Go_after_OCR(start_Packet, "null", filename);
                                _logger.LogInformation("Send OCR data to InfluxDB at: {time}", DateTimeOffset.Now);
                            });
                            Task task2 = Task.Run(async () =>
                            {
                                await Telegram.SendPhoto(jpg, filename);
                                _logger.LogInformation("Send Foto to Telegram at: {time}", DateTimeOffset.Now);
                            });

                        }
                        catch (Exception e)
                        {
                            _logger.LogInformation("File create error {0} at: {time}", e.Message, DateTimeOffset.Now);
                        }
                    }
                }

            }


            await Task.Delay(1);
        }
    }
}