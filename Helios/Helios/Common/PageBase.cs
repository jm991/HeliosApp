using GalaSoft.MvvmLight;
using System;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Helios.Common
{
    public abstract class PageBase : Page
    {
        private const string StateKey = "State";

        private readonly NavigationHelper _navigationHelper;

        public static PageBase Current;

        public NavigationHelper NavigationHelper
        {
            get
            {
                return _navigationHelper;
            }
        }

        protected PageBase()
        {
            // This is a static public property that allows downstream pages to get a handle to the PageBase instance
            // in order to call methods that are in this class.
            Current = this;

            _navigationHelper = new NavigationHelper(this);
            _navigationHelper.LoadState += NavigationHelperLoadState;
            _navigationHelper.SaveState += NavigationHelperSaveState;

            if (ViewModelBase.IsInDesignModeStatic)
            {
                // Populate values here for blend
            }
            else
            {
                HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            }
        }

        protected virtual void LoadState(object state)
        {
        }

        protected void NavigationHelperLoadState(object sender, LoadStateEventArgs e)
        {
            if (e.PageState != null
                && e.PageState.ContainsKey(StateKey))
            {
                LoadState(e.PageState[StateKey]);
            }
        }

        protected void NavigationHelperSaveState(object sender, SaveStateEventArgs e)
        {
            if (e.PageState == null)
            {
                throw new InvalidOperationException("PageState is null");
            }

            if (e.PageState.ContainsKey(StateKey))
            {
                e.PageState.Remove(StateKey);
            }

            var state = SaveState();

            if (state != null)
            {
                e.PageState.Add(StateKey, state);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);
        }

        protected virtual object SaveState()
        {
            return null;
        }

        // Use this instead of OnBackKeyPress(System.ComponentModel.CancelEventArgs e) in C# XAML app
        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (NavigationHelper.GoBackCommand.CanExecute(null))
            {
                e.Handled = true;
                NavigationHelper.GoBackCommand.Execute(null);
            }
        }
    }
}