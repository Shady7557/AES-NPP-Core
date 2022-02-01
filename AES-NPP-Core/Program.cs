using System;
using System.Diagnostics;
using System.IO;
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

                    //  if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    //  throw new NotImplementedException(_stringBuilder.Append("Not running on: ").Append(nameof(OSPlatform.Windows)).ToString());

                    //idk if this runs on non-windows now but it might lol

                    var password = string.Empty;
                    var fileToDecryptPath = string.Empty;
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

                    Console.Clear();

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

                    if (string.IsNullOrEmpty(readText))
                    {
                        Console.WriteLine(nameof(readText) + " was empty. The decrypted file either has no contents or you've entered the wrong password.");
                        Console.Read();
                        return;
                    }

                    Console.WriteLine(_stringBuilder.Clear().Append("--BEGIN DATA STREAM--").Append(Environment.NewLine).Append(Environment.NewLine).Append(readText).Append(Environment.NewLine).Append(Environment.NewLine).Append("--END DATA STREAM--").Append(Environment.NewLine).Append(Environment.NewLine).Append("Press enter to exit").ToString());

                    Console.WriteLine("Press Numpad0 to copy all text. Numpad1 to copy first line. Numpad2 to copy second line. Numpad9 (pgup) to move up one line. Numpad 3 (pgdn) to move down one line. C to copy.");

                    var intendedReadLine = 0;

                    while (true)
                    {
                        var readKey = Console.ReadKey().Key;
                        Console.Write("\b \b");


                        if (readKey == ConsoleKey.D0)
                        {
                            Console.WriteLine("Copied all text to clipboard");

                            TextCopy.ClipboardService.SetText(readText);
                        }
                        else if (readKey == ConsoleKey.D1)
                        {
                            Console.WriteLine("Copied first line to clipboard");

                            using (var reader = new StringReader(readText))
                                TextCopy.ClipboardService.SetText(reader.ReadLine());
                        }
                        else if (readKey == ConsoleKey.D2)
                        {
                            Console.WriteLine("Copied second line to clipboard");

                            using (var reader = new StringReader(readText))
                            {
                                reader.ReadLine(); //skip i guess lol

                                var secondLine = reader?.ReadLine();

                                if (string.IsNullOrEmpty(secondLine))
                                    Console.WriteLine("second line is null/empty. there is no second line.");
                                else TextCopy.ClipboardService.SetText(secondLine);

                            }
                        }
                        else if (readKey == ConsoleKey.D3)
                        {
                            if (intendedReadLine <= 0)
                                Console.WriteLine("Index is already 0");
                            else
                            {
                                intendedReadLine--;
                                Console.WriteLine("moving down an index. now intending to read line: " + intendedReadLine + " press c to copy");
                            }
                        }
                        else if (readKey == ConsoleKey.D9)
                        {
                            intendedReadLine++;
                            Console.WriteLine("moving up an index. now intending to read line: " + intendedReadLine + " press c to copy");
                        }
                        else if (readKey == ConsoleKey.C)
                        {
                            var num = -1;
                            var txt = string.Empty;
                            using (var reader = new StringReader(readText))
                            {

                                while (num < intendedReadLine)
                                {
                                    num++;
                                    txt = reader.ReadLine();
                                }
                                Console.WriteLine("read int: " + num + ": " + txt);
                            }
                            if (string.IsNullOrEmpty(txt))
                                Console.WriteLine("Empty text - not copying");
                            else
                            {
                                TextCopy.ClipboardService.SetText(txt);
                                Console.WriteLine("Copied: " + txt + " to clipboard");
                            }


                        }
                        else if (readKey == ConsoleKey.Enter)
                            break;

                    }

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
