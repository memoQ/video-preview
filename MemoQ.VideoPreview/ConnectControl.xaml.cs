using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MemoQ.VideoPreview
{
    /// <summary>
    /// Interaction logic for ConnectControl.xaml
    /// </summary>
    public partial class ConnectControl : UserControl
    {
        private LogWindow logWindow;

        public ConnectControl()
        {
            InitializeComponent();
        }

        public void SetLogWindow(LogWindow logWindow)
        {
            this.logWindow = logWindow;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(logWindow);
            settingsWindow.StateChanged += SettingsWindow_StateChanged;
            settingsWindow.Topmost = logWindow.Topmost;
            settingsWindow.ShowDialog();
        }

        private void SettingsWindow_StateChanged(object sender, EventArgs e)
        {
            var settingsWindow = sender as Window;
            if (settingsWindow != null && settingsWindow.Topmost)
            {
                if (settingsWindow.WindowState == WindowState.Minimized)
                    Application.Current.MainWindow.WindowState = WindowState.Minimized;
                else if (settingsWindow.WindowState == WindowState.Normal)
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
        }
    }
}
