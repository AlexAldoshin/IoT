using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        private static ConcurrentDictionary<Guid, Users.DataFormats.Image> AllUsersData;


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
                AllUsersData = new ConcurrentDictionary<Guid, Users.DataFormats.Image>();
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
            var ParsePacket = new DataPackets.NBIoT(data);
            if (ParsePacket.DataOk)
            {
                var KeyAPI = ParsePacket.KeyAPI;
                var BinData = ParsePacket.Data;
                var row = ParsePacket.LineID;
                var maxrow = ParsePacket.LinesCount;

                if (!AllUsersData.ContainsKey(KeyAPI))
                {
                    AllUsersData[KeyAPI] = new Users.DataFormats.Image();
                }
                AllUsersData[KeyAPI].AddPixelsRLE(row, maxrow, BinData);
                onLoadProgress?.Invoke(KeyAPI, row, maxrow);
                if (AllUsersData[KeyAPI].imageLoadOk)
                {
                    Users.DataFormats.Image imageOut;
                    AllUsersData.TryRemove(KeyAPI, out imageOut);
                    var png = imageOut.GetPNG();
                    try
                    {
                        var path = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\wwwroot\\UsersData\\";
                        var dirInfo = new DirectoryInfo(path);
                        if (!dirInfo.Exists)
                        {
                            dirInfo.Create();
                        }
                        path = path + KeyAPI.ToString();
                        dirInfo = new DirectoryInfo(path);
                        if (!dirInfo.Exists)
                        {
                            dirInfo.Create();
                        }
                        var filename = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss.ffff") + ".png";
                        path = path + "\\" + filename;
                        // создаем объект BinaryWriter
                        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
                        {
                            writer.Write(png);
                        }
                        _logger.LogInformation("Create new file {0} at: {time}", filename, DateTimeOffset.Now);
                        onNewImage?.Invoke(KeyAPI, path);
                    }
                    catch (Exception e)
                    {
                        _logger.LogInformation("File create error {0} at: {time}", e.Message, DateTimeOffset.Now);
                    }
                }
            }
            await Task.Delay(1);
        }
    }
}