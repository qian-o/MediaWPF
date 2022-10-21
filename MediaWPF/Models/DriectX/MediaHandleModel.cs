using LibVLCSharp.Shared;
using MediaWPF.Common;
using SharpDX;
using SharpDX.Direct3D9;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace MediaWPF.Models.DriectX
{
    public class MediaHandleModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region 变量
        #region VLC
        private LibVLC _lib;
        private Media _media;
        private MediaPlayer _mediaplayer;
        private byte[] _bufferY, _bufferUV;
        private int _sizeY, _sizeUV;
        private IntPtr _planeY, _planeUV;
        #endregion
        #region Direct3D
        private Direct3DEx _direct3D;
        private DisplayModeEx _displayMode;
        private DeviceEx _device;
        private Texture _texture;
        private Surface _textureSurface;
        private Surface _surface;
        private bool isInitTexture;
        #endregion
        private readonly string _file;
        private readonly bool _hdr;
        #endregion

        #region 属性
        private FileInfo videoFileInfo;
        private int videoWidth;
        private int videoHeight;
        private D3DImage d3DImage;
        private Int32Rect imageRect;

        /// <summary>
        /// 视频文件信息
        /// </summary>
        public FileInfo VideoFileInfo
        {
            get => videoFileInfo;
            set
            {
                videoFileInfo = value;
                OnPropertyChanged(nameof(VideoFileInfo));
            }
        }

        /// <summary>
        /// 视频宽度
        /// </summary>
        public int VideoWidth
        {
            get => videoWidth;
            set
            {
                videoWidth = value;
                OnPropertyChanged(nameof(VideoWidth));
            }
        }

        /// <summary>
        /// 视频高度
        /// </summary>
        public int VideoHeight
        {
            get => videoHeight;
            set
            {
                videoHeight = value;
                OnPropertyChanged(nameof(VideoHeight));
            }
        }

        /// <summary>
        /// 画面
        /// </summary>
        public D3DImage D3DImage
        {
            get => d3DImage;
            set
            {
                d3DImage = value;
                OnPropertyChanged(nameof(D3DImage));
            }
        }

        /// <summary>
        /// 画面形状
        /// </summary>
        public Int32Rect ImageRect
        {
            get => imageRect;
            set
            {
                imageRect = value;
                OnPropertyChanged(nameof(ImageRect));
            }
        }
        #endregion

        public MediaHandleModel(string file, bool hdr)
        {
            _file = file;
            _hdr = hdr;
            VideoFileInfo = new FileInfo(_file);
        }

        /// <summary>
        /// 控件初始化
        /// </summary>
        public void Media_Loaded(double dpiScaleX = 1.0, double dpiScaleY = 1.0)
        {
            D3DImage = new D3DImage(dpiScaleX, dpiScaleY);

            _lib = new();
            _media = new(_lib, new Uri(VideoFileInfo.FullName), new string[] { "input-repeat=65535" });
            _mediaplayer = new(_media)
            {
                EnableHardwareDecoding = true
            };
            _mediaplayer.SetVideoFormatCallbacks(VideoFormat, null);
            _mediaplayer.SetVideoCallbacks(LockVideo, null, DisplayVideo);
            _mediaplayer.Play();
        }

        /// <summary>
        /// 刷新图像
        /// </summary>
        public void RefreshImage()
        {
            if (isInitTexture)
            {
                D3DImage.Lock();
                D3DImage.AddDirtyRect(ImageRect);
                D3DImage.Unlock();
            }
        }

        #region VLC解码
        private uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, IntPtr pitches, IntPtr lines)
        {
            Console.WriteLine(Marshal.PtrToStringAnsi(chroma));

            byte[] bytes = Encoding.ASCII.GetBytes(_hdr ? "P010" : "NV12");
            for (int i = 0; i < bytes.Length; i++)
            {
                Marshal.WriteByte(chroma, i, bytes[i]);
            }

            if (_mediaplayer.Media is Media media)
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

            if (_hdr)
            {
                int[] pitche = { (int)width * 2, (int)width * 2 };
                int[] line = { (int)height, (int)height / 2 };

                Marshal.Copy(pitche, 0, pitches, pitche.Length);
                Marshal.Copy(line, 0, lines, pitche.Length);

                _bufferY = new byte[(int)width * (int)height * 2];
                _bufferUV = new byte[(int)width * (int)height];
            }
            else
            {
                int[] pitche = { (int)width, (int)width };
                int[] line = { (int)height, (int)height / 2 };

                Marshal.Copy(pitche, 0, pitches, pitche.Length);
                Marshal.Copy(line, 0, lines, pitche.Length);

                _bufferY = new byte[(int)width * (int)height];
                _bufferUV = new byte[(int)width * (int)height / 2];
            }

            _sizeY = _bufferY.Length;
            _sizeUV = _bufferUV.Length;

            _planeY = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferY, 0);
            _planeUV = Marshal.UnsafeAddrOfPinnedArrayElement(_bufferUV, 0);

            VideoWidth = (int)width;
            VideoHeight = (int)height;

            InitSharpDX();

            return 1;
        }

        private IntPtr LockVideo(IntPtr opaque, IntPtr planes)
        {
            IntPtr[] datas = { _planeY, _planeUV };
            Marshal.Copy(datas, 0, planes, datas.Length);
            return IntPtr.Zero;
        }

        private void DisplayVideo(IntPtr opaque, IntPtr picture)
        {
            Render();
        }
        #endregion

        #region SharpDX
        private void InitSharpDX()
        {
            if (!isInitTexture)
            {
                Format format = _hdr
                    ? D3DX.MakeFourCC((byte)'P', (byte)'0', (byte)'1', (byte)'0')
                    : D3DX.MakeFourCC((byte)'N', (byte)'V', (byte)'1', (byte)'2');
                _direct3D = new Direct3DEx();
                _displayMode = _direct3D.GetAdapterDisplayModeEx(0);
                PresentParameters presentParameters = new()
                {
                    BackBufferWidth = VideoWidth,
                    BackBufferHeight = VideoHeight,
                    Windowed = true,
                    SwapEffect = SwapEffect.Discard,
                    BackBufferFormat = Format.Unknown
                };
                _device = new DeviceEx(_direct3D, 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.Multithreaded | CreateFlags.HardwareVertexProcessing, presentParameters);
                _texture = new Texture(_device, VideoWidth, VideoHeight, 1, Usage.RenderTarget, _displayMode.Format, Pool.Default);
                _textureSurface = _texture.GetSurfaceLevel(0);
                _surface = Surface.CreateOffscreenPlainEx(_device, VideoWidth, VideoHeight, format, Pool.Default, Usage.None);

                D3DImage.Dispatcher.Invoke(delegate
                {
                    D3DImage.Lock();
                    D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _textureSurface.NativePointer);
                    D3DImage.Unlock();
                    ImageRect = new Int32Rect(0, 0, D3DImage.PixelWidth, D3DImage.PixelHeight);
                });

                isInitTexture = true;
            }
        }

        private void Render()
        {
            DataRectangle dataRectangle = _surface.LockRectangle(LockFlags.DoNotWait);
            IntPtr dataPointer = dataRectangle.DataPointer;

            ClassHelper.RunMemcpy(dataPointer, _planeY, _sizeY);
            dataPointer += _sizeY;
            ClassHelper.RunMemcpy(dataPointer, _planeUV, _sizeUV);

            _surface.UnlockRectangle();

            _device.StretchRectangle(_surface, null, _textureSurface, null, TextureFilter.Linear);
        }
        #endregion

        /// <summary>
        /// 属性通知
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
