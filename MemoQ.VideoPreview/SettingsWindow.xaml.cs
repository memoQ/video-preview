using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;
using static MemoQ.VideoPreview.Log.LogEntry;

namespace MemoQ.VideoPreview
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private LogWindow logWindow = null;

        public SettingsWindow(LogWindow logWindow)
        {
            InitializeComponent();
            this.logWindow = logWindow;
            DataContext = new SettingsViewModel();

            foreach (SeverityOption severity in Enum.GetValues(typeof(SeverityOption)))
            {
                cbMinimalSeverityToShowOnLog.Items.Add(severity.ToString());
            }
        }

        private void linkShowLog_Click(object sender, RoutedEventArgs e)
        {
            logWindow.Show();
        }
        
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var settingsViewModel = DataContext as SettingsViewModel;
            Settings.Instance.TimePaddingForLoop = (int)(settingsViewModel.TimePaddingForLoop * 1000);
            Settings.Instance.LoopNumber = settingsViewModel.LoopNumber;
            Settings.Instance.AlwaysOnTop = settingsViewModel.AlwaysOnTop;
            Settings.Instance.NamedPipeAddress = settingsViewModel.NamedPipeAddress;
            SeverityOption severity = SeverityOption.Warning;
            if (Enum.TryParse(settingsViewModel.MinimalSeverityToShowInLog, out severity))
                Settings.Instance.MinimalSeverityToShowInLog = severity;
            Settings.Instance.SaveSettings();
            Close();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://help.memoq.com/current/en/memoQ-video-preview-tool/mvpt-settings.html");
            e.Handled = true;
        }

        private void linkResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new ResetSettingsWindow();
            window.Topmost = Topmost;
            if (window.ShowDialog() == true)
            {
                Settings.Instance.ResetSettings();
                var settingsViewModel = DataContext as SettingsViewModel;
                settingsViewModel.UpdateProperties();
            }
        }
    }
}
