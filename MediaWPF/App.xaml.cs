using LibVLCSharp.Shared;
using System.Windows;

namespace MediaWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Core.Initialize();

            base.OnStartup(e);
        }
    }
}
