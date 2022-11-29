using Sentry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;
using Sentry;

namespace SenderELMA
{
    internal class Program
    {
        public static string dir = "";
        public static string fileLog = "";
        public static string log = "";
        public static string sentryDSN = "https://c77aa8df58e94a1caefc0102d6a81ab2@o1108631.ingest.sentry.io/6642637";

        static void Main(string[] args)
        {
            using (SentrySdk.Init(o =>
            {
                o.Dsn = sentryDSN;
                o.Debug = true;
                o.TracesSampleRate = 1.0;
            }))
            {
                string dir = ConfigurationManager.AppSettings.Get("elma_directory_for_log_file");

                dir += @"Logs";

                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                if (!dirInfo.Exists) dirInfo.Create();
                Console.WriteLine("Path to logs: " + dir);
                fileLog = Path.Combine(dir, "Logs-" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
                File.AppendAllText(fileLog, "Старт новой синхронизации\n");

                //DateTime currentTime = DateTime.Now;

                try
                {
                    while (true)
                    {
                        try
                        {
                            var task = UploadFile();
                            task.Wait();
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine("Произошла ошибка. Проверьте правильность введенных данных. \nТекст ошибки: " + err.Message);
                            SentrySdk.CaptureException(err);
                        }
                        log = "Обработка файлов завершена.\n";
                        Console.WriteLine(log);
                        File.AppendAllText(fileLog, DateTime.Now.ToLongTimeString() + " " + log + "\n");

                        Console.WriteLine(log);
                        File.AppendAllText(fileLog, DateTime.Now.ToLongTimeString() + " " + "\n");
                        Console.WriteLine("***\n");
                        Console.WriteLine("Не закрывайте окно\n");
                        File.AppendAllText(fileLog, "Передача файлов завершена\r\n\r\n");
                        Thread.Sleep(60000);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("Произошла ошибка.\nТекст ошибки: " + err.ToString());
                    SentrySdk.CaptureException(err);
                }
            }
        }

        internal static async Task<bool> UploadFile()
        {
            string directory = ConfigurationManager.AppSettings.Get("elma_folder_to_sending");
            var url = ConfigurationManager.AppSettings.Get("elma_url");
            var token = ConfigurationManager.AppSettings.Get("elma_token");
            if ((dir.Contains("'") == true) || (dir.Contains('"') == true))
            {
                Console.WriteLine("Неверно указана директория!");
            }
            if ((directory.Contains("'") == true) || (directory.Contains('"') == true))
                {
                    Console.WriteLine("Неверно указана директория!");
                }
            if ((url.Contains("http") != true) || (url.Contains("'") == true) || (url.Contains('"') == true))
            {
                Console.WriteLine("Неверно указана ссылка!");
            }
            if ((token.Contains("'") == true) || (token.Contains('"') == true))
            {
                Console.WriteLine("Неверно указан токен!");
            }

            string[] filePaths = Directory.GetFiles(directory);
            try
            {
                foreach (var filePath in filePaths)
                {
                    Guid guid = Guid.NewGuid();
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri($"{url}?hash={guid}");
                    var fileName = Path.GetFileName(filePath);
                    var request = new HttpRequestMessage(HttpMethod.Post, "");
                    var stream = System.IO.File.OpenRead(filePath);
                    request.Headers.Clear();
                    request.Headers.Add("X-TOKEN", token.ToString());

                    var content = new MultipartFormDataContent
                    {
                            {new StreamContent(stream), "file", fileName}
                    };
                    request.Content = content;

                    HttpResponseMessage result = await client.SendAsync(request);
                    Console.WriteLine("Обработка файла " + fileName);
                    File.AppendAllText(fileLog, DateTime.Now.ToLongTimeString() + " файл " + fileName + " был отправлен " + "\n" + log + "\n");
                    
                    if (result.IsSuccessStatusCode)
                    {
                        File.AppendAllText(fileLog, DateTime.Now.ToLongTimeString() + " файл " + fileName + " удален из " + directory + "\n" + log + "\n");
                        Console.WriteLine("Файл " + fileName + " удален");
                        File.Delete(Path.Combine(filePath));
                    }
                    else
                    {
                        File.AppendAllText(fileLog, DateTime.Now.ToLongTimeString() + " файл " + fileName + " не может быть удален из " + directory + "\n" + log + "\n");
                        Console.WriteLine("Файл не удален");
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Произошла ошибка. \nТекст ошибки: " + err.ToString());
                SentrySdk.CaptureException(err);
            }
            return true;
        }
    }
}
