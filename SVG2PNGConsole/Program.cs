using System;
using System.Diagnostics;

namespace SVG2PNGConsole
{
    class Program
    {
        static void Main(string[] args)
        {



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
                    Console.WriteLine(proc.StandardOutput.ReadToEnd());
                Console.WriteLine("java.exe -jar C:\\SourceCode\\DancorSVG2PNG-Function\\SVG2PNGConsole\\Batik\\batik-rasterizer.jar C:\\SourceCode\\DancorSVG2PNG-Function\\SVG2PNGConsole\\Test.svg");
                Console.WriteLine("success!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Batik Fail");
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Hello World!");
            Console.ReadKey(true);
        }
    }
}
