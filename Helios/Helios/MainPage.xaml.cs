using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;
using Helios.ViewModel;
using Windows.Media.Core;
using Windows.Media.Editing;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Helios
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        #region Properties (public)

        /// <summary>
        /// Gets the view's ViewModel.
        /// </summary>
        public MainViewModel Vm
        {
            get
            {
                return (MainViewModel)DataContext;
            }
        }

        #endregion


        #region Dependency Properties

        public MediaStreamSource MediaSource
        {
            get { return (MediaStreamSource)GetValue(MediaSourceProperty); }
            set { SetValue(MediaSourceProperty, value); }
        }

        public static readonly DependencyProperty MediaSourceProperty = DependencyProperty.Register(
            "MediaSource",
            typeof(MediaStreamSource),
            typeof(MainPage),
            new PropertyMetadata(null, MediaSourceChanged)
            );

        private static void MediaSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Code for dealing with your property changes
            if ((d as MainPage).Vm.MediaSource != null)
            {
                (d as MainPage).m_player.SetMediaStreamSource((d as MainPage).Vm.MediaSource);
            }
        }

        #endregion


        #region Constructors

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            // Bind the new dependency property to the source property of the view model
            // Had to bind this page's MediaSourceProperty to be notified of changes in the ViewModel's MediaSource property;
            // This was special for the MediaStreamSource, since there was no way to bind directly to a MediaStreamSource in the XAML
            Binding mSPBinding = new Binding();
            mSPBinding.Path = new PropertyPath(Utilities.GetMemberName(() => Vm.MediaSource));
            this.SetBinding(MainPage.MediaSourceProperty, mSPBinding);
        }

        #endregion


        #region Event handlers

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            // Temporarily deferring to PageBase to cover page load functionality  
            base.OnNavigatedTo(e);
        }

        #endregion

        
        #region Methods 

        protected override void LoadState(object state)
        {
            var casted = state as MainPageState;

            if (casted != null)
            {
                Vm.Load(casted.LastVisit);
            }
        }

        protected override object SaveState()
        {
            return new MainPageState
            {
                LastVisit = DateTime.Now
            };
        }

        #endregion
    }

    public class MainPageState
    {
        public DateTime LastVisit
        {
            get;
            set;
        }
    }
}
