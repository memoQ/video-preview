using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using static MemoQ.VideoPreview.Log.LogEntry;
using static MemoQ.VideoPreview.MainViewModel;
using static MemoQ.VideoPreview.VideoViewModel;

namespace MemoQ.VideoPreview
{
    public class Settings
    {
        #region Singleton
        private static readonly Lazy<Settings> lazyInstance = new Lazy<Settings>(() => new Settings());

        public static Settings Instance { get { return lazyInstance.Value; } }

        private Settings()
        {
            ResetSettings();
        }
        #endregion Singleton

        private const string Tag_Root = "MemoQVideoPreviewSettings";
        private const string Tag_NamedPipeAddress = "NamedPipeAddress";
        private const string Tag_AutoConnect = "AutoConnect";
        private const string Tag_TimePaddingForLoop = "TimePaddingForLoop";
        private const string Tag_LoopNumber = "LoopNumber";
        private const string Tag_LoopSelection = "LoopSelection";
        private const string Tag_PlayMode = "PlayMode";
        private const string Tag_Volume = "Volume";
        private const string Tag_VolumeValue = "Value";
        private const string Tag_VolumeMute = "Mute";
        private const string Tag_Documents = "Documents";
        private const string Tag_Document = "Document";
        private const string Att_Document_Id = "Id";
        private const string Tag_Document_Name = "Name";
        private const string Tag_Media = "Media";
        private const string Tag_MinimalSeverityToShowInLog = "MinimalSeverityToShowInLog";
        private const string Tag_Window = "Window";
        private const string Tag_Top = "Top";
        private const string Tag_Left = "Left";
        private const string Tag_Width = "Width";
        private const string Tag_Height = "Height";
        private const string Tag_Maximized = "Maximized";
        private const string Tag_AlwaysOnTop = "AlwaysOnTop";
        private const string Tag_VlcLibPath = "VlcLibrariesPath";

        private readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName, "settings.xml");

        public string NamedPipeAddress { get; set; }
        public bool AutoConnect { get; set; }
        public int TimePaddingForLoop { get; set; }
        public bool LoopSelection { get; set; }
        public int LoopNumber { get; set; }
        public PlayMode PlayMode { get; set; }
        public int VolumeValue { get; set; }
        public bool VolumeMute { get; set; }
        public IDictionary<Guid, Document> DocumentByDocumentGuid { get; set; } = new Dictionary<Guid, Document>();
        public SeverityOption MinimalSeverityToShowInLog { get; set; }
        public double WindowTop { get; set; }
        public double WindowLeft { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public bool WindowMaximized { get; set; }
        public bool AlwaysOnTop { get; set; }
        public string VlcLibPath { get; set; }

        public void ResetSettings()
        {
            NamedPipeAddress = "MQ_PREVIEW_PIPE";
            AutoConnect = true;
            TimePaddingForLoop = 1000;
            LoopSelection = false;
            LoopNumber = 0;
            PlayMode = PlayMode.Selection;
            VolumeValue = 50;
            VolumeMute = false;
            DocumentByDocumentGuid.Clear();
            MinimalSeverityToShowInLog = SeverityOption.Warning;
            WindowTop = 0;
            WindowLeft = 0;
            WindowWidth = 654;
            WindowHeight = 520;
            WindowMaximized = false;
            AlwaysOnTop = false;
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            if (currentDirectory != null)
                VlcLibPath = Path.Combine(currentDirectory, "libvlc");
        }

        public void LoadSettings()
        {
            try
            {
                var xDocument = XDocument.Load(SettingsPath);
                var root = xDocument.Element(Tag_Root);
                NamedPipeAddress = root.Element(Tag_NamedPipeAddress).Value;
                AutoConnect = bool.Parse(root.Element(Tag_AutoConnect).Value);
                TimePaddingForLoop = int.Parse(root.Element(Tag_TimePaddingForLoop).Value);
                LoopSelection = bool.Parse(root.Element(Tag_LoopSelection).Value);
                LoopNumber = int.Parse(root.Element(Tag_LoopNumber).Value);
                if (!Enum.TryParse(root.Element(Tag_PlayMode).Value, out PlayMode playMode))
                    playMode = PlayMode.Selection;
                PlayMode = playMode;
                var xVolume = root.Element(Tag_Volume);
                VolumeValue = int.Parse(xVolume.Element(Tag_VolumeValue).Value);
                VolumeMute = bool.Parse(xVolume.Element(Tag_VolumeMute).Value);

                DocumentByDocumentGuid.Clear();
                foreach (var item in root.Element(Tag_Documents).Elements())
                {
                    var documentGuid = new Guid(item.Attribute(Att_Document_Id).Value);
                    var documentName = item.Element(Tag_Document_Name).Value;
                    var videoFile = item.Element(Tag_Media).Value;

                    DocumentByDocumentGuid.Add(documentGuid, new Document(documentGuid, documentName, videoFile));
                }
                MinimalSeverityToShowInLog = (SeverityOption)Enum.Parse(typeof(SeverityOption), root.Element(Tag_MinimalSeverityToShowInLog).Value);

                var xWindows = root.Element(Tag_Window);
                WindowTop = double.Parse(xWindows.Element(Tag_Top).Value);
                WindowLeft = double.Parse(xWindows.Element(Tag_Left).Value);
                WindowWidth = double.Parse(xWindows.Element(Tag_Width).Value);
                WindowHeight = double.Parse(xWindows.Element(Tag_Height).Value);
                WindowMaximized = bool.Parse(xWindows.Element(Tag_Maximized).Value);
                AlwaysOnTop = bool.Parse(xWindows.Element(Tag_AlwaysOnTop).Value);
                VlcLibPath = root.Element(Tag_VlcLibPath).Value;
            }
            catch (IOException)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                SaveSettings();
            }
            catch
            {
                if (DialogResult.OK == MessageBox.Show($"The settings file is corrupt.{Environment.NewLine}{Environment.NewLine}Press \"OK\" to reset the settings file or \"Cancel\" to exit", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error))
                {
                    ResetSettings();
                }
                else
                {
                    Environment.Exit(ExitCodes.SettingsFileCorrupt);
                }
            }
        }

        public void SaveSettings()
        {
            new XDocument(
                new XElement(Tag_Root,
                    new XElement(Tag_NamedPipeAddress, NamedPipeAddress),
                    new XElement(Tag_AutoConnect, AutoConnect.ToString()),
                    new XElement(Tag_TimePaddingForLoop, TimePaddingForLoop.ToString()),
                    new XElement(Tag_LoopSelection, LoopSelection.ToString()),
                    new XElement(Tag_LoopNumber, LoopNumber.ToString()),
                    new XElement(Tag_PlayMode, PlayMode.ToString()),
                    new XElement(Tag_Volume,
                        new XElement(Tag_VolumeValue, VolumeValue.ToString()),
                        new XElement(Tag_VolumeMute, VolumeMute.ToString())),
                    new XElement(Tag_Documents, DocumentByDocumentGuid.Select(x =>
                        new XElement(Tag_Document, new XAttribute(Att_Document_Id, x.Key), new XElement(Tag_Document_Name, x.Value.Name), new XElement(Tag_Media, x.Value.Media)))),
                    new XElement(Tag_MinimalSeverityToShowInLog, MinimalSeverityToShowInLog.ToString()),
                    new XElement(Tag_Window,
                        new XElement(Tag_Top, WindowTop.ToString()),
                        new XElement(Tag_Left, WindowLeft.ToString()),
                        new XElement(Tag_Width, WindowWidth.ToString()),
                        new XElement(Tag_Height, WindowHeight.ToString()),
                        new XElement(Tag_Maximized, WindowMaximized.ToString()),
                        new XElement(Tag_AlwaysOnTop, AlwaysOnTop.ToString())),
                    new XElement(Tag_VlcLibPath, VlcLibPath))
            ).Save(SettingsPath);
        }
    }
}
