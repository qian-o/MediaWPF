using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MediaWPF.Common
{
    public static class ClassHelper
    {
        public static readonly string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "ffmpeg");
        public const string pathFilter = "Video Files|*.3g2;*.3gp;*.3gp2;*.3gpp;*.amrec;*.amv;*.asf;*.avi;*.bik;*.bin;*.crf;*.dav;*.divx;*.drc;*.dv;*.dvr-ms;*.evo;*.f4v;*.flv;*.gvi;*.gxf;*.iso;*.m1v;*.m2v;*.m2t;*.m2ts;*.m4v;*.mkv;*.mov;*.mp2;*.mp2v;*.mp4;*.mp4v;*.mpe;*.mpeg;*.mpeg1;*.mpeg2;*.mpeg4;*.mpg;*.mpv2;*.mts;*.mtv;*.mxf;*.mxg;*.nsv;*.nuv;*.ogg;*.ogm;*.ogv;*.ogx;*.ps;*.rec;*.rm;*.rmvb;*.rpl;*.thp;*.tod;*.tp;*.ts;*.tts;*.txd;*.vob;*.vro;*.webm;*.wm;*.wmv;*.wtv;*.xesc";

        [DllImport("ntdll.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Memcpy(IntPtr dest, IntPtr source, int length);

        /// <summary>
        /// FFmpeg执行命令
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string RunFFmpeg(string args)
        {
            Process process = new()
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

        /// <summary>
        /// 判断是否为Hdr视频
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool JudgeHdrVideo(string file)
        {
            string data = RunFFmpeg($"-i \"{file}\"");

            return data.Contains("yuv420p10") && data.Contains("bt2020");
        }

        public static IntPtr RunMemcpy(IntPtr dest, IntPtr source, int length)
        {
            return Memcpy(dest, source, length);
        }
    }
}
