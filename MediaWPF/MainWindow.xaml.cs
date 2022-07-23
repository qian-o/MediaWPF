using MediaWPF.Controls;
using MediaWPF.Models;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MediaWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MitOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Video Files|*.3g2;*.3gp;*.3gp2;*.3gpp;*.amrec;*.amv;*.asf;*.avi;*.bik;*.bin;*.crf;*.dav;*.divx;*.drc;*.dv;*.dvr-ms;*.evo;*.f4v;*.flv;*.gvi;*.gxf;*.iso;*.m1v;*.m2v;*.m2t;*.m2ts;*.m4v;*.mkv;*.mov;*.mp2;*.mp2v;*.mp4;*.mp4v;*.mpe;*.mpeg;*.mpeg1;*.mpeg2;*.mpeg4;*.mpg;*.mpv2;*.mts;*.mtv;*.mxf;*.mxg;*.nsv;*.nuv;*.ogg;*.ogm;*.ogv;*.ogx;*.ps;*.rec;*.rm;*.rmvb;*.rpl;*.thp;*.tod;*.tp;*.ts;*.tts;*.txd;*.vob;*.vro;*.webm;*.wm;*.wmv;*.wtv;*.xesc"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                grdLoading.Visibility = Visibility.Visible;
                MediaBaseModel mediaBase = null;
                await Task.Run(delegate
                {
                    mediaBase = MediaBaseModel.GetMediaBase(openFileDialog.FileName);
                });
                txbName.Text = mediaBase.VideoFileInfo.Name;
                MediaOpenGL mediaOpenGL = new(mediaBase);
                brdMedia.Child = mediaOpenGL;
                brdMedia.ContextMenu = null;
                grdLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                Thickness thickness = SystemParameters.WindowResizeBorderThickness;
                grdMain.Margin = new Thickness(thickness.Left + 4, thickness.Top + 4, thickness.Right + 4, thickness.Bottom + 4);
            }
            else
            {
                grdMain.Margin = new Thickness(0);
            }
        }

        private void BtnMin_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnState_Click(object sender, RoutedEventArgs e)
        {
            ResizeMode = WindowState == WindowState.Normal ? ResizeMode.NoResize : ResizeMode.CanResize;
            WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GrdMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DoubleAnimation doubleAnimation = new()
            {
                To = grdControl.Opacity == 1 ? 0 : 1,
                Duration = new TimeSpan(0, 0, 0, 0, 300)
            };
            grdControl.BeginAnimation(OpacityProperty, doubleAnimation);
        }

        private void GrdMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
            else
            {
                if (grdControl.Opacity == 0)
                {
                    DoubleAnimation doubleAnimation = new()
                    {
                        To = 1,
                        Duration = new TimeSpan(0, 0, 0, 0, 300)
                    };
                    grdControl.BeginAnimation(OpacityProperty, doubleAnimation);
                }
            }
        }

        private void GrdMain_MouseLeave(object sender, MouseEventArgs e)
        {
            if (grdControl.Opacity == 1)
            {
                DoubleAnimation doubleAnimation = new()
                {
                    To = 0,
                    Duration = new TimeSpan(0, 0, 0, 0, 300)
                };
                grdControl.BeginAnimation(OpacityProperty, doubleAnimation);
            }
        }
    }
}
