using LibVLCSharp.Shared;
using MediaWPF.Shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;


namespace MediaWPF
{
    /// <summary>
    /// MediaShaderHDR.xaml 的交互逻辑
    /// </summary>
    public partial class MediaShaderHDR : UserControl
    {
        #region shader
        private readonly float[] _vertices =
        {
            -1.0f, -1.0f, 0.0f, 0.0f, 1.0f,
             1.0f, -1.0f, 0.0f, 1.0f, 1.0f,
            -1.0f,  1.0f, 0.0f, 0.0f, 0.0f,
             1.0f,  1.0f, 0.0f, 1.0f, 0.0f
        };

        private int _vertexBufferObject;

        private int _vertexArrayObject;

        private Shader _shader;
        #endregion

        private readonly string _path = @"E:\BaiduNetdiskDownload\[A]ddiction _2160p_HDR_Extreme.mp4";
        private Uri _uri;
        private LibVLC _lib;
        private Media _media;
        private MediaPlayer _mediaplayer;
        private int videoWidth;
        private int videoHeight;
        private byte[] _buffer;
        private int indexY;
        private int sizeY;
        private IntPtr planeY;
        private int id_y;
        private int buffer_y;
        private int textureUniformY;
        private bool isInitTexture;

        public MediaShaderHDR()
        {
            InitializeComponent();

            GLWpfControlSettings gLWpfControlSettings = new();
            glMedia.Start(gLWpfControlSettings);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (File.Exists(_path))
                {
                    DependencyObject dependencyObject = LogicalTreeHelper.GetParent(this);
                    while (dependencyObject != null)
                    {
                        if (dependencyObject is MainWindow mainWindow)
                        {
                            mainWindow.Title = new FileInfo(_path).Name;
                            break;
                        }
                        else
                        {
                            dependencyObject = LogicalTreeHelper.GetParent(dependencyObject);
                        }
                    }
                    _uri = new(_path);
                    _lib = new();
                    _media = new(_lib, _uri, new string[] { "input-repeat=65535" });
                    _mediaplayer = new(_media)
                    {
                        EnableHardwareDecoding = true
                    };
                    _mediaplayer.SetVideoFormatCallbacks(VideoFormat, null);
                    _mediaplayer.SetVideoCallbacks(LockVideo, null, null);
                    _mediaplayer.Mute = true;
                    _mediaplayer.Play();
                }
            }
        }

        private void GlMedia_Loaded(object sender, RoutedEventArgs e)
        {
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StreamDraw);

            string p = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Shaders");
            _shader = new Shader(Path.Combine(p, "shader.vert"), Path.Combine(p, "shaderHDR.frag"));
            _shader.Use();

            textureUniformY = GL.GetUniformLocation(_shader.Handle, "tex_y");

            int vertexLocation = _shader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            int texCoordLocation = _shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        }

        private void GlMedia_Render(TimeSpan obj)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(_vertexArrayObject);
            Display();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        #region VLC解码
        private uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, IntPtr pitches, IntPtr lines)
        {
            Debug.WriteLine(Marshal.PtrToStringAnsi(chroma));
            byte[] bytes = Encoding.ASCII.GetBytes("I0AL");
            for (int i = 0; i < bytes.Length; i++)
            {
                Marshal.WriteByte(chroma, i, bytes[i]);
            }

            width *= 2;
            height *= 2;
            int[] pitche = { (int)width };
            int[] line = { (int)height / 2 };
            Marshal.Copy(pitche, 0, pitches, pitche.Length);
            Marshal.Copy(line, 0, lines, pitche.Length);

            _buffer = new byte[width * height / 2];

            sizeY = _buffer.Length;
            indexY = 0;

            opaque = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0);

            planeY = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, indexY);

            if (_mediaplayer.Media is Media media)
            {
                foreach (MediaTrack track in media.Tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        Debug.WriteLine(Encoding.ASCII.GetString(BitConverter.GetBytes(track.OriginalFourcc)));
                        Debug.WriteLine(Encoding.ASCII.GetString(BitConverter.GetBytes(track.Codec)));
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

            // GLWpfControl控件外层嵌套Viewbox进行比例缩放，防止视频比例变形。
            // 但会影响渲染性能。
            Dispatcher.Invoke(delegate
            {
                glMedia.Width = videoWidth;
                glMedia.Height = videoHeight;
            });

            return 1;
        }

        private IntPtr LockVideo(IntPtr opaque, IntPtr planes)
        {
            IntPtr[] datas = { planeY };
            Marshal.Copy(datas, 0, planes, datas.Length);
            return IntPtr.Zero;
        }
        #endregion

        private void Display()
        {
            if (!isInitTexture && _buffer != null)
            {
                //Init Texture
                id_y = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id_y);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb10, videoWidth, videoHeight, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                buffer_y = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, buffer_y);
                GL.BufferData(BufferTarget.ArrayBuffer, sizeY, IntPtr.Zero, BufferUsageHint.StreamCopy);

                isInitTexture = true;
            }
            if (isInitTexture)
            {
                // Y
                GL.BindBuffer(BufferTarget.PixelPackBuffer, id_y);
                GL.BufferSubData(BufferTarget.PixelPackBuffer, IntPtr.Zero, sizeY, planeY);
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, id_y);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, buffer_y);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, videoWidth, videoHeight, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                GL.Uniform1(textureUniformY, 0);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }
        }
    }
}
