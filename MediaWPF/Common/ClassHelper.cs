using System;
using System.Diagnostics;
using System.IO;

namespace MediaWPF.Common
{
    public static class ClassHelper
    {
        public static readonly string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "ffmpeg");

        public static string RunFFmpeg(string args)
        {
            Process process = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(ffmpegPath,"ffmpeg.exe"),
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            process.Start();
            string data = process.StandardError.ReadToEnd();
            data += process.StandardOutput.ReadToEnd();
            process.Close();
            process.Dispose();
            return data;
        }
    }
}
