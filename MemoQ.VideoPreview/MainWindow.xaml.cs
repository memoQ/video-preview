using MemoQ.PreviewInterfaces;
using MemoQ.PreviewInterfaces.Entities;
using MemoQ.PreviewInterfaces.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vlc.DotNet.Core;
using Vlc.DotNet.Wpf;
using static MemoQ.VideoPreview.Log.LogEntry;
using static MemoQ.VideoPreview.VideoViewModel;

namespace MemoQ.VideoPreview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IPreviewToolCallback
    {
        private static readonly Brush brushUnderLimit = new SolidColorBrush(Color.FromArgb(255, 59, 55, 81));
        private static readonly Brush brushAboveLimit = new SolidColorBrush(Color.FromArgb(255, 239, 63, 63));

        /// <summary>
        /// Supported video file format extensions
        /// </summary>
        public static readonly string[] supportedExtensions = { ".3g2",".3gp",".3gp2",".3gpp",".amv",".asf",".avi",".bik",".bin",".divx",".drc",".dv",
            ".evo",".f4v",".flv",".gvi",".gxf",".iso",".m1v",".m2v",".m2t",".m2ts",".m4v",".mkv",".mov",".mp2",".mp2v",".mp4",".mp4v",
            ".mpe",".mpeg",".mpeg1", ".mpeg2",".mpeg4",".mpg",".mpv2",".mts",".mtv",".mxf",".mxg",".nsv",".nuv",".ogg",".ogm",".ogv",".ogx",
            ".ps",".rec",".rm",".rmvb",".rpl",".thp",".tod",".ts",".tts",".txd",".vob",".vro",".webm",".wm",".wmv",".wtv",".xesc" };

        /// <summary>
        /// A multiplier that converts volume percent value to VLC volume value. Let's say 300 is the max.
        /// </summary>
        private const double volumeMultiplier = 3;

        /// <summary>
        /// Offset values to avoid the player to reach the end of the video (as it would stop automatically)
        /// </summary>
        private const int startOffsetInMs = 500;
        private const int endOffsetInMs = 500;

        private readonly LogWindow logWindow = new LogWindow();

        /// <summary>
        /// The view model
        /// </summary>
        private MainViewModel mainViewModel;
        
        /// <summary>
        /// The cached segment parts to show in the player
        /// </summary>
        private readonly SortedList<Tuple<Guid, long, long>, SegmentPart> segmentParts = new SortedList<Tuple<Guid, long, long>, SegmentPart>();

        /// <summary>
        /// The actual segment part that is shown in the player (and looped around in looping mode)
        /// </summary>
        private List<SegmentPart> segmentPartsToLoop = new List<SegmentPart>();

        /// <summary>
        /// The folder path of the current temporary subtitle file
        /// </summary>
        private string subtitleTempFile;

        /// <summary>
        /// The current looping number (between 0 and Settings.LoopNumber)
        /// </summary>
        private int currentLoopNumber = 1;

        /// <summary>
        /// Whether the loop should be paused
        /// </summary>
        private bool loopShouldBePaused = false;

        /// <summary>
        /// Helper variable to differentiate video time change triggered by the player and the code
        /// </summary>
        private bool isVideoTimeChangedByHand;

        /// <summary>
        /// Helper variable to differentiate slider value change triggered by the player and the code
        /// </summary>
        private bool isVideoSliderChangedByHand;

        private bool startTimeOutOfVideoBounds = false;

        public MainWindow()
        {
            mainViewModel = new MainViewModel(this as IPreviewToolCallback, stopVideo);
            DataContext = mainViewModel;

            try
            {
                InitializeComponent();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Cannot find Vlc libraries in the specified location.{Environment.NewLine}{Environment.NewLine}Press \"OK\" to quit.",
                    "Cannot find Vlc libraries", MessageBoxButton.OK, MessageBoxImage.Error);
                Settings.Instance.VlcLibPath = "";
                Settings.Instance.SaveSettings();
                Environment.Exit(ExitCodes.VlcLibrariesNotFound);
            }

            Top = Settings.Instance.WindowTop;
            Left = Settings.Instance.WindowLeft;
            Width = Settings.Instance.WindowWidth;
            Height = Settings.Instance.WindowHeight;
            if (Settings.Instance.WindowMaximized)
                WindowState = WindowState.Maximized;
            Topmost = Settings.Instance.AlwaysOnTop;
            logWindow.Topmost = Settings.Instance.AlwaysOnTop;
            BorderBrush = SystemParameters.WindowGlassBrush;

            openControl.SetPlayAction((doc) => playBrowsedVideo(doc));
            connectControl.SetLogWindow(logWindow);
            Log.Instance.MessageAdded += Log_MessageAdded;
            sliderVolume.Value = Settings.Instance.VolumeValue;
            mainViewModel.VideoViewModel.IsMute = Settings.Instance.VolumeMute;

            subtitleTempFile = Path.GetTempFileName();
            myVlcControl.MediaPlayer.Playing += myVlcControl_Playing;
            myVlcControl.MediaPlayer.TimeChanged += myVlcControl_TimeChanged;
            myVlcControl.MediaPlayer.VlcLibDirectoryNeeded += myVlcControl_VlcLibDirectoryNeeded;
            myVlcControl.MediaPlayer.EncounteredError += myVlcControl_EncounteredError;
        }

        private static void ensureArialMSFontInstalledOrIgnored()
        {
            // check if Arial Unicode MS is installed or not
            string fontName = "Arial Unicode MS";
            bool isInstalled = false;

            var fontsCollection = new InstalledFontCollection();
            foreach (var fontFamily in fontsCollection.Families)
            {
                if (fontFamily.Name == fontName)
                {
                    isInstalled = true;
                }
            }

            // check if the client already clicked to "do not ask again" button
            bool dontAskAgainIsTrue = Settings.Instance.FontMissingWindowDoNotAskAgain;

            // show missing font form 
            if (!isInstalled && !dontAskAgainIsTrue)
            {
                MissingFontDialogViewModel vm = new MissingFontDialogViewModel();
                MissingFontDialogView iqf = new MissingFontDialogView(vm);
                iqf.Owner = Application.Current.MainWindow;
                iqf.ShowDialog();

                if (vm.InstallFont)
                {
                    string path = Path.GetTempPath();
                    string fileName = Path.Combine(path, "arial-unicode-ms.ttf");
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MemoQ.VideoPreview.Fonts.arial-unicode-ms.ttf"))
                    using (Stream writer = new FileStream(fileName, FileMode.Create))
                        stream.CopyTo(writer);

                    Process.Start(fileName).WaitForExit();
                    File.Delete(fileName);
                }

                if (vm.IgnorePermamently)
                {
                    Settings.Instance.FontMissingWindowDoNotAskAgain = true;
                    Settings.Instance.SaveSettings();
                }
            }
        }

        #region Window events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ensureArialMSFontInstalledOrIgnored();

            if (Settings.Instance.AutoConnect)
                mainViewModel.Connect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Instance.WindowTop = Top;
            Settings.Instance.WindowLeft = Left;
            Settings.Instance.WindowWidth = Width;
            Settings.Instance.WindowHeight = Height;
            Settings.Instance.WindowMaximized = WindowState == WindowState.Maximized;
            Settings.Instance.SaveSettings();
            
            logWindow.Close();
            mainViewModel.Disconnect();

            // Need to unsubscribe from VlcControl event handlers to avoid deadlock when stopping the playback.
            myVlcControl.MediaPlayer.Playing -= myVlcControl_Playing;
            myVlcControl.MediaPlayer.TimeChanged -= myVlcControl_TimeChanged;
            myVlcControl.MediaPlayer.VlcLibDirectoryNeeded -= myVlcControl_VlcLibDirectoryNeeded;
            myVlcControl.MediaPlayer.Stop();
            myVlcControl.Dispose();
            myVlcControl = null;
            try
            {
                if (subtitleTempFile != null)
                    File.Delete(subtitleTempFile);
            }
            catch
            {
                // subtitle temp file could not been deleted, not a huge problem
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var heightToCalculateWith = Math.Min(Math.Max(400, Height), 600);
            var margin = (heightToCalculateWith - 400) * 0.5;
            connectControl.Margin = new Thickness(0, margin, 0, 0);
            openControl.Margin = new Thickness(0, margin, 0, 0);
        }

        private void Log_MessageAdded(object sender, Log.MessageAddedEventArgs e)
        {
            if (e.LogEntry.Severity <= Settings.Instance.MinimalSeverityToShowInLog)
                this.InvokeIfRequired(_ => logWindow.Show());
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        #endregion Window events

        #region Named pipe callbacks

        public void HandleContentUpdateRequest(ContentUpdateRequestFromMQ contentUpdateRequest)
        {
            if (!contentUpdateRequest.PreviewParts.Any())
                return;
            Log.Instance.WriteMessage($"Content updated in memoQ.", SeverityOption.Info);

            this.InvokeIfRequired(_ =>
            {
                bool isFirstPreviewPart = true;
                bool docWasEmpty = mainViewModel.VideoViewModel.Document.Id == Guid.Empty;
                var newDocuments = new List<Document>();
                foreach (var previewPart in contentUpdateRequest.PreviewParts)
                {
                    var previewIdParts = previewPart.PreviewPartId.Split('|');
                    long start = 0;
                    long end = 0;
                    if (!string.IsNullOrEmpty(previewIdParts[1]))
                        start = TimeSpan.Parse(previewIdParts[1], CultureInfo.InvariantCulture).Ticks / TimeSpan.TicksPerMillisecond;
                    else
                        Log.Instance.WriteMessage("Start time was given probably not in the right format (hh:mm:ss.fff).", SeverityOption.Warning);
                    if (!string.IsNullOrEmpty(previewIdParts[1]))
                        end = TimeSpan.Parse(previewIdParts[2], CultureInfo.InvariantCulture).Ticks / TimeSpan.TicksPerMillisecond;
                    else
                        Log.Instance.WriteMessage("End time was given probably not in the right format (hh:mm:ss.fff).", SeverityOption.Warning);

                    var docGuid = previewPart.SourceDocument.DocumentGuid;

                    var sourceContent = previewPart.SourceContent.Content;
                    var targetContent = previewPart.TargetContent.Content;
                    var content = string.IsNullOrEmpty(targetContent) ? sourceContent : targetContent;
                    var segmentPart = new SegmentPart(previewPart.PreviewPartId, previewPart.PreviewProperties,
                        previewPart.SourceLangCode, previewPart.TargetLangCode, sourceContent, targetContent, new Subtitle(start, end, content));

                    var segmentKey = Tuple.Create(docGuid, start, end);
                    segmentParts[segmentKey] = segmentPart;

                    var sourceDocument = previewPart.SourceDocument;
                    var document = addMediaToDocumentIfNeeded(sourceDocument.DocumentGuid, sourceDocument.DocumentName, sourceDocument.ImportPath);

                    if (isFirstPreviewPart)
                    {
                        if (!segmentPartsToLoop.Any())
                            segmentPartsToLoop.Add(segmentPart);
                        if (mainViewModel.VideoViewModel.Document.Id == Guid.Empty)
                            mainViewModel.VideoViewModel.Document = document;
                        isFirstPreviewPart = false;
                    }

                    if (!mainViewModel.VideoViewModel.DocumentsWithSameMedia.Any(d => d.Id == document.Id))
                    {
                        // collect unique documents that have the same media (e.g. a view could contain multiple documents)
                        if (!newDocuments.Any() || (!newDocuments.Any(d => d.Id == document.Id) && newDocuments.Any(d => d.Media == document.Media)))
                            newDocuments.Add(document);
                    }
                }

                if (newDocuments.Any())
                    mainViewModel.VideoViewModel.DocumentsWithSameMedia = newDocuments;

                if (Settings.Instance.PlayMode == PlayMode.All)
                {
                    // open video file if it wasn't
                    if (docWasEmpty)
                    {
                        var firstDoc = contentUpdateRequest.PreviewParts.First().SourceDocument;
                        var firstDocument = addMediaToDocumentIfNeeded(firstDoc.DocumentGuid, firstDoc.DocumentName, firstDoc.ImportPath);
                        mainViewModel.VideoViewModel.Document = firstDocument;
                        reinitVideoPlayer();
                        startVideo();
                    }
                }
                else
                {
                    reinitVideoPlayer();
                    loopVideo(segmentPartsToLoop.First().Subtitle.Start - Settings.Instance.TimePaddingForLoop,
                        segmentPartsToLoop.Last().Subtitle.End + Settings.Instance.TimePaddingForLoop);
                }
            });
        }

        public void HandleChangeHighlightRequest(ChangeHighlightRequestFromMQ changeHighlightRequest)
        {
            if (!changeHighlightRequest.ActivePreviewParts.Any())
                return;
            Log.Instance.WriteMessage($"Highlight changed in memoQ.", SeverityOption.Info);

            this.InvokeIfRequired(_ =>
            {
                SourceDocument sourceDocument = null;
                if (Settings.Instance.PlayMode == PlayMode.All)
                {
                    handlePlayAllMode(changeHighlightRequest.ActivePreviewParts.First().SourceDocument);
                    return;
                }

                long minStart = 0;
                long maxEnd = 0;
                bool isFirstPreviewPart = true;
                foreach (var activePreviewPart in changeHighlightRequest.ActivePreviewParts)
                {
                    var previewIdParts = activePreviewPart.PreviewPartId.Split('|');
                    long start = 0;
                    long end = 0;
                    if (!string.IsNullOrEmpty(previewIdParts[1]))
                        start = TimeSpan.Parse(previewIdParts[1], CultureInfo.InvariantCulture).Ticks / TimeSpan.TicksPerMillisecond;
                    else
                        Log.Instance.WriteMessage("Start time was given probably not in the right format (hh:mm:ss.fff).", SeverityOption.Warning);
                    if (!string.IsNullOrEmpty(previewIdParts[1]))
                        end = TimeSpan.Parse(previewIdParts[2], CultureInfo.InvariantCulture).Ticks / TimeSpan.TicksPerMillisecond;
                    else
                        Log.Instance.WriteMessage("End time was given probably not in the right format (hh:mm:ss.fff).", SeverityOption.Warning);

                    if (isFirstPreviewPart)
                    {
                        minStart = start;
                        maxEnd = end;
                        sourceDocument = activePreviewPart.SourceDocument;
                        isFirstPreviewPart = false;
                    }

                    // if other document found, skip those parts, we cannot open multiple videos
                    if (!isFirstPreviewPart && sourceDocument.DocumentGuid != activePreviewPart.SourceDocument.DocumentGuid)
                        continue;

                    minStart = Math.Min(minStart, start);
                    maxEnd = Math.Max(maxEnd, end);
                }

                var document = addMediaToDocumentIfNeeded(sourceDocument.DocumentGuid, sourceDocument.DocumentName, sourceDocument.ImportPath);

                if (mainViewModel.VideoViewModel.Document.Id != sourceDocument.DocumentGuid)
                {
                    mainViewModel.VideoViewModel.Document = document;

                    // also update the document list if the highlight reached a new document
                    if (!mainViewModel.VideoViewModel.DocumentsWithSameMedia.Any(d => d.Id == document.Id))
                        mainViewModel.VideoViewModel.DocumentsWithSameMedia = new List<Document>() { document };
                    reinitVideoPlayer();
                }

                loopVideo(minStart - Settings.Instance.TimePaddingForLoop, maxEnd + Settings.Instance.TimePaddingForLoop);
            });
        }

        public void HandlePreviewPartIdUpdateRequest(PreviewPartIdUpdateRequestFromMQ previewPartIdUpdateRequest)
        {
            if (!previewPartIdUpdateRequest.PreviewPartIds.Any())
                return;
            Log.Instance.WriteMessage($"Some preview part ids updated in memoQ.", SeverityOption.Info);

            mainViewModel.RequestContentUpdate(previewPartIdUpdateRequest.PreviewPartIds);
        }

        public void HandleDisconnect()
        {
            Log.Instance.WriteMessage($"Disconnect request from memoQ.", SeverityOption.Info);
            this.InvokeIfRequired(_ => mainViewModel.ConnectViewModel.IsConnected = false);
        }

        #endregion Named pipe callbacks

        #region VlcControl events

        private void myVlcControl_Playing(object sender, VlcMediaPlayerPlayingEventArgs e)
        {
            this.BeginInvokeIfRequired(_ =>
            {
                if (myVlcControl == null)
                    return;

                if (myVlcControl.MediaPlayer.Length == 0)
                {
                    mainViewModel.VideoViewModel.CannotOpen = true;
                    return;
                }
                loopShouldBePaused = false;
                sliderVideo.Maximum = myVlcControl.MediaPlayer.Length;
                mainViewModel.VideoViewModel.IsLoading = false;
            });
        }

        private void myVlcControl_TimeChanged(object sender, VlcMediaPlayerTimeChangedEventArgs e)
        {
            if (isVideoSliderChangedByHand || mainViewModel.VideoViewModel.IsLoading)
                return;

            this.BeginInvokeIfRequired(_ =>
            {
                if (myVlcControl == null)
                    return;

                var videoTime = myVlcControl.MediaPlayer.Time;
                var segmentPart = getCurrentSegmentPart(videoTime);
                updateLabels(segmentPart, videoTime);

                if (loopShouldBePaused)
                {
                    // the subtitle and other labels have been updated, the loop can be paused
                    myVlcControl.MediaPlayer.Pause();
                    loopShouldBePaused = false;
                    return;
                }

                if (isVideoTimeChangedByHand)
                    return;

                // handle the looping
                long loopStart = (Settings.Instance.PlayMode == PlayMode.Selection && segmentPartsToLoop.Any()) ?
                    ((segmentPartsToLoop.First().Subtitle.Start - Settings.Instance.TimePaddingForLoop < myVlcControl.MediaPlayer.Length - startOffsetInMs) ?
                    (Math.Max(0, segmentPartsToLoop.First().Subtitle.Start - Settings.Instance.TimePaddingForLoop)) : 0) : 0;
                long loopEnd = (Settings.Instance.PlayMode == PlayMode.Selection && segmentPartsToLoop.Any()) ?
                    Math.Min(segmentPartsToLoop.Last().Subtitle.End + Settings.Instance.TimePaddingForLoop, myVlcControl.MediaPlayer.Length - endOffsetInMs) :
                    myVlcControl.MediaPlayer.Length - endOffsetInMs;

                if (e.NewTime >= loopEnd && !startTimeOutOfVideoBounds)
                {
                    if (Settings.Instance.LoopSelection && (Settings.Instance.LoopNumber == 0 || currentLoopNumber < Settings.Instance.LoopNumber))
                    {
                        // looping continues
                        myVlcControl.MediaPlayer.Time = loopStart;
                        if (Settings.Instance.LoopNumber == 0)
                            currentLoopNumber = 0;
                        else
                            currentLoopNumber++;
                    }
                    else
                    {
                        // end of looping
                        mainViewModel.VideoViewModel.IsPlaying = false;
                        myVlcControl.MediaPlayer.Time = loopStart;
                        loopShouldBePaused = true;
                        currentLoopNumber = 0;
                    }
                    videoTime = loopStart;
                }

                isVideoSliderChangedByHand = true;
                sliderVideo.Value = videoTime;
                isVideoSliderChangedByHand = false;
            });
        }

        private void myVlcControl_VlcLibDirectoryNeeded(object sender, Vlc.DotNet.Forms.VlcLibDirectoryNeededEventArgs e)
        {
            var isVlcLibPathSet = !string.IsNullOrWhiteSpace(Settings.Instance.VlcLibPath);
            if (isVlcLibPathSet)
                e.VlcLibDirectory = new DirectoryInfo(Settings.Instance.VlcLibPath);
            if (!isVlcLibPathSet || !e.VlcLibDirectory.Exists)
            {
                var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
                folderBrowserDialog.Description = "Select Vlc libraries folder.";
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
                folderBrowserDialog.ShowNewFolderButton = true;
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    e.VlcLibDirectory = new DirectoryInfo(folderBrowserDialog.SelectedPath);
                    Settings.Instance.VlcLibPath = folderBrowserDialog.SelectedPath;
                    Settings.Instance.SaveSettings();
                }
                else
                {
                    MessageBox.Show($"memoQ video preview cannot work without Vlc libraries.{Environment.NewLine}{Environment.NewLine}Press \"OK\" to quit.",
                        "Vlc libraries needed", MessageBoxButton.OK, MessageBoxImage.Error);

                    Environment.Exit(ExitCodes.VlcLibrariesRequired);
                }
            }
        }

        private void myVlcControl_EncounteredError(object sender, VlcMediaPlayerEncounteredErrorEventArgs e)
        {
            mainViewModel.VideoViewModel.CannotOpen = true;
        }

        #endregion VlcControl events

        #region Control events

        private void sliderVideo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isVideoSliderChangedByHand || mainViewModel.VideoViewModel.IsLoading)
                return;

            isVideoTimeChangedByHand = true;
            currentLoopNumber = 0;
            long previousSubtitleEnd = 0;
            var videoTime = sliderVideo.Value * myVlcControl.MediaPlayer.Length / sliderVideo.Maximum;
            bool foundSegmentPart = false;
            var currentSegmentParts = segmentParts.Where(x => mainViewModel.VideoViewModel.DocumentsWithSameMedia.Any(d => d.Id == x.Key.Item1))
                                                  .Select(x => x.Value).OrderBy(s => s.Subtitle.Start);
            foreach (var segmentPart in currentSegmentParts)
            {
                if (videoTime >= previousSubtitleEnd && videoTime <= segmentPart.Subtitle.End)
                {
                    if (Settings.Instance.PlayMode == PlayMode.Selection && segmentPartsToLoop.FirstOrDefault() != segmentPart)
                        mainViewModel.RequestHighlightChange(segmentPart);

                    segmentPartsToLoop.Clear();
                    segmentPartsToLoop.Add(segmentPart);
                    foundSegmentPart = true;
                    break;
                }
                previousSubtitleEnd = segmentPart.Subtitle.End;
            }

            if (!foundSegmentPart)
                segmentPartsToLoop.Clear();

            myVlcControl.MediaPlayer.Time = (int)sliderVideo.Value;
            isVideoTimeChangedByHand = false;
        }

        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int sliderVolumeValue = (int)sliderVolume.Value;
            var vlcAudioVolume = (int)(sliderVolumeValue * volumeMultiplier);
            if (Settings.Instance.VolumeMute && sliderVolumeValue > 0)
            {
                if (myVlcControl.MediaPlayer.Audio != null)
                    myVlcControl.MediaPlayer.Audio.IsMute = false;
                mainViewModel.VideoViewModel.IsMute = false;
            }
            if (myVlcControl.MediaPlayer.Audio != null)
                myVlcControl.MediaPlayer.Audio.Volume = vlcAudioVolume;

            if (sliderVolumeValue != Settings.Instance.VolumeValue)
            {
                Settings.Instance.VolumeValue = sliderVolumeValue;
                Settings.Instance.SaveSettings();
            }
        }

        private void btnVolume_Click(object sender, RoutedEventArgs e)
        {
            var isMute = btnVolume.IsChecked ?? false;
            if (myVlcControl.MediaPlayer.Audio != null)
                myVlcControl.MediaPlayer.Audio.IsMute = isMute;
            mainViewModel.VideoViewModel.IsMute = isMute;
            Settings.Instance.VolumeMute = isMute;
            Settings.Instance.SaveSettings();
        }

        private void btnSelection_Click(object sender, RoutedEventArgs e)
        {
            // if user changed back to selection mode, request content update from memoQ
            if (mainViewModel.VideoViewModel.IsSelectionMode)
            {
                var currentSegmentParts = segmentParts.Where(x => mainViewModel.VideoViewModel.DocumentsWithSameMedia.Any(d => d.Id == x.Key.Item1))
                                                      .Select(x => x.Value.PreviewPartId);
                mainViewModel.RequestContentUpdate(currentSegmentParts.ToArray());
            }
        }

        private void btnPlayOrPause_Click(object sender, RoutedEventArgs e)
        {
            if (mainViewModel.VideoViewModel.IsPlaying)
            {
                currentLoopNumber = Math.Max(currentLoopNumber, 1);
                playVideo();
            }
            else
                myVlcControl.MediaPlayer.Pause();
        }

        private void btnFullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;

            Settings.Instance.WindowMaximized = WindowState == WindowState.Maximized;
            Settings.Instance.SaveSettings();
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                var window = new AddMediaToDocumentWindow(mainViewModel.VideoViewModel.Document.Id,
                    mainViewModel.VideoViewModel.Document.Name, mainViewModel.VideoViewModel.Document.Media);
                window.Topmost = Topmost;
                if (window.ShowDialog() != true)
                    return;

                playBrowsedVideo(window.Document);
            });
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(logWindow);
            settingsWindow.StateChanged += SettingsWindow_StateChanged;
            settingsWindow.Topmost = Topmost;
            settingsWindow.ShowDialog();
            mainViewModel.VideoViewModel.LoopNumber = Settings.Instance.LoopNumber;
            Topmost = Settings.Instance.AlwaysOnTop;
            logWindow.Topmost = Topmost;
        }

        private void SettingsWindow_StateChanged(object sender, EventArgs e)
        {
            var settingsWindow = sender as Window;
            if (settingsWindow != null && settingsWindow.Topmost)
            {
                if (settingsWindow.WindowState == WindowState.Minimized)
                    this.WindowState = WindowState.Minimized;
                else if (settingsWindow.WindowState == WindowState.Normal)
                    this.WindowState = WindowState.Normal;
            }
        }

        #endregion Control events

        #region Private methods

        private Document addMediaToDocumentIfNeeded(Guid id, string name, string path)
        {
            if (Settings.Instance.DocumentByDocumentGuid.ContainsKey(id))
                return Settings.Instance.DocumentByDocumentGuid[id];

            var videoFiles = new List<string>();
            try
            {
                // document is opened for the first time, try to find video file with the same name
                videoFiles = Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".*")
                                      .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()) && f != path)
                                      .OrderBy(f => f).ToList();
            }
            catch
            {
                // no video file found, e.g. the import path of the document was not set
            }

            if (videoFiles.Any())
            {
                // if any video file found, connect it with the document
                var document = new Document(id, name, videoFiles.First());
                Settings.Instance.DocumentByDocumentGuid[id] = document;
                Settings.Instance.SaveSettings();
                return document;
            }
            else
            {
                // otherwise offer to choose media
                var window = new AddMediaToDocumentWindow(id, name, "");
                window.Topmost = Topmost;
                window.ShowDialog();
                return window.Document;
            }
        }

        private void reinitVideoPlayer()
        {
            mainViewModel.VideoViewModel.IsLoading = true;
            var currentSubtitles = segmentParts.Where(x => mainViewModel.VideoViewModel.DocumentsWithSameMedia.Any(d => d.Id == x.Key.Item1))
                                               .Select(x => x.Value.Subtitle).OrderBy(s => s.Start);
            SrtFileCreator.Create(currentSubtitles, subtitleTempFile);

            myVlcControl.MediaPlayer.Stop();
            myVlcControl.MediaPlayer.VlcMediaplayerOptions = new string[] { $"--sub-file={subtitleTempFile}", "--freetype-font=Arial Unicode MS" };
            myVlcControl.MediaPlayer.EndInit();
        }

        private void handlePlayAllMode(SourceDocument sourceDocument)
        {
            var firstDocument = addMediaToDocumentIfNeeded(sourceDocument.DocumentGuid, sourceDocument.DocumentName, sourceDocument.ImportPath);
            // open video file if it wasn't
            if (mainViewModel.VideoViewModel.Document.Id == Guid.Empty)
            {
                mainViewModel.VideoViewModel.Document = firstDocument;
                reinitVideoPlayer();
                startVideo();
            }
        }

        private void loopVideo(long start, long end)
        {
            loopShouldBePaused = false;
            currentLoopNumber = 1;
            try
            {
                var segmentKey = Tuple.Create(mainViewModel.VideoViewModel.Document.Id, 
                    start + Settings.Instance.TimePaddingForLoop, end - Settings.Instance.TimePaddingForLoop);
                segmentPartsToLoop.Clear();
                segmentPartsToLoop.Add(segmentParts[segmentKey]);
            }
            catch (KeyNotFoundException)
            {
                // maybe some segments are joined
                var firstSegmentPart = segmentParts.FirstOrDefault(s => s.Key.Item1 == mainViewModel.VideoViewModel.Document.Id && 
                    s.Key.Item2 == start + Settings.Instance.TimePaddingForLoop);
                var lastSegmentPart = segmentParts.FirstOrDefault(s => s.Key.Item1 == mainViewModel.VideoViewModel.Document.Id && 
                    s.Key.Item3 == end - Settings.Instance.TimePaddingForLoop);
                if (firstSegmentPart.Value != null && lastSegmentPart.Value != null)
                {
                    segmentPartsToLoop.Add(firstSegmentPart.Value);
                    segmentPartsToLoop.Add(lastSegmentPart.Value);
                }
                else
                {
                    Log.Instance.WriteMessage("Unknown segment data received, try to reopen the document in memoQ.", SeverityOption.Warning);
                    return;
                }
            }
            startVideo(start);
        }

        private void startVideo(long start = 0)
        {
            var media = Settings.Instance.DocumentByDocumentGuid[mainViewModel.VideoViewModel.Document.Id].Media;
            if (string.IsNullOrWhiteSpace(media))
                mainViewModel.VideoViewModel.CannotOpen = true;
            else
            {
                // if the starting time would be after the end of the video
                if (myVlcControl.MediaPlayer.Length > 0 && start > myVlcControl.MediaPlayer.Length)
                {
                    var currentMedia = myVlcControl.MediaPlayer.GetCurrentMedia();
                    if (currentMedia != null && Path.GetFileName(media) == Path.GetFileName(currentMedia.Title))
                    {
                        // move the video almost to the end and pause
                        startTimeOutOfVideoBounds = true;
                        myVlcControl.MediaPlayer.Time = myVlcControl.MediaPlayer.Length - startOffsetInMs;
                        startTimeOutOfVideoBounds = false;
                        if (mainViewModel.VideoViewModel.IsPlaying)
                        {
                            mainViewModel.VideoViewModel.IsPlaying = false;
                            myVlcControl.MediaPlayer.Pause();
                        }
                        else
                        {
                            isVideoSliderChangedByHand = true;
                            sliderVideo.Value = myVlcControl.MediaPlayer.Time;
                            isVideoSliderChangedByHand = false;
                            updateLabels(getCurrentSegmentPart(myVlcControl.MediaPlayer.Time), myVlcControl.MediaPlayer.Time);
                        }
                        return;
                    }
                }

                playVideo(media);
                myVlcControl.MediaPlayer.Time = Math.Max(start, 0);
            }
        }

        private void playVideo(string media = null)
        {
            try
            {
                if (media != null)
                    myVlcControl.PlayMedia(media);
                else
                    myVlcControl.MediaPlayer.Play();
                var currentMedia = myVlcControl.MediaPlayer.GetCurrentMedia();
                if (currentMedia == null)
                {
                    mainViewModel.VideoViewModel.CannotOpen = true;
                }
                else
                {
                    mainViewModel.VideoViewModel.CannotOpen = false;
                    mainViewModel.VideoViewModel.IsPlaying = true;
                }
            }
            catch
            {
                mainViewModel.VideoViewModel.CannotOpen = true;
            }
            if (myVlcControl.MediaPlayer.Audio != null)
            {
                myVlcControl.MediaPlayer.Audio.Volume = (int)(Settings.Instance.VolumeValue * volumeMultiplier);
                myVlcControl.MediaPlayer.Audio.IsMute = Settings.Instance.VolumeMute;
            }
        }

        private void playBrowsedVideo(Document doc)
        {
            mainViewModel.VideoViewModel.Document = doc;
            reinitVideoPlayer();
            playVideo(doc.Media);
            if (segmentPartsToLoop.Any())
                myVlcControl.MediaPlayer.Time = segmentPartsToLoop.First().Subtitle.Start;
        }

        private void stopVideo()
        {
            if (myVlcControl != null && myVlcControl.MediaPlayer != null)
                myVlcControl.MediaPlayer.Stop();
        }

        private void updateLabels(SegmentPart segmentPart, long videoTime)
        {
            // Time frame
            mainViewModel.VideoViewModel.TimeFrame = $"{new DateTime(videoTime * TimeSpan.TicksPerMillisecond).ToString("H:mm:ss", CultureInfo.InvariantCulture)} /" +
                $" {new DateTime(myVlcControl.MediaPlayer.Length * TimeSpan.TicksPerMillisecond).ToString("H:mm:ss", CultureInfo.InvariantCulture)}";
            
            if (segmentPart == null || segmentPart.PreviewProperties == null)
                return;

            // WPM, CPS, Line length
            long? wordPerMinute = null;
            long? charPerSecond = null;
            long? lineLengthLimit = null;
            int wordCount = 0;
            int charCount = 0;
            int currentLineLength = 0;
            foreach (var previewProperty in segmentPart.PreviewProperties)
            {
                long value;
                if (previewProperty.Name == PropertyNames.Wpm && long.TryParse((string)previewProperty.Value, out value))
                    wordPerMinute = value;
                else if (previewProperty.Name == PropertyNames.Cps && long.TryParse((string)previewProperty.Value, out value))
                    charPerSecond = value;
                else if (previewProperty.Name == PropertyNames.LineLengthLimit && long.TryParse((string)previewProperty.Value, out value))
                    lineLengthLimit = value;
                else if (previewProperty.Name == PropertyNames.WordCount)
                    wordCount = Convert.ToInt32(previewProperty.Value);
                else if (previewProperty.Name == PropertyNames.CharCount)
                    charCount = Convert.ToInt32(previewProperty.Value);
            }

            string[] plainTextLines = segmentPart.Subtitle.PlainText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in plainTextLines)
            {
                currentLineLength = Math.Max(currentLineLength, line.Length);
            }
            
            var frameLength = segmentPart.Subtitle.End - segmentPart.Subtitle.Start;
            var currentWordPerMinute = Math.Ceiling((double)(wordCount * TimeSpan.TicksPerMinute / TimeSpan.TicksPerMillisecond) / frameLength);
            var currentCharPerSecond = Math.Ceiling((double)(charCount * TimeSpan.TicksPerSecond / TimeSpan.TicksPerMillisecond) / frameLength);

            txtWpm.Text = $"Words per minute: {currentWordPerMinute.ToString()}" + (wordPerMinute != null ? $"/{wordPerMinute.ToString()}" : "");
            txtWpm.Foreground = (wordPerMinute == null || currentWordPerMinute <= wordPerMinute) ? brushUnderLimit : brushAboveLimit;
            txtCps.Text = $"Characters per second: {currentCharPerSecond.ToString()}" + (charPerSecond != null ? $"/{charPerSecond.ToString()}" : "");
            txtCps.Foreground = (charPerSecond == null || currentCharPerSecond <= charPerSecond) ? brushUnderLimit : brushAboveLimit;
            txtLineLength.Text = $"Line length: {currentLineLength.ToString()}" + (lineLengthLimit != null ? $"/{lineLengthLimit.ToString()}" : "");
            txtLineLength.Foreground = (lineLengthLimit == null || currentLineLength <= lineLengthLimit) ? brushUnderLimit : brushAboveLimit;
        }

        private SegmentPart getCurrentSegmentPart(long videoTime)
        {
            var currentSegmentParts = segmentParts.Where(x => mainViewModel.VideoViewModel.DocumentsWithSameMedia.Any(d => d.Id == x.Key.Item1))
                                                  .Select(x => x.Value).OrderBy(s => s.Subtitle.Start);
            foreach (var segmentPart in currentSegmentParts)
            {
                if (videoTime >= segmentPart.Subtitle.Start && videoTime <= segmentPart.Subtitle.End)
                    return segmentPart;
            }
            return null;
        }
        #endregion Private methods
    }
}
