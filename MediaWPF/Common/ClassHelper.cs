using MediaInfo;
using System.IO;
using System.Runtime.InteropServices;
using MediaInfoEx = MediaInfo.MediaInfo;

namespace MediaWPF.Common
{
    public static class ClassHelper
    {
        public const string pathFilter = "Video Files|*.3g2;*.3gp;*.3gp2;*.3gpp;*.amrec;*.amv;*.asf;*.avi;*.bik;*.bin;*.crf;*.dav;*.divx;*.drc;*.dv;*.dvr-ms;*.evo;*.f4v;*.flv;*.gvi;*.gxf;*.iso;*.m1v;*.m2v;*.m2t;*.m2ts;*.m4v;*.mkv;*.mov;*.mp2;*.mp2v;*.mp4;*.mp4v;*.mpe;*.mpeg;*.mpeg1;*.mpeg2;*.mpeg4;*.mpg;*.mpv2;*.mts;*.mtv;*.mxf;*.mxg;*.nsv;*.nuv;*.ogg;*.ogm;*.ogv;*.ogx;*.ps;*.rec;*.rm;*.rmvb;*.rpl;*.thp;*.tod;*.tp;*.ts;*.tts;*.txd;*.vob;*.vro;*.webm;*.wm;*.wmv;*.wtv;*.xesc";

        [DllImport("ntdll.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Memcpy(IntPtr dest, IntPtr source, int length);

        /// <summary>
        /// 判断是否为Hdr视频
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool JudgeHdrVideo(string file, out double maxLuminance)
        {
            bool state = false;
            maxLuminance = 600;

            MediaInfoEx mediaInfo = new();
            mediaInfo.Open(file);
            string config = mediaInfo.Inform();
            mediaInfo.Close();
            Dictionary<string, Dictionary<string, string>> keyValues = FormatPairs(config);

            if (keyValues.TryGetValue(StreamKind.Video.ToString(), out Dictionary<string, string> value))
            {
                bool hdrFormat = value.ContainsKey("HDR format");
                bool bt2020 = value.Any(item => item.Key == "Colorprimaries" && item.Value.Contains("BT.2020"));
                bool bit10 = value.Any(item => item.Key == "Bitdepth" && item.Value == "10bits");
                if ((hdrFormat || bt2020) && bit10)
                {
                    state = true;

                    if (value.TryGetValue("Masteringdisplayluminance", out string displayLuminance))
                    {
                        Dictionary<string, string> valuePairs = FormatPairs(displayLuminance.Replace(",", Environment.NewLine), false)["Data"];
                        if (valuePairs.TryGetValue("max", out string lumin))
                        {
                            double sum = Convert.ToDouble(lumin.Replace("cd/m2", string.Empty));
                            if (sum >= 400)
                            {
                                maxLuminance = sum;
                            }
                        }
                    }
                }
            }
            return state;
        }

        public static IntPtr RunMemcpy(IntPtr dest, IntPtr source, int length)
        {
            return Memcpy(dest, source, length);
        }

        private static Dictionary<string, Dictionary<string, string>> FormatPairs(string data, bool isGroup = true)
        {
            Dictionary<string, Dictionary<string, string>> pairs = new();
            if (!isGroup)
            {
                pairs.Add("Data", new Dictionary<string, string>());
            }

            string str = data.Replace(" ", string.Empty);
            StringReader stringReader = new(str);

            while (stringReader.ReadLine() is string line && line != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    if (line.Contains(':') && pairs.LastOrDefault() is KeyValuePair<string, Dictionary<string, string>> pair)
                    {
                        Dictionary<string, string> keyValues = pair.Value;
                        int division = line.IndexOf(':');
                        string key = line[..division];
                        string value = line.Substring(division + 1, line.Length - key.Length - 1);

                        keyValues.TryAdd(key, value);
                    }
                    else if (isGroup)
                    {
                        pairs.Add(line, new Dictionary<string, string>());
                    }
                }
            }

            return pairs;
        }
    }
}
