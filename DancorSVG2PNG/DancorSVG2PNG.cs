using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Diagnostics;
using System;


namespace DancorSVG2PNG
{
    public static class DancorSVG2PNG
    {
        [FunctionName("DancorSVG2PNG")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log, ExecutionContext context)
        {
            log.Info("C# HTTP trigger function processed a request.");

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
            // sample URL: http://dancorinc.com/order/orders/110467_3933_320943.svg
            var uniqueName = GenerateId() + ".svg";
            log.Info("-----------------------");
            log.Info(uniqueName);
            log.Info($"{context.FunctionDirectory}");
            log.Info("-----------------------");

            using (var client = new WebClient())
            {
                client.DownloadFile(svgURL, uniqueName );
            }


            Process proc = new Process();
            try
            {
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.FileName = "java.exe";
                proc.StartInfo.Arguments = "-jar Batik\\batik-rasterizer.jar " + uniqueName;
                proc.Start();
                proc.WaitForExit();
                if (proc.HasExited)
                    log.Info(proc.StandardOutput.ReadToEnd());
                log.Info("java.exe -jar Batik\\batik-rasterizer.jar " + uniqueName);
                log.Info("success!");
            }
            catch (Exception e)
            {
                log.Info("Batik Fail");
                log.Info(e.Message);
            }

            return svgURL == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello " + svgURL);


            
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
