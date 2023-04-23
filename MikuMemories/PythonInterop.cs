using System;
using Python.Runtime;


namespace MikuMemories
{
    public static class PythonInterop
    {
        static PythonInterop()
        {
            PythonEngine.Initialize();
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