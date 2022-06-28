﻿namespace MediaWPF.Models.ViewModels
{
    public class MdeiaViewModel : BaseModel
    {
        #region FPS计算
        private long timeConsumingData;
        private int timeConsumingCount;
        #endregion

        private string path;
        private string fileName;
        private int videoWidth;
        private int videoHeight;
        private bool isInitTexture;
        private long timeConsuming;
        private int fPS;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string Path
        {
            get => path; set
            {
                path = value;
                OnPropertyChanged(nameof(Path));
            }
        }

        /// <summary>
        /// 文件名称
        /// </summary>
        public string FileName
        {
            get => fileName;
            set
            {
                fileName = value;
                OnPropertyChanged(nameof(FileName));
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
        /// 是否初始化纹理
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

        /// <summary>
        /// 耗时
        /// </summary>
        public long TimeConsuming
        {
            get => timeConsuming;
            set
            {
                timeConsuming = value;
                OnPropertyChanged(nameof(TimeConsuming));
            }
        }

        /// <summary>
        /// 处理帧率
        /// </summary>
        public int FPS
        {
            get => fPS;
            set
            {
                fPS = value;
                OnPropertyChanged(nameof(FPS));
            }
        }

        public override void InitializeVariable()
        {
            Path = string.Empty;
            FileName = string.Empty;
            videoWidth = 0;
            videoHeight = 0;
            IsInitTexture = false;
            TimeConsuming = 0;
            FPS = 0;
        }

        /// <summary>
        /// 耗时统计，计算FPS
        /// </summary>
        /// <param name="timeConsuming">耗时</param>
        public void TimeConsumingStatistics(long timeConsuming)
        {
            TimeConsuming = timeConsuming;
            timeConsumingData += timeConsuming;
            timeConsumingCount++;
            if (timeConsumingData > 1000)
            {
                FPS = timeConsumingCount;
                timeConsumingData = 0;
                timeConsumingCount = 0;
            }
        }
    }
}