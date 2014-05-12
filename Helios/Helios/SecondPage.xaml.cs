using Expression.Blend.SampleData.SampleDataSource;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Helios
{
    public sealed partial class SecondPage
    {
        private static readonly Random random = new Random();

        /// <summary>
        /// Gets a demonstration collection of strings. Since this is an ObservableCollection,
        /// changes will be synchronized between this collection and the ReorderListBox that
        /// displays it.
        /// </summary>
        public ObservableCollection<string> DemoData
        {
            get;
            private set;
        }

        public SecondPage()
        {
            InitializeComponent();
            this.Loaded += SecondPage_Loaded;

            // Create some demonstration data.
            this.DemoData = SecondPage.CreateDemoList(30);

            // Set the listbox data context (and the bound ItemsSource) to the demo data collection.
            this.reorderListBox.DataContext = this.DemoData;

            //// create a new instance of store data
            //storeData = new StoreData();
            //// set the source of the GridView to be the sample data
            //ItemGridView.ItemsSource = storeData.Collection;
        }

        void SecondPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Debug.WriteLine("W: " + this.ActualWidth + " H: " + this.ActualHeight);
        }


        #region Demo list generation

        private static ObservableCollection<string> CreateDemoList(int count)
        {
            ObservableCollection<string> list = new ObservableCollection<string>();
            for (int i = 0; i < count; i++)
            {
                string item = SecondPage.CreateDemoListItem();
                list.Add(item);
            }
            return list;
        }

        private static string CreateDemoListItem()
        {
            char letter1, letter2, letter3;
            letter1 = (char)('A' + SecondPage.random.Next(26));
            do
            {
                letter2 = (char)('A' + SecondPage.random.Next(26));
            } while (letter2 == letter1);
            do
            {
                letter3 = (char)('A' + SecondPage.random.Next(26));
            } while (letter3 == letter1);

            // Uncomment for testing with list items of different heights.
            ////if (MainPage.random.Next(5) == 0) return "list item " + letter1 + letter2 + letter3 + "\n is two lines";

            return "list item " + letter1 + letter2 + letter3;
        }

        #endregion


        /*#region Sortable list (TODO: cleanup)

        
        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        // MainPage rootPage = MainPage.Current;

        // holds the sample data - See SampleData folder
        StoreData storeData = null;


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        /// <summary>
        /// We will visualize the data item in asynchronously in multiple phases for improved panning user experience 
        /// of large lists.  In this sample scneario, we will visualize different parts of the data item
        /// in the following order:
        /// 
        ///     1) Placeholders (visualized synchronously - Phase 0)
        ///     2) Tilte (visualized asynchronously - Phase 1)
        ///     3) Category and Image (visualized asynchronously - Phase 2)
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void ItemGridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            ItemViewer iv = args.ItemContainer.ContentTemplateRoot as ItemViewer;

            if (args.InRecycleQueue == true)
            {
                iv.ClearData();
            }
            else if (args.Phase == 0)
            {
                iv.ShowPlaceholder(args.Item as Item);

                // Register for async callback to visualize Title asynchronously
                args.RegisterUpdateCallback(ContainerContentChangingDelegate);
            }
            else if (args.Phase == 1)
            {
                iv.ShowTitle();
                args.RegisterUpdateCallback(ContainerContentChangingDelegate);
            }
            else if (args.Phase == 2)
            {
                iv.ShowCategory();
                iv.ShowImage();
            }

            // For imporved performance, set Handled to true since app is visualizing the data item
            args.Handled = true;
        }

        /// <summary>
        /// Managing delegate creation to ensure we instantiate a single instance for 
        /// optimal performance. 
        /// </summary>
        private TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> ContainerContentChangingDelegate
        {
            get
            {
                if (_delegate == null)
                {
                    _delegate = new TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs>(ItemGridView_ContainerContentChanging);
                }
                return _delegate;
            }
        }
        private TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> _delegate;

        #endregion*/
    }
}