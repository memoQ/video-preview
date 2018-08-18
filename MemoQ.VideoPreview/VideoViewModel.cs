using MemoQ.PreviewInterfaces;
using MemoQ.PreviewInterfaces.Entities;
using MemoQ.PreviewInterfaces.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MemoQ.VideoPreview
{
    public class VideoViewModel : ObservableViewModelBase
    {
        #region Fields

        public enum PlayMode { Selection, All }

        /// <summary>
        /// The preview service proxy to communicate with memoQ
        /// </summary>
        private PreviewServiceProxy previewServiceProxy;

        /// <summary>
        /// The document that is currently seen
        /// </summary>
        private Document document;

        /// <summary>
        /// The documents that are all seen in the current same video (there may be more in case of views for example)
        /// </summary>
        private List<Document> documentsWithSameMedia;

        /// <summary>
        /// Whether the video is loading...
        /// </summary>
        private bool isLoading;

        /// <summary>
        /// Whether the video is playing or paused
        /// </summary>
        private bool isPlaying;

        /// <summary>
        /// Whether the playing mode of the video is Selection
        /// </summary>
        private bool isSelectionMode;

        /// <summary>
        /// Whether to loop the selection or play only once. Only in selection mode.
        /// </summary>
        private bool loopSelection;

        /// <summary>
        /// The number of looping times. Only in selection mode.
        /// </summary>
        private int loopNumber;

        /// <summary>
        /// Whether the video is muted.
        /// </summary>
        private bool isMute;

        /// <summary>
        /// The actual time and length in H:mm:ss/H:mm:ss format
        /// </summary>
        private string timeFrame;
        
        /// <summary>
        /// The actual path/URL of the video/media
        /// </summary>
        private string media;

        /// <summary>
        /// The actual video could not open
        /// </summary>
        private bool cannotOpen;

        #endregion Fields

        #region Properties

        public Document Document
        {
            get
            {
                return document;
            }
            set
            {
                document = value;
                OnPropertyChanged();
            }
        }

        public List<Document> DocumentsWithSameMedia
        {
            get
            {
                return documentsWithSameMedia;
            }
            set
            {
                documentsWithSameMedia = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get
            {
                return isLoading;
            }
            set
            {
                isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get
            {
                return isPlaying;
            }
            set
            {
                isPlaying = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelectionMode
        {
            get
            {
                return isSelectionMode;
            }
            set
            {
                isSelectionMode = value;
                Settings.Instance.PlayMode = isSelectionMode ? PlayMode.Selection : PlayMode.All;
                Settings.Instance.SaveSettings();
                OnPropertyChanged();
            }
        }

        public bool LoopSelection
        {
            get
            {
                return loopSelection;
            }
            set
            {
                loopSelection = value;
                Settings.Instance.LoopSelection = loopSelection;
                Settings.Instance.SaveSettings();
                OnPropertyChanged();
            }
        }

        public int LoopNumber
        {
            get
            {
                return loopNumber;
            }
            set
            {
                loopNumber = value;
                Settings.Instance.LoopNumber = loopNumber;
                Settings.Instance.SaveSettings();
                OnPropertyChanged();
            }
        }

        public bool IsMute
        {
            get
            {
                return isMute;
            }
            set
            {
                isMute = value;
                OnPropertyChanged();
            }
        }

        public string TimeFrame
        {
            get
            {
                return timeFrame;
            }
            set
            {
                timeFrame = value;
                OnPropertyChanged();
            }
        }

        public bool CannotOpen
        {
            get
            {
                return cannotOpen;
            }
            set
            {
                cannotOpen = value;
                if (cannotOpen)
                {
                    IsPlaying = false;
                    IsLoading = false;
                }
                OnPropertyChanged();
            }
        }

        public string Media
        {
            get
            {
                return media;
            }
            set
            {
                media = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Constructor

        public VideoViewModel(PreviewServiceProxy serviceProxy)
        {
            previewServiceProxy = serviceProxy;
            document = new Document(Guid.Empty, null, null);
            documentsWithSameMedia = new List<Document>();
            IsLoading = false;
            IsPlaying = false;
            IsSelectionMode = Settings.Instance.PlayMode == PlayMode.Selection;
            LoopSelection = true;
            LoopNumber = Settings.Instance.LoopNumber;
            IsMute = false;
            TimeFrame = "0:00:00 / 0:00:00";
            CannotOpen = false;
            Media = string.Empty;
        }

        #endregion Constructor
    }
}
