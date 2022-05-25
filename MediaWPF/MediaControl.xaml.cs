using LibVLCSharp.Shared;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using GlPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using System.IO;

namespace MediaWPF
{
    /// <summary>
    /// MediaControl.xaml 的交互逻辑
    /// </summary>
    public partial class MediaControl : UserControl
    {
        private Uri _uri;
        private LibVLC _lib;
        private Media _media;
        private MediaPlayer _mediaplayer;
        private byte[] _bytes;
        private byte[] _bytes_rgb;
        private IntPtr _intPtr_rgb;
        private int videoWidth;
        private int videoHeight;

        public MediaControl()
        {
            InitializeComponent();

            GLWpfControlSettings gLWpfControlSettings = new()
            {
                MajorVersion = 2,
                MinorVersion = 1,
                RenderContinuously = false
            };
            OpenTkControl.Start(gLWpfControlSettings);
        }

        private void UserMain_Loaded(object sender, RoutedEventArgs e)
        {
            
            _uri = new(@"E:\BaiduNetdiskDownload\20220114_185308.mp4");
            // _uri = new(@"E:\BaiduNetdiskDownload\[A]ddiction _2160p_HDR_Extreme.mp4");
            // _uri = new(@"E:\BaiduNetdiskDownload\NARAKA  BLADEPOINT 1080p 60.mp4");
            // _uri = new(@"E:\BaiduNetdiskDownload\NARAKA  BLADEPOINT 3440p 60.mp4");
            _lib = new();
            // avcodec-hw={any,d3d11va,dxva2,none}
            _media = new(_lib, _uri, new string[] { "input-repeat=65535" });
            _mediaplayer = new(_media);

            _mediaplayer.SetVideoFormatCallbacks(VideoFormat, null);
            _mediaplayer.SetVideoCallbacks(LockVideo, null, DisplayVideo);
            _mediaplayer.Mute = true;
            _mediaplayer.Play();
        }

        private void UserMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        public uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, IntPtr pitches, IntPtr lines)
        {
            // YUYV RV32 I420
            byte[] bytes = Encoding.ASCII.GetBytes("I420");
            for (var i = 0; i < bytes.Length; i++)
            {
                Marshal.WriteByte(chroma, i, bytes[i]);
            }

            if (_mediaplayer.Media is Media media)
            {
                foreach (MediaTrack track in media.Tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        VideoTrack trackInfo = track.Data.Video;
                        if (trackInfo.Width > 0 && trackInfo.Height > 0)
                        {
                            width = trackInfo.Width;
                            height = trackInfo.Height;
                            if (trackInfo.SarDen != 0)
                            {
                                width = width * trackInfo.SarNum / trackInfo.SarDen;
                            }
                        }

                        break;
                    }
                }
            }

            int[] pitche = { (int)width, (int)width / 2, (int)width / 2 };
            int[] line = { (int)height, (int)height / 2, (int)height / 2 };
            Marshal.Copy(pitche, 0, pitches, pitche.Length);
            Marshal.Copy(line, 0, lines, pitche.Length);

            _bytes = new byte[width * height * 12 / 8];
            opaque = Marshal.UnsafeAddrOfPinnedArrayElement(_bytes, 0);

            _bytes_rgb = new byte[width * height * 3];
            _intPtr_rgb = Marshal.UnsafeAddrOfPinnedArrayElement(_bytes_rgb, 0);

            videoWidth = (int)width;
            videoHeight = (int)height;
            return 1;

        }

        public IntPtr LockVideo(IntPtr opaque, IntPtr planes)
        {
            IntPtr pY_ptr = Marshal.UnsafeAddrOfPinnedArrayElement(_bytes, 0);
            IntPtr pU_ptr = Marshal.UnsafeAddrOfPinnedArrayElement(_bytes, videoWidth * videoHeight);
            IntPtr pV_ptr = Marshal.UnsafeAddrOfPinnedArrayElement(_bytes, videoWidth * videoHeight + videoWidth * videoHeight / 4);
            IntPtr[] datas = { pY_ptr, pU_ptr, pV_ptr };
            Marshal.Copy(datas, 0, planes, datas.Length);

            return IntPtr.Zero;
        }

        public void DisplayVideo(IntPtr opaque, IntPtr picture)
        {
            int rgb_width, u_width;
            rgb_width = videoWidth * 3;
            u_width = (videoWidth >> 1);
            int ypSize = videoWidth * videoHeight;
            int upSize = ypSize >> 2;
            int offSet = 0;
            byte[] pY = _bytes;
            byte[] pU = pY.Skip(ypSize).ToArray();
            byte[] pV = pU.Skip(videoWidth * videoHeight / 4).ToArray();
            for (int i = 0; i < videoHeight; i++)
            {
                for (int j = 0; j < videoWidth; j++)
                {
                    byte Y = pY[videoWidth * i + j];
                    offSet = (i >> 1) * (u_width) + (j >> 1);
                    byte V = pV[offSet];
                    byte U = pU[offSet];
                    byte R = Convert_ADJUST(Y + 1.402 * (V - 128)); //R
                    byte G = Convert_ADJUST(Y - 0.34413 * (U - 128) - 0.71414 * (V - 128)); //G
                    byte B = Convert_ADJUST(Y + 1.772 * (U - 128)); //B

                    offSet = rgb_width * i + j * 3;

                    _bytes_rgb[offSet] = R;
                    _bytes_rgb[offSet + 1] = G;
                    _bytes_rgb[offSet + 2] = B;
                }
            }
            Dispatcher.Invoke(delegate
            {
                OpenTkControl.InvalidateVisual();
            });
        }

        private void OpenTkControl_OnRender(TimeSpan obj)
        {
            GL.ClearColor(Color4.AliceBlue);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            if (_bytes_rgb != null)
            {
                GL.RasterPos3(-1.0f, 1.0f, 0);
                GL.PixelZoom((float)OpenTkControl.ActualWidth / videoWidth, -(float)OpenTkControl.ActualHeight / videoHeight);
                GL.DrawPixels(videoWidth, videoHeight, GlPixelFormat.Rgb, PixelType.UnsignedByte, _intPtr_rgb);
            }
        }


        private static byte Convert_ADJUST(double tmp)
        {
            return (byte)((tmp >= 0 && tmp <= 255) ? tmp : (tmp < 0 ? 0 : 255));
        }
    }
}
