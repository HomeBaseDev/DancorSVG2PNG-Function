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

            log.Info(Environment.GetEnvironmentVariable("JAVA_HOME"));
            log.Info("---------------------");

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
            Directory.CreateDirectory(Path.GetTempPath() + "\\" + uniqueName);
            try {

                foreach (string file in Directory.GetFiles(Path.GetTempPath() + uniqueName))
                {
                    log.Info("File in Directory After Download:");
                    log.Info(file);
                }

                using (var client = new WebClient())
                {
                    client.DownloadFile(svgURL, Path.GetTempPath() + uniqueName + "\\" + uniqueName + ".svg");
                }
                foreach (string file in Directory.GetFiles(Path.GetTempPath() + uniqueName))
                {
                    log.Info("File in Directory After Download:");
                    log.Info(file);
                }
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
                proc.StartInfo.FileName = "D:\\Program Files (x86)\\Java\\jdk1.8.0_73\\bin\\java.exe";
                proc.StartInfo.Arguments = "-jar " + context.FunctionAppDirectory + "\\Batik\\batik-rasterizer.jar -d " + Path.GetTempPath() + uniqueName + "\\" + uniqueName + ".png " + Path.GetTempPath() + uniqueName + "\\" + uniqueName + ".svg";
                proc.Start();
                proc.WaitForExit();
                if (proc.HasExited)
                    log.Info(proc.StandardOutput.ReadToEnd());

                log.Info("Batik Success!");
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

                log.Info(Path.GetTempPath() + uniqueName + ".png");
                ////get Blob reference
                Image imageIn = Image.FromFile(Path.GetTempPath() + uniqueName + "\\" +  uniqueName + ".png");

                byte[] arr;
                using (MemoryStream ms = new MemoryStream())
                {
                    imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    arr = ms.ToArray();
                }


                CloudBlockBlob svgBlob = cloudBlobContainer.GetBlockBlobReference(Path.GetTempPath() + uniqueName + "\\" + uniqueName + ".png");
                svgBlob.Properties.ContentType = "image/png";
                svgBlob.UploadFromByteArray(arr, 0, arr.Length);

                imageIn.Dispose();
                log.Info("Image Upload Success!");

            }
            catch (Exception e)
            {
                log.Info("Image Upload Fail");
                log.Info(e.Message);
            }

            // clean up
            if (File.Exists(Path.GetTempPath() + "\\" + uniqueName + "\\" + uniqueName + ".png"))
                File.Delete(Path.GetTempPath() + "\\" + uniqueName + "\\" + uniqueName + ".png");
            if (File.Exists(Path.GetTempPath() + "\\" + uniqueName + "\\" + uniqueName + ".svg"))
                File.Delete(Path.GetTempPath() + "\\" + uniqueName + "\\" + uniqueName + ".svg");
            if (Directory.Exists(Path.GetTempPath() + "\\" + uniqueName))
                Directory.Delete(Path.GetTempPath() + "\\" + uniqueName);


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
