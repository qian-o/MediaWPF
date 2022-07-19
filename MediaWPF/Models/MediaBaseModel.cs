using LibVLCSharp.Shared;
using MediaWPF.Shaders;
using OpenTK.Graphics.OpenGL4;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MediaWPF.Models
{
    public abstract class MediaBaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region 变量
        #region Shader
        protected readonly float[] _vertices =
        {
            -1.0f, -1.0f, 0.0f, 0.0f, 1.0f,
             1.0f, -1.0f, 0.0f, 1.0f, 1.0f,
            -1.0f,  1.0f, 0.0f, 0.0f, 0.0f,
             1.0f,  1.0f, 0.0f, 1.0f, 0.0f
        };
        protected int _vertexBufferObject;
        protected int _vertexArrayObject;
        protected Shader _shader;
        #endregion
        #region VLC
        protected LibVLC _lib;
        protected Media _media;
        protected MediaPlayer _mediaplayer;
        protected int sizeY, sizeU, sizeV;
        protected IntPtr planeY, planeU, planeV;
        protected int lengthY, lengthU, lengthV;
        #endregion
        #region OpenGL
        protected int id_y, id_u, id_v;
        protected int buffer_y, buffer_u, buffer_v;
        protected int textureUniformY, textureUniformU, textureUniformV;
        #endregion
        private readonly string _file;
        private readonly bool _hdr;
        #endregion

        #region 属性
        private int videoWidth;
        private int videoHeight;
        private bool isInitTexture;

        /// <summary>
        /// 视频文件信息
        /// </summary>
        public FileInfo VideoFileInfo { get; set; }

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
        /// 纹理已初始化
        /// </summary>
        public bool IsInitTexture
        {
            get => isInitTexture;
            set
            {
                isInitTexture = value;
                OnPropertyChanged(nameof(IsInitTexture));
            }
        }

        #endregion

        public MediaBaseModel(string file, bool hdr)
        {
            _file = file;
            _hdr = hdr;
        }

        /// <summary>
        /// 控件初始化
        /// </summary>
        public void Media_Loaded()
        {
            if (File.Exists(_file))
            {
                VideoFileInfo = new FileInfo(_file);
                _lib = new();
                _media = new(_lib, new Uri(VideoFileInfo.FullName), new string[] { "input-repeat=65535" });
                _mediaplayer = new(_media)
                {
                    EnableHardwareDecoding = true
                };
                _mediaplayer.SetVideoFormatCallbacks(VideoFormat, null);
                _mediaplayer.SetVideoCallbacks(LockVideo, null, DisplayVideo);
                _mediaplayer.Mute = true;
                _mediaplayer.Play();
            }
        }

        /// <summary>
        /// OpenGL初始化
        /// </summary>
        public void OpenGL_Loaded()
        {
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StreamDraw);

            string path = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Shaders");
            _shader = new Shader(Path.Combine(path, "shader.vert"), Path.Combine(path, $"shader{(_hdr ? "HDR" : "SDR")}.frag"));

            textureUniformY = GL.GetUniformLocation(_shader.Handle, "tex_y");
            textureUniformU = GL.GetUniformLocation(_shader.Handle, "tex_u");
            textureUniformV = GL.GetUniformLocation(_shader.Handle, "tex_v");

            int vertexLocation = _shader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            int texCoordLocation = _shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        }

        /// <summary>
        /// 渲染
        /// </summary>
        public void OpenGL_Render()
        {
            GL.UseProgram(_shader.Handle);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(_vertexArrayObject);
            Display();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.UseProgram(0);
        }

        #region VLC解码
        public virtual uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, IntPtr pitches, IntPtr lines)
        {
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

            VideoWidth = (int)width;
            VideoHeight = (int)height;

            return 1;
        }

        public virtual IntPtr LockVideo(IntPtr opaque, IntPtr planes)
        {
            IntPtr[] datas = { planeY, planeU, planeV };
            Marshal.Copy(datas, 0, planes, datas.Length);
            return IntPtr.Zero;
        }

        public virtual void DisplayVideo(IntPtr opaque, IntPtr picture)
        {

        }
        #endregion

        /// <summary>
        /// 渲染具体操作（HDR、SDR视频操作不同）
        /// </summary>
        public abstract void Display();

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
