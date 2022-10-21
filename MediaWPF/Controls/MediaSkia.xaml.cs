using LibVLCSharp.Shared;
using SkiaSharp;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace MediaWPF.Controls
{
    /// <summary>
    /// MediaSkia.xaml 的交互逻辑
    /// </summary>
    public partial class MediaSkia : UserControl
    {
        [DllImport(@"YUV_RGB.dll")]
        static extern void YUV_RGB_8Bit(int i, int videoWidth, int rgb_width, int yuv_width, IntPtr buffer, IntPtr bufferY, IntPtr bufferU, IntPtr bufferV);

        private readonly string _file;

        #region VLC
        private LibVLC lib;
        private Media media;
        private MediaPlayer mediaplayer;
        private int videoWidth;
        private int videoHeight;
        private IntPtr planeY, planeU, planeV;
        private byte[] _bufferY, _bufferU, _bufferV;
        #endregion

        #region Skia
        private int rgb_width, yuv_width;
        private byte[] buffer;
        private IntPtr plane;
        private WriteableBitmap bitmap;
        private SKImageInfo imageInfo;
        private SKSurface surface;
        private Int32Rect rect;
        #endregion

        public MediaSkia(string file)
        {
            InitializeComponent();

            _file = file;
        }

        private void ConMedia_Loaded(object sender, RoutedEventArgs e)
        {
            lib = new();
            media = new(lib, new Uri(_file), new string[] { "input-repeat=65535" });
            mediaplayer = new(media)
            {
                EnableHardwareDecoding = true
            };
            mediaplayer.SetVideoFormatCallbacks(VideoFormat, null);
            mediaplayer.SetVideoCallbacks(LockVideo, null, DisplayVideo);
            mediaplayer.Play();
        }

        #region VLC解码
        private uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, IntPtr pitches, IntPtr lines)
        {
            Console.WriteLine(Marshal.PtrToStringAnsi(chroma));

            byte[] bytes = Encoding.ASCII.GetBytes("I420");
            for (int i = 0; i < bytes.Length; i++)
            {
                Marshal.WriteByte(chroma, i, bytes[i]);
            }

            int[] pitche = { (int)width, (int)width / 2, (int)width / 2 };
            int[] line = { (int)height, (int)height / 2, (int)height / 2 };

            Marshal.Copy(pitche, 0, pitches, pitche.Length);
            Marshal.Copy(line, 0, lines, pitche.Length);

            _bufferY = new byte[(int)width * (int)height];
            _bufferU = new byte[(int)width * (int)height / 4];
            _bufferV = new byte[(int)width * (int)height / 4];

            planeY = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferY, 0);
            planeU = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferU, 0);
            planeV = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferV, 0);

            if (mediaplayer.Media is Media media)
            {
                foreach (MediaTrack track in media.Tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        Console.WriteLine(Encoding.ASCII.GetString(BitConverter.GetBytes(track.OriginalFourcc)));
                        Console.WriteLine(Encoding.ASCII.GetString(BitConverter.GetBytes(track.Codec)));
                        VideoTrack trackInfo = track.Data.Video;
                        if (trackInfo.Width > 0 && trackInfo.Height > 0)
                        {
                            width = trackInfo.Width;
                            height = trackInfo.Height;
                        }

                        break;
                    }
                }
            }

            videoWidth = (int)width;
            videoHeight = (int)height;

            Dispatcher.Invoke(delegate
            {
                rgb_width = videoWidth * 4;
                yuv_width = videoWidth >> 1;
                buffer = new byte[videoWidth * videoHeight * 4];
                plane = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
                bitmap = new WriteableBitmap(videoWidth, videoHeight, 96, 96, PixelFormats.Pbgra32, null);
                imageInfo = new SKImageInfo(videoWidth, videoHeight, SKColorType.Rgba8888);
                surface = SKSurface.Create(new SKImageInfo(videoWidth, videoHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul), bitmap.BackBuffer, bitmap.BackBufferStride);
                rect = new Int32Rect(0, 0, videoWidth, videoHeight);

                imgMedia.Width = videoWidth;
                imgMedia.Height = videoHeight;
                imgMedia.Source = bitmap;
            });

            return 1;
        }

        private IntPtr LockVideo(IntPtr opaque, IntPtr planes)
        {
            IntPtr[] datas = { planeY, planeU, planeV };
            Marshal.Copy(datas, 0, planes, datas.Length);
            return IntPtr.Zero;
        }

        private void DisplayVideo(IntPtr opaque, IntPtr picture)
        {
            DrawFrame();
        }
        #endregion

        private void DrawFrame()
        {
            Parallel.For(0, videoHeight, i =>
            {
                YUV_RGB_8Bit(i, videoWidth, rgb_width, yuv_width, plane, planeY, planeU, planeV);
            });
            SKImage image = SKImage.FromPixels(imageInfo, plane);
            SKCanvas canvas = surface.Canvas;
            canvas.DrawImage(image, new SKPoint(0, 0));

            Dispatcher.Invoke(delegate
            {
                bitmap.Lock();
                bitmap.AddDirtyRect(rect);
                bitmap.Unlock();
            });
        }
    }
}
