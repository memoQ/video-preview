﻿using System;
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
using System.Windows.Shapes;

namespace MemoQ.VideoPreview
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        public LogWindow()
        {
            InitializeComponent();
            Log.Instance.MessageAdded += logMessageAdded;
        }

        private void logMessageAdded(object sender, Log.MessageAddedEventArgs e)
        {
            if (e.LogEntry.Severity <= Settings.Instance.MinimalSeverityToShowInLog)
                tbLog.InvokeIfRequired(x => x.AppendText($"{e.LogEntry.Time} [{e.LogEntry.Severity}] {e.LogEntry.Origin}: {e.LogEntry.Message}{Environment.NewLine}"));
        }

        private void linkClearLog_Click(object sender, RoutedEventArgs e)
        {
            tbLog.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void tbLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            tbLog.ScrollToEnd();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
