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
using Windows.UI.ViewManagement;
using Windows.Storage;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Controls;

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

        private const string fileNameAppend = "_Trim.mp4";

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
            private set
            {
                Set(ref m_clip, value);
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

        public RelayCommand SaveFileCommand { get; set; }

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
            SaveFileCommand = new RelayCommand(TranscodeVideo);

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

        public async void TranscodeVideo()
        {
            bool succeeded = false;
            StatusBar statusBar = StatusBar.GetForCurrentView();

            // Transcoding cannot be used if there is a MediaElement playing; unset it
            MediaSource = null;

            // Create a StorageFile to hold the result
            StorageFile outputFile = await KnownFolders.SavedPictures.CreateFileAsync(fileName + fileNameAppend, CreationCollisionOption.GenerateUniqueName);

            try
            {
                // Set up the progress bar
                statusBar.ProgressIndicator.ProgressValue = 0.0f;
                await statusBar.ProgressIndicator.ShowAsync();

                // Begin rendering
                var renderOperation = m_composition.RenderToFileAsync(outputFile);

                renderOperation.Progress = async (_, progress) =>
                {
                    // Update the progress bar
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () =>
                        {
                            statusBar.ProgressIndicator.ProgressValue = progress / 100.0;
                        });
                };

                await renderOperation;
                succeeded = true;
            }
            catch (Exception ex)
            {
                Utilities.MessageBox(ex.Message);
            }

            await statusBar.ProgressIndicator.HideAsync();

            // Transcode completed, show result
            if (succeeded)
            {
                ContentDialog complete = new ContentDialog();
                complete.Content = "Transcode complete.";
                complete.PrimaryButtonText = "Play";
                complete.SecondaryButtonText = "Cancel";
                var result = await complete.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    Uri savedUri = new Uri(outputFile.Path);
                    // TODO: navigate to second page and put a mediaplayer control there
                    //Frame.Navigate(typeof(Preview), savedUri);
                }
            }

            // Reinitialize the MediaElement now that we are done
            // TODO: use low res here too
            MediaSource = m_composition.GenerateMediaStreamSource();
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
                DataItem item = await _dataService.GetData();
                _originalTitle = item.Title;
                WelcomeTitle = item.Title;

                //var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Video.mp4", UriKind.Absolute));
                //await LoadFile(file);

                //Clip = item.Clip;
                //MediaLoaded = item.MediaLoaded;

                //TrimStartPosition = 1000;
                //TrimEndPosition = 1000;
            }
            catch (Exception ex)
            {
                WelcomeTitle = ex.ToString();
                // Report error here
                if (!IsInDesignModeStatic)
                {
                    Utilities.MessageBox(ex.ToString());
                }
            }
        }

        private async Task LoadFile(StorageFile file)
        {
            fileName = Path.GetFileNameWithoutExtension(file.Name);
            Debug.WriteLine("Picked video: " + fileName + " with full name: " + file.Name);

            MediaLoaded = true;

            Clip = await MediaClip.CreateFromFileAsync(file);
            m_composition = new MediaComposition();
            m_composition.Clips.Add(Clip);

            // Set up the MSS for the MediaElement to bind to for preview
            // TODO: pass in the preview streamsource and grab the screensize to determine this in addition to the aspect ratio of the video
            MediaSource = m_composition.GenerateMediaStreamSource();
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
                await LoadFile(args.Files[0]);
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        #endregion
    }
}