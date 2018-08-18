using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MemoQ.VideoPreview.Log.LogEntry;

namespace MemoQ.VideoPreview
{
    public class SettingsViewModel : ObservableViewModelBase, IDataErrorInfo
    {
        #region Fields

        private const string pipeNamePrefix = "\\\\.\\pipe\\";
        private static char[] illegalCharactersInPipeAddress = new char[] { '?', '|', '"', '*', '<', '>', '\\', };

        /// <summary>
        /// Time (ms) to show before and after a loop
        /// </summary>
        public double timePaddingForLoop;

        /// <summary>
        /// Number of loops
        /// </summary>
        public int loopNumber;

        /// <summary>
        /// Whether the tool should be shown always on top
        /// </summary>
        public bool alwaysOnTop;

        /// <summary>
        /// The address of the named pipe memoQ service
        /// </summary>
        private string namedPipeAddress;

        /// <summary>
        /// The minimal severity to show in the log
        /// </summary>
        private string minimalSeverityToShowInLog;

        #endregion Fields

        #region Properties

        public double TimePaddingForLoop
        {
            get
            {
                return timePaddingForLoop;
            }
            set
            {
                timePaddingForLoop = value;
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
                OnPropertyChanged();
            }
        }

        public bool AlwaysOnTop
        {
            get
            {
                return alwaysOnTop;
            }
            set
            {
                alwaysOnTop = value;
                OnPropertyChanged();
            }
        }

        public string NamedPipeAddress
        {
            get
            {
                return namedPipeAddress;
            }
            set
            {
                namedPipeAddress = value;
                OnPropertyChanged();
            }
        }

        public string MinimalSeverityToShowInLog
        {
            get
            {
                return minimalSeverityToShowInLog;
            }
            set
            {
                minimalSeverityToShowInLog = value;
                OnPropertyChanged();
            }
        }

        public bool IsValidNamedPipeAddress
        {
            get
            {
                return NamedPipeAddress.Length > 0 && pipeNamePrefix.Length + NamedPipeAddress.Length <= 256 &&
                    !NamedPipeAddress.Any(c => illegalCharactersInPipeAddress.Contains(c));
            }
        }

        public string Error
        {
            get { return "..."; }
        }

        /// <summary>
        /// Will be called for each and every property when ever its value is changed
        /// </summary>
        /// <param name="columnName">Name of the property whose value is changed</param>
        /// <returns></returns>
        public string this[string columnName]
        {
            get
            {
                return Validate(columnName);
            }
        }

        #endregion Properties

        #region Constructor

        public SettingsViewModel()
        {
            UpdateProperties();
        }

        #endregion Constructor

        #region Public methods

        public void UpdateProperties()
        {
            TimePaddingForLoop = (double)Settings.Instance.TimePaddingForLoop / 1000;
            LoopNumber = Settings.Instance.LoopNumber;
            AlwaysOnTop = Settings.Instance.AlwaysOnTop;
            NamedPipeAddress = Settings.Instance.NamedPipeAddress;
            MinimalSeverityToShowInLog = Settings.Instance.MinimalSeverityToShowInLog.ToString();
        }

        #endregion Public methods

        #region Private methods

        private string Validate(string propertyName)
        {
            string validationMessage = string.Empty;
            switch (propertyName)
            {
                case "NamedPipeAddress":
                    if (!IsValidNamedPipeAddress)
                        validationMessage = "Named pipe service address is invalid.";
                    break;
                case "TimePaddingForLoop":
                    if (TimePaddingForLoop < 0)
                        validationMessage = "Time cannot be negative.";
                    break;
                case "LoopNumber":
                    if (LoopNumber < 0)
                        validationMessage = "Number of loops cannot be negative.";
                    break;
            }

            return validationMessage;
        }

        #endregion Private methods
    }
}
