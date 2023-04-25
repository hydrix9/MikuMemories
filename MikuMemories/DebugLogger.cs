using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;
using System.IO;

namespace MikuMemories {

    public class DualWriter : TextWriter
    {
        private StreamWriter fileWriter;
        private TextWriter consoleWriter;

        public DualWriter(string logFilePath)
        {
            fileWriter = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read));
            consoleWriter = Console.Out;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            fileWriter.Write(value);
            consoleWriter.Write(value);
        }

        public override void WriteLine(string value)
        {
            fileWriter.WriteLine(value);
            consoleWriter.WriteLine(value);
        }

        public override void Flush()
        {
            fileWriter.Flush();
            consoleWriter.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fileWriter.Dispose();
                consoleWriter.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}