using MemoQ.PreviewInterfaces;
using MemoQ.PreviewInterfaces.Entities;
using MemoQ.PreviewInterfaces.Exceptions;
using MemoQ.PreviewInterfaces.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static MemoQ.VideoPreview.Log.LogEntry;

namespace MemoQ.VideoPreview
{
    public class ConnectViewModel : ObservableViewModelBase
    {
        private const string previewIdRegex = "memoQSubtitlePlugin";
        private const string previewToolName = "memoQ video preview";
        private const string previewToolDescription = "Provides preview for subtitles in video files";
        private static readonly Guid previewToolId = new Guid("07E1A8E0-5D32-49FD-932A-0B9415D668D0");

        #region Fields

        private Action stopVideo;

        /// <summary>
        /// The preview service proxy to communicate with memoQ
        /// </summary>
        private PreviewServiceProxy previewServiceProxy;

        /// <summary>
        /// An interface that handles the callback methods called by memoQ
        /// </summary>
        private IPreviewToolCallback previewToolCallback;

        /// <summary>
        /// Whether the tool is connected to memoQ
        /// </summary>
        private bool isConnected;

        /// <summary>
        /// Whether the tool should automatically connect (to memoQ) on startup
        /// </summary>
        private bool autoConnectOnStartup;

        #endregion Fields

        #region Properties

        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                isConnected = value;
                if (!IsConnected && stopVideo != null)
                    stopVideo();
                OnPropertyChanged();
            }
        }

        public bool AutoConnectOnStartup
        {
            get
            {
                return autoConnectOnStartup;
            }
            set
            {
                autoConnectOnStartup = value;
                Settings.Instance.AutoConnect = autoConnectOnStartup;
                Settings.Instance.SaveSettings();
                OnPropertyChanged();
            }
        }

        public string WarningText => !IsConnected ? "This preview tool is not yet connected to your memoQ." : "";

        public string SuggestionText => !IsConnected ? "To connect the preview tool: start memoQ, then come back here, and click the \"Connect\" button. " +
            $"If the tool is connecting for the first time, switch to memoQ to accept registration.{Environment.NewLine}{Environment.NewLine}" +
            "Please also make sure that connecting external preview tools is enabled in memoQ." : "";

        public PreviewServiceProxy PreviewServiceProxy
        {
            get
            {
                if (previewServiceProxy != null)
                    return previewServiceProxy;
                try
                {
                    previewServiceProxy = new PreviewServiceProxy(previewToolCallback, Settings.Instance.NamedPipeAddress, CommunicationProtocols.NamedPipe);
                }
                catch (PreviewServiceUnavailableException)
                {
                    IsConnected = false;
                    Log.Instance.WriteMessage(Log.PreviewUnavailableMessage, SeverityOption.Warning);
                }
                catch (NotSupportedException)
                {
                    Settings.Instance.NamedPipeAddress = "MQ_PREVIEW_PIPE";
                    Settings.Instance.SaveSettings();
                    previewServiceProxy = new PreviewServiceProxy(previewToolCallback, Settings.Instance.NamedPipeAddress, CommunicationProtocols.NamedPipe);
                }
                return previewServiceProxy;
            }
        }

        #endregion Properties

        #region Constructor

        public ConnectViewModel(PreviewServiceProxy serviceProxy, IPreviewToolCallback callback, Action stopVideoAction, bool isConnected)
        {
            previewServiceProxy = serviceProxy;
            previewToolCallback = callback;
            IsConnected = isConnected;
            stopVideo = stopVideoAction;
            AutoConnectOnStartup = Settings.Instance.AutoConnect;
        }

        #endregion Constructor

        #region Public methods

        public async void Register()
        {
            Log.Instance.WriteMessage($"Registering to memoQ.", SeverityOption.Info);
            var request = new RegistrationRequest(previewToolId, previewToolName, previewToolDescription,
                System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, previewIdRegex, false,
                ContentComplexityLevel.PlainWithInterpretedFormatting,
                new string[] { PropertyNames.Wpm, PropertyNames.Cps, PropertyNames.LineLengthLimit, PropertyNames.WordCount, PropertyNames.CharCount });

            var requestStatus = await CallProxyMethod(new Func<RequestStatus>(() => PreviewServiceProxy?.Register(request)));
            IsConnected = (requestStatus != null && requestStatus.RequestAccepted);
            if (!IsConnected)
                Log.Instance.WriteMessage($"Could not register this preview tool in memoQ. Check if memoQ allows connecting external preview tools, and memoQ video preview tool is enabled under \"Installed external preview tools\".", SeverityOption.Warning);
        }

        public async void Connect()
        {
            Log.Instance.WriteMessage($"Connecting to memoQ.", SeverityOption.Info);
            var requestStatus = await CallProxyMethod(new Func<RequestStatus>(() => PreviewServiceProxy?.ConnectAndRequestPreviewPartIdUpdate(previewToolId)));
            if (requestStatus != null && requestStatus.RequestAccepted)
                IsConnected = true;
            else
                // if connecting failed, try a register, as maybe the tool is connecting for the first time
                Register();
        }

        public async void Disconnect()
        {
            Log.Instance.WriteMessage($"Disconnecting from memoQ.", SeverityOption.Info);
            try
            {
                await CallProxyMethod(new Func<RequestStatus>(() => PreviewServiceProxy?.Disconnect()));
                PreviewServiceProxy?.Dispose();
            }
            catch (PreviewServiceUnavailableException)
            {
                // if memoQ is not available, the disconnect might fail
            }
            finally
            {
                previewServiceProxy = null;
                IsConnected = false;
            }
        }

        #endregion Public methods

        #region Properties -- Command

        private ICommand connectCommand;
        public ICommand ConnectCommand
        {
            get
            {
                return connectCommand ?? (connectCommand = new CommandHandler(() => executeConnect(), connectCanExecute()));
            }
        }

        #endregion Properties -- Command

        #region Commands -- CanExecute

        private bool connectCanExecute()
        {
            return !IsConnected;
        }

        #endregion Commands -- CanExecute

        #region Commands -- Execute

        private void executeConnect()
        {
            Connect();
        }

        #endregion Commands -- Execute

        #region Private methods

        /// <summary>
        /// Helper function to call proxy methods and handle PreviewServiceUnavailableException in one place.
        /// </summary>
        public async Task<RequestStatus> CallProxyMethod(Func<RequestStatus> proxyMethod)
        {
            try
            {
                return await Task.Run(() => proxyMethod());
            }
            catch (PreviewServiceUnavailableException)
            {
                IsConnected = false;
                Log.Instance.WriteMessage(Log.PreviewUnavailableMessage, SeverityOption.Warning);
            }
            catch (PreviewToolAlreadyConnectedException)
            {
                return RequestStatus.Success();
            }
            catch (System.IO.IOException e)
            {
                Log.Instance.WriteMessage($"Error to connect: {e.Message}", SeverityOption.Error);
            }
            catch (Exception ex)
            {
                Log.Instance.WriteMessage($"Unexpected error occurred: {ex}", SeverityOption.Error);
            }
            return null;
        }

        #endregion Private methods
    }
}
