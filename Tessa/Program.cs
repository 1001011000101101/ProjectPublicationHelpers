using Microsoft.Web.Administration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Tessa
{
    class Program
    {
        static int Main(string[] args)
        {
            int result = 0;
            try
            {

                var parameters = GetParams(args);
                string app = string.Empty;

                if (!parameters.TryGetValue(Constants.AppKey, out app))
                    throw new Exception(Constants.AppKey + Constants.WasFoundErrorMessage);


                switch (app)
                {
                    case Constants.Server:
                        PublicationServerExtension();
                        break;

                    case Constants.Client:
                        PublicationClientExtension();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
            }

            return result;
        }

        private static void  PublicationServerExtension()
        {
            string result = string.Empty;

            //stop server
            string path = Path.Combine(Constants.TessaDALUrl, Constants.IisStop);
            var stringContent = new StringContent(JsonConvert.SerializeObject(new { }), Encoding.UTF8, Constants.MediaTypeApplicationJson);
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage res = client.PostAsync(path, stringContent).Result)
                {
                    using (HttpContent content = res.Content)
                    {
                        string data = content.ReadAsStringAsync().Result;
                        if (data != null)
                        {
                            result = data;
                        }
                    }
                }
            }

            // Copy files
            foreach (string file in Directory.GetFiles(Constants.TessaSourseExtensions, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, Path.Combine(Constants.TessaDestinationExtensions, Path.GetFileName(file)), true);
            }


            //start server
            path = Path.Combine(Constants.TessaDALUrl, Constants.IisStart);
            stringContent = new StringContent(JsonConvert.SerializeObject(new { }), Encoding.UTF8, Constants.MediaTypeApplicationJson);
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage res = client.PostAsync(path, stringContent).Result)
                {
                    using (HttpContent content = res.Content)
                    {
                        string data = content.ReadAsStringAsync().Result;
                        if (data != null)
                        {
                            result = data;
                        }
                    }
                }
            }
        }

        private static void PublicationClientExtension()
        {
            //stop client
            var processes = Process.GetProcesses().Where(pr => pr.ProcessName == Constants.TessaClient);
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit(1000 * 20);
            }

            // Copy files
            foreach (string file in Directory.GetFiles(Constants.TessaSourseClientExtensions, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, Path.Combine(Constants.TessaDestinationClientExtensions, Path.GetFileName(file)), true);
            }

            var t = Task.Run(() => 
            { 
                Process.Start(Path.Combine(Constants.TessaDestinationClientExtensions, Constants.TessaClient + ".exe")); 
            });
            t.Wait();
            
        }


        //
        // Summary:
        //     Конвертирует неудобный string[] args в удобный IDictionary
        //     
        private static Dictionary<string, string> GetParams(string[] args)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            if (args.Length % 2 != 0)
                throw new Exception(Constants.ArgsErrorMessage);

            for (int i = 0; i < args.Length; i++)
            {
                if (i % 2 != 0)
                {
                    parameters.Add(args[i - 1], args[i]);
                }
            }

            return parameters;
        }
    }
}
