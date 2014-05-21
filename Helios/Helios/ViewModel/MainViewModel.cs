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
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

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

        private const double DEFAULT_DURATION = 5.0;
        private const int HEIGHT = 100;

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

        private List<string> handledExtensions;

        private List<string> imageExtensions = new List<string>() { ".jpg", ".jpeg", ".png" };

        private List<string> videoExtensions = new List<string>() { ".mp4", ".wmv", ".avi" };

        private ObservableCollection<MediaClip> clips;
        
        #endregion


        #region Properties (public)

        //public IList<MediaClip> Clips
        public ObservableCollection<MediaClip> Clips
        {
            get
            {
                //return m_composition.Clips;
                return clips;
            }
            set
            {
                m_composition.Clips.Clear();
                foreach (MediaClip curClip in value)
                {
                    m_composition.Clips.Add(curClip);
                }

                Set(ref clips, value, true, Utilities.GetMemberName(() => Clips));
            }
        }

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

        public Uri MyUri
        {
            get
            {
                string uri = "";
                if (m_composition.Clips.Count > 0)
                {
                    m_composition.Clips[0].UserData.TryGetValue("thumb", out uri);
                    return new Uri(uri);
                }
                return new Uri("ms-appx:///SampleData/Images/60Vanilla.png", UriKind.Absolute);
            }
            set
            {
                RaisePropertyChanged(Utilities.GetMemberName(() => MyUri));
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

            handledExtensions = new List<string>();
            handledExtensions.AddRange(imageExtensions);
            handledExtensions.AddRange(videoExtensions);

            m_composition = new MediaComposition();
            clips = new ObservableCollection<MediaClip>();

            Initialize();
        }

        #endregion


        #region Methods

        private void OpenFile()
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            foreach (string extension in handledExtensions)
            {
                openPicker.FileTypeFilter.Add(extension);
            }

            // Store this instance as the last caller of the continuation code
            ((App)(App.Current)).PickerCaller = this;

            // Launch file open picker and caller app is suspended and may be terminated if required
            openPicker.PickMultipleFilesAndContinue();
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
            
            if (handledExtensions.Contains(Path.GetExtension(file.Path)))
            {
                MediaClip newClip;

                if (imageExtensions.Contains(Path.GetExtension(file.Path)))
                {
                    newClip = await MediaClip.CreateFromImageFileAsync(file, TimeSpan.FromSeconds(DEFAULT_DURATION));
                }
                else // if (videoExtensions.Contains(Path.GetExtension(file.Path)))
                {
                    newClip = await MediaClip.CreateFromFileAsync(file);
                }

                m_composition.Clips.Add(newClip);

                // Render a thumbnail from the center of the clip's duration
                ImageStream x = await m_composition.GetThumbnailAsync(TimeSpan.FromMilliseconds(newClip.StartTimeInComposition.TotalMilliseconds + newClip.TrimmedDuration.TotalMilliseconds / 2d), HEIGHT, 0, VideoFramePrecision.NearestKeyFrame);
                

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                // Write data to a file
                StorageFile imageFile = await localFolder.CreateFileAsync(newClip.GetHashCode() + imageExtensions[0], CreationCollisionOption.ReplaceExisting);

                //BitmapImage bitmap = new BitmapImage();
                //bitmap.SetSource(x);

                
                //wBitmap.SetSource(x);

                

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(x);
                WriteableBitmap wBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                // Scale image to appropriate size 
                BitmapTransform transform = new BitmapTransform()
                {
                    ScaledWidth = Convert.ToUInt32(decoder.PixelWidth),
                    ScaledHeight = Convert.ToUInt32(decoder.PixelHeight)
                };
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8, // WriteableBitmap uses BGRA format 
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation, // This sample ignores Exif orientation 
                    ColorManagementMode.DoNotColorManage
                );

                // An array containing the decoded image data, which could be modified before being displayed 
                byte[] sourcePixels = pixelData.DetachPixelData();

                // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer 
                using (Stream stream = wBitmap.PixelBuffer.AsStream())
                {
                    await stream.WriteAsync(sourcePixels, 0, sourcePixels.Length);
                }

                await wBitmap.SaveToFile(imageFile, BitmapEncoder.JpegEncoderId);

                //var fs = await imageFile.OpenAsync(FileAccessMode.ReadWrite);
                //DataWriter writer = new DataWriter(fs.GetOutputStreamAt(0));
                //writer.WriteBytes(await x.ReadAsync());
                //await writer.StoreAsync();
                //writer.DetachStream();
                //await fs.FlushAsync();
 
                //StorageFile imgFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(newClip.GetHashCode() + imageExtensions[0], CreationCollisionOption.ReplaceExisting);
 

                //byte[] pixels = new byte[4 * bitmap.PixelWidth * bitmap.PixelHeight];

                //Stream pixelStream = wBitmap.PixelBuffer.AsStream();
                //pixelStream.Seek(0, SeekOrigin.Begin);
                //pixelStream.Write(pixels, 0, pixels.Length); 

                ////BitmapToWriteableBitmap(imgFile);
                //await Utilities.SaveToFile(wBitmap, imgFile, new Guid());
 
                ////using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite)) 
                ////{
                ////    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                ////    encoder.SetPixelData(
                ////        BitmapPixelFormat.Bgra8,
                ////        BitmapAlphaMode.Ignore,
                ////        (uint)bitmap.PixelWidth,
                ////        (uint)bitmap.PixelHeight, 
                ////        96d, 
                ////        96d,
                ////        bitmap.
                ////    );
 
                //////    await encoder.FlushAsync();
                //////} 
                ////if (bitmap != null)
                ////{
                ////    IRandomAccessStream stream = await bitmap.OpenAsync(FileAccessMode.Read);
                ////    bmp.SetSource(stream);
                ////    imageGrid.Source = bmp;
                ////}


                newClip.UserData.Add("thumb", imageFile.Path);
                Clips.Add(newClip);
                RaisePropertyChanged(Utilities.GetMemberName(() => Clips));
                MyUri = new Uri(imageFile.Path);
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
            //if (args.Files.Count > 0)
            //{
            //    await LoadFile(args.Files[0]);
            //}
            //else
            //{
            //    Debug.WriteLine("Operation cancelled.");
            //}

            IReadOnlyList<StorageFile> files = args.Files;
            if (files.Count > 0)
            {
                StringBuilder output = new StringBuilder("Picked files:\n");
                // Application now has read/write access to the picked file(s)
                foreach (StorageFile file in files)
                {
                    output.Append(file.Name + "\n");
                    await LoadFile(file);
                }
                Debug.WriteLine(output.ToString());
                MediaLoaded = true;


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