using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Python.Runtime;


namespace MikuMemories
{
    public static class PythonInterop
    {

        private static dynamic steganoHelper;

        static PythonInterop()
        {
            try
            {
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    sys.path.append(Environment.CurrentDirectory);
                    steganoHelper = Py.Import("stegano_helper");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing PythonInterop: {ex}");
                throw;
            }

        }

        public static string GenerateSummary(string text, double ratio = 0.3)
        {
            using (Py.GIL())
            {
                dynamic summarizer = Py.Import("summarizer");
                string summary = summarizer.generate_summary(text, ratio);
                return summary;
            }
        
        }
        
    }
}