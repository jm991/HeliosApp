using System;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Helios.Common;
using Helios.Model;
using Windows.ApplicationModel.Activation;
using Windows.Media.Editing;
using Windows.Media.Core;
using System.Diagnostics;
using System.IO;
using Windows.Storage.Pickers;

namespace Helios.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase, IFileOpenPickerContinuable
    {
        /// <summary>
        /// The <see cref="WelcomeTitle" /> property's name.
        /// </summary>
        public const string WelcomeTitlePropertyName = "WelcomeTitle";


        #region Variables (private)

        private readonly IDataService _dataService;
        private readonly INavigationService _navigationService;

        private RelayCommand _navigateCommand;
        private string _originalTitle;
        private string _welcomeTitle = string.Empty;

        private MediaClip m_clip;
        private MediaComposition m_composition;
        private string fileName;
        private MediaStreamSource m_mediaSource;
        private bool m_mediaLoaded;
        private TimeSpan m_lastPosition;

        #endregion


        #region Properties (public)

        public TimeSpan LastPosition
        {
            get
            {
                if (m_lastPosition == null)
                {
                    m_lastPosition = TimeSpan.Zero;
                }
                return m_lastPosition;
            }
            set
            {
                Set(ref m_lastPosition, value, true, Utilities.GetMemberName(() => LastPosition));
            }
        }

        public double TrimStartPosition
        {
            get
            {
                return m_clip == null ? 0 : m_clip.TrimTimeFromStart.TotalMilliseconds;
            }
            set
            {
                m_clip.TrimTimeFromStart = TimeSpan.FromMilliseconds(value);
                LastPosition = TimeSpan.Zero;

                RaisePropertyChanged(Utilities.GetMemberName(() => TrimStartPosition));
            }
        }

        public double TrimEndPosition
        {
            get
            {
                return m_clip == null ? 0 : m_clip.TrimTimeFromEnd.TotalMilliseconds;
            }
            set
            {
                m_clip.TrimTimeFromEnd = m_clip.OriginalDuration - TimeSpan.FromMilliseconds(value);
                LastPosition = m_composition.Duration;
                RaisePropertyChanged(Utilities.GetMemberName(() => TrimEndPosition));
            }
        }

        public MediaStreamSource MediaSource
        {
            get
            {
                return m_mediaSource;
            }
            set
            {
                Set(ref m_mediaSource, value, true, Utilities.GetMemberName(() => MediaSource));
            }
        }


        public bool MediaLoaded
        {
            get
            {
                return m_mediaLoaded;
            }
            set
            {
                Set(ref m_mediaLoaded, value, true, Utilities.GetMemberName(() => MediaLoaded));
            }
        }

        public MediaClip Clip
        {
            get
            {
                return m_clip;
            }
        }

        /// <summary>
        /// Gets the NavigateCommand.
        /// </summary>
        public RelayCommand NavigateCommand
        {
            get
            {
                return _navigateCommand
                       ?? (_navigateCommand = new RelayCommand(
                           () => _navigationService.Navigate(typeof(SecondPage))));
            }
        }

        public RelayCommand OpenFileCommand { get; set; }

        /// <summary>
        /// Gets the WelcomeTitle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string WelcomeTitle
        {
            get
            {
                return _welcomeTitle;
            }

            set
            {
                Set(ref _welcomeTitle, value);
            }
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(
            IDataService dataService,
            INavigationService navigationService)
        {
            _dataService = dataService;
            _navigationService = navigationService;
            OpenFileCommand = new RelayCommand(OpenFile);

            Initialize();
        }

        #endregion


        #region Methods

        private void OpenFile()
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".mp4");
            openPicker.FileTypeFilter.Add(".wmv");
            openPicker.FileTypeFilter.Add(".avi");

            // Store this instance as the last caller of the continuation code
            ((App)(App.Current)).PickerCaller = this;

            // Launch file open picker and caller app is suspended and may be terminated if required
            openPicker.PickSingleFileAndContinue();
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}

        public void Load(DateTime lastVisit)
        {
            if (lastVisit > DateTime.MinValue)
            {
                WelcomeTitle = string.Format(
                    "{0} (last visit on the {1})",
                    _originalTitle,
                    lastVisit);
            }
        }

        private async Task Initialize()
        {
            try
            {
                var item = await _dataService.GetData();
                _originalTitle = item.Title;
                WelcomeTitle = item.Title;
            }
            catch (Exception ex)
            {
                // Report error here
                Utilities.MessageBox(ex.ToString());
            }
        }

        #endregion


        #region Event Handlers

        /// <summary>
        /// Handle the returned files from file picker
        /// This method is triggered by ContinuationManager based on ActivationKind
        /// </summary>
        /// <param name="args">File open picker continuation activation argment. It cantains the list of files user selected with file open picker </param>
        public async void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {
            if (args.Files.Count > 0)
            {
                fileName = Path.GetFileNameWithoutExtension(args.Files[0].Name);
                Debug.WriteLine("Picked video: " + fileName + " with full name: " + args.Files[0].Name);

                MediaLoaded = true;

                m_clip = await MediaClip.CreateFromFileAsync(args.Files[0]);
                RaisePropertyChanged(Utilities.GetMemberName(() => Clip));
                m_composition = new MediaComposition();
                m_composition.Clips.Add(m_clip);

                // Set up the MSS for the MediaElement to bind to for preview
                // TODO: pass in the preview streamsource and grab the screensize to determine this in addition to the aspect ratio of the video
                MediaSource = m_composition.GenerateMediaStreamSource();
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        #endregion
    }
}