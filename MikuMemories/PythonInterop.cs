using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Python.Runtime;


namespace MikuMemories
{
    public class PythonInterop
    {

        private static dynamic steganoHelper;

        public PythonInterop()
        {
            try
            {


                string pythonLibName;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    pythonLibName = "python310.dll";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    pythonLibName = "libpython3.so";
                }
                else
                {
                    Console.WriteLine("Unsupported platform.");
                    return;
                }

                
                string[] searchDirectories = {
                    AppDomain.CurrentDomain.BaseDirectory,
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    Directory.GetCurrentDirectory()
                };
                
                string pythonPath;
                string pythonHome;
                
                /*
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    pythonPath = baseDir + @"PythonNet\Windows";
                    pythonHome = baseDir + @"PythonNet\Windows";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    pythonPath = baseDir + "PythonNet/Linux/python/install/lib/libpython3.so";
                    pythonHome = baseDir + "PythonNet/Linux/python/install/";
                }
                else
                {
                    throw new NotSupportedException("Platform not supported");
                }
                */

                //Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", baseDir + "PythonNet/Linux/python/install/lib/");
                //Runtime.PythonDLL = pythonPath;
                //Runtime.PythonDLL = "/home/io/MikuMemories/MikuMemories/Python_old/lib/libpython3.so";

                Runtime.PythonDLL = Config.GetValue("pythonDll");
                PythonEngine.PythonHome = Config.GetValue("pythonHome");

                //PythonEngine.PythonHome = pythonHome;
                
                Console.WriteLine($"PythonDLL set to: {Runtime.PythonDLL}");

                /*
                string[] searchDirectories = {
                    "/home/io/miniconda3/bin/"
                }; 
                */
                /*
                string pythonLibPath = null;

                foreach (string searchDirectory in searchDirectories)
                {
                    pythonLibPath = FindPythonLib(searchDirectory, pythonLibName);
                    if (pythonLibPath != null)
                    {
                        break;
                    }
                }

                if (pythonLibPath == null)
                {
                    Console.WriteLine($"{pythonLibName} not found.");
                    return;
                }

                string pythonHome = FindPathUpward(pythonLibPath, "bin", 6);
                string pythonStdLibPath = FindPathUpward(pythonLibPath, "python3.10", 6);
                string pythonLibDynloadPath = FindPathUpward(pythonLibPath, "lib-dynload", 6);
                string pythonSitePackagesPath = FindPathUpward(pythonLibPath, "site-packages", 6);

                Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
                PythonEngine.PythonHome = pythonHome;

                string pythonPath = $"{pythonStdLibPath}{Path.PathSeparator}{pythonLibDynloadPath}{Path.PathSeparator}{pythonSitePackagesPath}";
                Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath);

                Runtime.PythonDLL = pythonLibPath;
                Console.WriteLine($"PythonDLL set to: {Runtime.PythonDLL}");
                */

                PythonEngine.Initialize();
                PythonEngine.BeginAllowThreads();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing PythonInterop: {ex}");
                throw;
            }
        }

        string FindPathUpward(string startPath, string targetName, int maxLevelsUp)
        {
            string currentPath = startPath;
            for (int i = 0; i <= maxLevelsUp; i++)
            {
                if (Directory.Exists(currentPath))
                {
                    string[] directories = Directory.GetDirectories(currentPath);
                    foreach (string dir in directories)
                    {
                        if (Path.GetFileName(dir).Equals(targetName))
                        {
                            return dir;
                        }
                    }
                }

                currentPath = Path.GetDirectoryName(currentPath);
            }

            return null;
        }

       
        static string FindPythonLib(string searchDirectory, string fileName)
        {
            if (!Directory.Exists(searchDirectory)) return null;

            foreach (string file in Directory.GetFiles(searchDirectory))
            {
                if (Path.GetFileName(file).Equals(fileName))
                {
                    return file;
                }
            }

            foreach (string subDirectory in Directory.GetDirectories(searchDirectory))
            {
                string result = FindPythonLib(subDirectory, fileName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }


        public static float[] GenerateEmbedding(string text)
        {
            dynamic py = Py.Import("__main__");
            Py.Import("embeddings"); // Replace with the name of your Python script

            PyObject result = py.generate_embedding(text);
            float[] embedding = result.As<float[]>();

            return embedding;
        }





        public static async Task<string> GenerateSummary(string text, double ratio = 0.3)
        {
            
            string summary = null;
            Exception exception = null;

            await Program.MainThreadSemaphore.WaitAsync();

            try
            {

                using (Py.GIL())
                {
                    dynamic site = Py.Import("site");
                    Console.WriteLine("python site-packages: " + site.getsitepackages());
                    dynamic os = Py.Import("os");
                    dynamic sys = Py.Import("sys");

                    Console.WriteLine("Python executable path: " + sys.executable.ToString());

                    sys.path.append(os.getcwd());
                    sys.path.append("/home/io/MikuMemories/MikuMemories/");

                    dynamic summarizer = Py.Import("summarizer");
                    summary = summarizer.generate_summary(text, ratio);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                Console.WriteLine("Exception in GenerateSummary: " + ex.Message);
            }
            finally
            {
                Program.MainThreadSemaphore.Release();
            }

            if (exception != null)
            {
                throw exception;
            }

            return summary;
        }

    }
}