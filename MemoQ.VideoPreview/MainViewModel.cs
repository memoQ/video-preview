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
using static MemoQ.VideoPreview.Log.LogEntry;
using static MemoQ.VideoPreview.VideoViewModel;

namespace MemoQ.VideoPreview
{
    public class MainViewModel : ObservableViewModelBase
    {
        #region Fields

        /// <summary>
        /// The preview service proxy to communicate with memoQ
        /// </summary>
        private PreviewServiceProxy previewServiceProxy;

        /// <summary>
        /// View model for connection related properties
        /// </summary>
        private ConnectViewModel connectViewModel;

        /// <summary>
        /// View model for video related properties
        /// </summary>
        private VideoViewModel videoViewModel;

        #endregion Fields

        #region Properties

        public ConnectViewModel ConnectViewModel
        {
            get
            {
                return connectViewModel;
            }
            set
            {
                connectViewModel = value;
                OnPropertyChanged();
            }
        }

        public VideoViewModel VideoViewModel
        {
            get
            {
                return videoViewModel;
            }
            set
            {
                videoViewModel = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Constructor

        public MainViewModel(IPreviewToolCallback previewToolCallback, Action stopVideoAction)
        {
            Settings.Instance.LoadSettings();
            try
            {
                previewServiceProxy = new PreviewServiceProxy(previewToolCallback, Settings.Instance.NamedPipeAddress, CommunicationProtocols.NamedPipe);
            }
            catch (PreviewServiceUnavailableException)
            {
                Log.Instance.WriteMessage(Log.PreviewUnavailableMessage, SeverityOption.Info);
            }
            catch (NotSupportedException)
            {
                Settings.Instance.NamedPipeAddress = "MQ_PREVIEW_PIPE";
                Settings.Instance.SaveSettings();
                previewServiceProxy = new PreviewServiceProxy(previewToolCallback, Settings.Instance.NamedPipeAddress, CommunicationProtocols.NamedPipe);
            }
            ConnectViewModel = new ConnectViewModel(previewServiceProxy, previewToolCallback, stopVideoAction, false);
            VideoViewModel = new VideoViewModel(previewServiceProxy);
        }

        #endregion Constructor

        #region Public methods

        public void Connect()
        {
            ConnectViewModel.Connect();
        }

        public void Disconnect()
        {
            ConnectViewModel.Disconnect();
        }

        public async void RequestContentUpdate(string[] previewPartIds)
        {
            Log.Instance.WriteMessage($"Requesting content update from memoQ.", SeverityOption.Info);
            if (!previewPartIds.Any())
                return;

            var request = new ContentUpdateRequestFromPreviewTool(previewPartIds, null);
            await ConnectViewModel.CallProxyMethod(new Func<RequestStatus>(() => previewServiceProxy?.RequestContentUpdate(request)));
        }

        public async void RequestHighlightChange(SegmentPart segmentPart)
        {
            Log.Instance.WriteMessage($"Requesting highlight from memoQ.", SeverityOption.Info);
            var sourceFocusedRange = new FocusedRange(0, segmentPart.SourceContent.Length);
            var targetFocusedRange = new FocusedRange(0, segmentPart.TargetContent.Length);
            var request = new ChangeHighlightRequestFromPreviewTool(segmentPart.PreviewPartId, segmentPart.SourceLangCode, segmentPart.TargetLangCode,
                segmentPart.SourceContent, segmentPart.TargetContent, sourceFocusedRange, targetFocusedRange);

            var requestStatus = await ConnectViewModel.CallProxyMethod(new Func<RequestStatus>(() => previewServiceProxy?.RequestHighlightChange(request)));
            if (requestStatus == null || !requestStatus.RequestAccepted)
                ConnectViewModel.IsConnected = false;
        }

        #endregion Public methods
    }
}