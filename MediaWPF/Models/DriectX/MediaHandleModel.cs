using LibVLCSharp.Shared;
using MediaWPF.Common;
using MediaWPF.Effects;
using Silk.NET.Direct3D9;
using Silk.NET.Maths;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Effects;

namespace MediaWPF.Models.DriectX
{
    public unsafe class MediaHandleModel : INotifyPropertyChanged
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
        IDirect3D9Ex* _direct3D9;
        IDirect3DDevice9Ex* _device;
        IDirect3DTexture9* _texture;
        IDirect3DSurface9* _textureSurface;
        IDirect3DSurface9* _surface;
        private bool _isInitTexture;
        #endregion
        private readonly string _file;
        private readonly bool _hdr;
        private readonly double _maxLuminance;
        #endregion

        #region 属性
        private FileInfo videoFileInfo;
        private uint videoWidth;
        private uint videoHeight;
        private D3DImage d3DImage;
        private ShaderEffect effect;
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
        public uint VideoWidth
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
        public uint VideoHeight
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
        /// 着色器
        /// </summary>
        public ShaderEffect Effect
        {
            get => effect;
            set
            {
                effect = value;
                OnPropertyChanged(nameof(Effect));
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

        public MediaHandleModel(string file, bool hdr, double maxLuminance)
        {
            _file = file;
            _hdr = hdr;
            _maxLuminance = maxLuminance;
            VideoFileInfo = new FileInfo(_file);
        }

        /// <summary>
        /// 控件初始化
        /// </summary>
        public void Media_Loaded(double dpiScaleX = 1.0, double dpiScaleY = 1.0)
        {
            D3DImage = new D3DImage(dpiScaleX, dpiScaleY);
            if (_hdr)
            {
                Hdr2sdrEffect sdrEffect = new()
                {
                    ToneP1 = Convert.ToSingle(10000.0f / _maxLuminance * (2.0f / 1.4f)),
                    ToneP2 = Convert.ToSingle(_maxLuminance / (100.0f * 1.4f)),
                    Contrast = 0.5f,
                    Brightness = 0.5f
                };
                Effect = sdrEffect;
            }

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
            if (_isInitTexture)
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

            VideoWidth = width;
            VideoHeight = height;

            InitDirect3D9();

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

        #region Direct3D9
        private void InitDirect3D9()
        {
            if (!_isInitTexture)
            {
                Format format = _hdr
                    ? ClassHelper.MakeFourCC((byte)'P', (byte)'0', (byte)'1', (byte)'0')
                    : ClassHelper.MakeFourCC((byte)'N', (byte)'V', (byte)'1', (byte)'2');

                D3D9.GetApi(null).Direct3DCreate9Ex(D3D9.SdkVersion, ref _direct3D9);

                Displaymodeex pMode = new((uint)sizeof(Displaymodeex));
                _direct3D9->GetAdapterDisplayModeEx(D3D9.AdapterDefault, ref pMode, null);

                PresentParameters presentParameters = new()
                {
                    BackBufferWidth = VideoWidth,
                    BackBufferHeight = VideoHeight,
                    Windowed = 1,
                    SwapEffect = Swapeffect.Discard,
                    BackBufferFormat = Format.Unknown
                };
                _direct3D9->CreateDeviceEx(D3D9.AdapterDefault, Devtype.Hal, 0, D3D9.CreateMultithreaded | D3D9.CreateHardwareVertexprocessing, ref presentParameters, (Displaymodeex*)IntPtr.Zero, ref _device);
                _device->CreateTexture(VideoWidth, VideoHeight, 1, D3D9.UsageRendertarget, pMode.Format, Pool.Default, ref _texture, null);
                _texture->GetSurfaceLevel(0, ref _textureSurface);
                _device->CreateOffscreenPlainSurfaceEx(VideoWidth, VideoHeight, format, Pool.Default, ref _surface, null, 0);

                D3DImage.Dispatcher.Invoke(delegate
                {
                    D3DImage.Lock();
                    D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, (IntPtr)_textureSurface);
                    D3DImage.Unlock();
                    ImageRect = new Int32Rect(0, 0, D3DImage.PixelWidth, D3DImage.PixelHeight);
                });

                _isInitTexture = true;
            }
        }

        private void Render()
        {
            LockedRect pLockedRect;
            _surface->LockRect(&pLockedRect, (Box2D<int>*)IntPtr.Zero, D3D9.LockDonotwait);
            IntPtr dataPointer = (IntPtr)pLockedRect.PBits;
            if (pLockedRect.Pitch == VideoWidth)
            {
                ClassHelper.RunMemcpy(dataPointer, _planeY, _sizeY);
                dataPointer += _sizeY;
                ClassHelper.RunMemcpy(dataPointer, _planeUV, _sizeUV);
            }
            else
            {
                var srcPtr = _planeY;
                for (int i = 0; i < videoHeight; i++)
                {
                    ClassHelper.RunMemcpy(dataPointer, srcPtr, (int)VideoWidth);
                    dataPointer += pLockedRect.Pitch;
                    srcPtr += (int)VideoWidth;
                }
                srcPtr = _planeUV;
                for (int i = 0; i < videoHeight >> 1; i++)
                {
                    ClassHelper.RunMemcpy(dataPointer, srcPtr, (int)VideoWidth);
                    dataPointer += pLockedRect.Pitch;
                    srcPtr += (int)VideoWidth;
                }
            }
            _surface->UnlockRect();

            _device->StretchRect(_surface, null, _textureSurface, null, Texturefiltertype.Linear);
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
