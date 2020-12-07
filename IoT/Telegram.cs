using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Text;

namespace IoT
{
    public class Telegram
    {
        private const string token = "1413334566:AAFgcGS-l0gkqimAoEJ1xGB02o4-5Nmr8mg";
        private const string user_chatid = "289880579";
        public async static Task SendPhoto(byte[] img, string fileName)
        {
            var url = string.Format("https://api.telegram.org/bot{0}/sendPhoto", token);


            using (var form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(user_chatid.ToString(), Encoding.UTF8), "chat_id");

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
