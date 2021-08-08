using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AES_NPP_Core
{
    class Program
    {
        private static readonly StringBuilder _stringBuilder = new();


        private static void Main(string[] args)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                if (args.Length < 1)
                    throw new ArgumentOutOfRangeException(nameof(args));

                try
                {

                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        throw new NotImplementedException(_stringBuilder.Append("Not running on: ").Append(nameof(OSPlatform.Windows)).ToString());


                    var password = string.Empty;
                    var fileToDecryptPath = string.Empty;
                    var nppPath = string.Empty;
                    var aesCryptPath = string.Empty;


                    for (int i = 0; i < args.Length; i++)
                    {
                        var arg = args[i];
                        var splitArg = arg.Split('=');
                        if (splitArg.Length < 2) continue;
                        var arg2 = splitArg[0];
                        var val = splitArg[1];
                        if (string.IsNullOrEmpty(arg2) || string.IsNullOrEmpty(val)) continue;

                        if (arg2.Equals("-password", StringComparison.OrdinalIgnoreCase)) password = val;
                        if (arg2.Equals("-path", StringComparison.OrdinalIgnoreCase)) fileToDecryptPath = val;
                        if (arg2.Equals("-aespath", StringComparison.OrdinalIgnoreCase)) aesCryptPath = val;
                        if (arg2.Equals("-npppath", StringComparison.OrdinalIgnoreCase)) nppPath = val;

                    }

                    if (string.IsNullOrEmpty(aesCryptPath)) aesCryptPath = _stringBuilder.Clear().Append(AppDomain.CurrentDomain.BaseDirectory).Append("aescrypt.exe").ToString();


                    if (string.IsNullOrEmpty(fileToDecryptPath))
                    {
                        Console.WriteLine("Empty path specified");
                        return;
                    }

                    if (!File.Exists(fileToDecryptPath))
                    {
                        Console.WriteLine("Invalid path specified (file does not exist)");
                        return;
                    }

                    if (string.IsNullOrEmpty(nppPath) || !File.Exists(nppPath))
                    {
                        Console.WriteLine("Couldn't find np++");
                        return;
                    }

                    if (!File.Exists(aesCryptPath))
                    {
                        Console.WriteLine("Couldn't find aescrypt in path: " + aesCryptPath);
                        return;
                    }

                    if (string.IsNullOrEmpty(password))
                    {
                        Console.WriteLine("Please specify password:");

                        var oldColor = Console.BackgroundColor;
                        var oldForegroundColor = Console.ForegroundColor;

                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Black;


                        try
                        {
                            watch.Stop();
                            password = Console.ReadLine();
                        }
                        finally { watch.Start(); }

                        Console.BackgroundColor = oldColor;
                        Console.ForegroundColor = oldForegroundColor;
                    }

                    var aescryptArgs = _stringBuilder.Clear().Append("-d -p \"").Append(password).Append("\" -o - \"").Append(fileToDecryptPath).Append('"').ToString();


                    var info = new ProcessStartInfo
                    {
                        FileName = aesCryptPath,
                        Arguments = aescryptArgs,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    };


                    var readText = string.Empty;

                    using (var proc = Process.Start(info))
                    {
                        readText = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                    }

                    if (string.IsNullOrEmpty(readText)) throw new ArgumentNullException(nameof(readText));


                    var npInfo = new ProcessStartInfo
                    {
                        FileName = nppPath,
                        Arguments = _stringBuilder.Clear().Append("-multiInst -nosession -noPlugin -notabbar -p0 -qSpeed3 -qt=\"").Append(readText).Append('"').ToString()
                    };

                    Process.Start(npInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exiting with HResult " + ex.HResult + " after we caught an exception: " + Environment.NewLine + ex.ToString());
                    Environment.Exit(ex.HResult);
                }
            }
            finally { Console.WriteLine("Took: " + watch.ElapsedMilliseconds + "ms"); }
        }
    }
}
