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
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            if (name == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                name = data?.name;
            }

            Process proc = new Process();
            try
            {
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.FileName = "java.exe";
                proc.StartInfo.Arguments = "-jar C:\\SourceCode\\DancorSVG2PNG-Function\\SVG2PNGConsole\\Batik\\batik-rasterizer.jar C:\\SourceCode\\DancorSVG2PNG-Function\\SVG2PNGConsole\\Test.svg";
                proc.Start();
                proc.WaitForExit();
                if (proc.HasExited)
                    log.Info(proc.StandardOutput.ReadToEnd());
                log.Info("java.exe -jar C:\\SourceCode\\DancorSVG2PNG-Function\\SVG2PNGConsole\\Batik\\batik-rasterizer.jar C:\\SourceCode\\DancorSVG2PNG-Function\\SVG2PNGConsole\\Test.svg");
                log.Info("success!");
            }
            catch (Exception e)
            {
                log.Info("Batik Fail");
                log.Info(e.Message);
            }

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);


            
        }
    }
}
