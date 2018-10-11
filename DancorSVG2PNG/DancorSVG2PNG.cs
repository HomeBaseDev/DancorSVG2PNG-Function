using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Diagnostics;
using System;
using System.IO;


namespace DancorSVG2PNG
{
    public static class DancorSVG2PNG
    {
        [FunctionName("DancorSVG2PNG")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, 
            TraceWriter log, ExecutionContext context)
        {
            log.Info("C# HTTP trigger function processed a request.");


            foreach (string folder in Directory.GetDirectories(context.FunctionAppDirectory))
            {
                log.Info(folder);
            }

            foreach (string file in Directory.GetFiles(context.FunctionAppDirectory))
            {
                log.Info(file);
            }
            foreach (string file in Directory.GetFiles(context.FunctionAppDirectory + "\\Assets"))
            {
                log.Info(file);
            }

            // parse query parameter
            string svgURL = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "l", true) == 0)
                .Value;

            if (svgURL == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                svgURL = data?.svgURL;
            }

            // download file from URL
            var uniqueName = GenerateId() ;
            uniqueName = "Test";
            try {
                HttpWebRequest request =  (HttpWebRequest)HttpWebRequest.Create(svgURL);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream s = response.GetResponseStream();
                FileStream os = new FileStream(Path.GetTempPath() + "\\" + uniqueName + ".svg", FileMode.OpenOrCreate, FileAccess.Write);
                byte[] buff = new byte[102400];
                int c = 0;
                while ((c = s.Read(buff, 0, 10400)) > 0)
                {
                    os.Write(buff, 0, c);
                    os.Flush();
                }
                os.Close();
                s.Close();


                //using (var client = new WebClient())
                //{
                //    client.DownloadFile(svgURL, uniqueName + ".svg" );
                //}
            }
            catch (Exception e)
            {
                log.Info("Download Fail");
                log.Info(e.Message);
            }

            Process proc = new Process();
            try
            {
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.FileName = "java.exe";
                proc.StartInfo.Arguments = "-jar Batik\\batik-rasterizer.jar " + Path.GetTempPath() + "\\" + uniqueName + ".svg";
                proc.Start();
                proc.WaitForExit();
                if (proc.HasExited)
                    log.Info(proc.StandardOutput.ReadToEnd());
                log.Info("java.exe -jar Batik\\batik-rasterizer.jar " + Path.GetTempPath() + "\\" + uniqueName + ".svg");
                log.Info("success!");
            }
            catch (Exception e)
            {
                log.Info("Batik Fail");
                log.Info(e.Message);
            }

            try
            {
                // upload file to blob storage
                string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnection);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                //create a container CloudBlobContainer 
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("svg2png");

                log.Info(Path.GetTempPath() + "\\" + uniqueName + ".png");
                ////get Blob reference
                Image imageIn = Image.FromFile(Path.GetTempPath() + "\\" +  uniqueName + ".png");

                byte[] arr;
                using (MemoryStream ms = new MemoryStream())
                {
                    imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    arr = ms.ToArray();
                }


                CloudBlockBlob svgBlob = cloudBlobContainer.GetBlockBlobReference(uniqueName + ".png");
                svgBlob.Properties.ContentType = "image/png";
                svgBlob.UploadFromByteArray(arr, 0, arr.Length);

                imageIn.Dispose();

            }
            catch (Exception e)
            {
                log.Info("Image Upload Fail");
                log.Info(e.Message);
            }

            // clean up
            if (File.Exists(context.FunctionAppDirectory + "\\" + uniqueName + ".png"))
            {
                File.Delete(context.FunctionAppDirectory + "\\" + uniqueName + ".png");
            }
            if (File.Exists(context.FunctionAppDirectory + "\\" + uniqueName + ".svg"))
            {
                File.Delete(context.FunctionAppDirectory + "\\" + uniqueName + ".svg");
            }


            return svgURL == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a url on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, uniqueName);   
        }

        private static string GenerateId()
        {
            long i = 1;
            foreach (byte b in Guid.NewGuid().ToByteArray())
            {
                i *= ((int)b + 1);
            }
            return string.Format("{0:x}", i - DateTime.Now.Ticks);
        }

    }
}
