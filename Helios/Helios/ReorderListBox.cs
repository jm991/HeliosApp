﻿using Helios.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace Helios
{
    /// <summary>
    /// Extends ListBox to enable drag-and-drop reorder within the list.
    /// </summary>
    [TemplatePart(Name = ReorderListBox.ScrollViewerPart, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = ReorderListBox.DragIndicatorPart, Type = typeof(Image))]
    [TemplatePart(Name = ReorderListBox.DragInterceptorPart, Type = typeof(Canvas))]
    [TemplatePart(Name = ReorderListBox.RearrangeCanvasPart, Type = typeof(Canvas))]
    public class ReorderListBox : ListBox
    {
        #region Template part name constants

        public const string ScrollViewerPart = "ScrollViewer";
        public const string DragIndicatorPart = "DragIndicator";
        public const string DragInterceptorPart = "DragInterceptor";
        public const string RearrangeCanvasPart = "RearrangeCanvas";
        public const string ItemsPanelPart = "ItemsPanel";

        #endregion


        #region String path names (private)

        private const string ScrollViewerScrollingVisualState = "Scrolling";
        private const string ScrollViewerNotScrollingVisualState = "NotScrolling";

        private const string IsReorderEnabledPropertyName = "IsReorderEnabled";

        #endregion 


        #region Private fields

        /// <summary>
        /// Half pixel bias to settle any double == issues when trying to determine if the dragged item is being dropped
        /// before or after the ReorderListBox item it's currently above (bias towards before)
        /// </summary>
        private const double EPSILON = -0.5d;
        private bool drawn = false;
        private double dragScrollDelta;
        private Panel itemsPanel;
        private ScrollViewer scrollViewer;
        // TODO: made public
        /// <summary>
        /// The Canvas overlaid on the items that intercepts drag requests
        /// </summary>
        public Canvas dragInterceptor;
        /// <summary>
        /// Image used as proxy for dragging item - more implementation agnostic than copying/instantiating a copy of the actual dragged control
        /// </summary>
        private Image dragIndicator;
        /// <summary>
        /// Contents of dragItemContainer (unknown type, changes based on use of the control)
        /// </summary>
        private object dragItem;
        /// <summary>
        /// Stored reference to currently dragged ReorderListBoxItem
        /// </summary>
        private ReorderListBoxItem dragItemContainer;
        private bool isDragItemSelected;
        private Rect dragInterceptorRect;
        private int dropTargetIndex;
        private Canvas rearrangeCanvas;
        private Queue<KeyValuePair<Action, Duration>> rearrangeQueue;

        #endregion


        #region Constructor

        /// <summary>
        /// Creates a new ReorderListBox and sets the default style key.
        /// The style key is used to locate the control template in Generic.xaml.
        /// </summary>
        public ReorderListBox()
        {
            this.DefaultStyleKey = typeof(ReorderListBox);
        }

        #endregion 


        #region IsReorderEnabled DependencyProperty

        public static readonly DependencyProperty IsReorderEnabledProperty = DependencyProperty.Register(
            ReorderListBox.IsReorderEnabledPropertyName, typeof(bool), typeof(ReorderListBox),
            new PropertyMetadata(false, OnIsReorderEnabledChanged));
        // TODO: changed from (d, e) => ((ReorderListBox)d).OnIsReorderEnabledChanged(e))); to OnIsReorderEnabledChanged

        /// <summary>
        /// Gets or sets a value indicating whether reordering is enabled in the listbox.
        /// This also controls the visibility of the reorder drag-handle of each listbox item.
        /// </summary>
        public bool IsReorderEnabled
        {
            get
            {
                return (bool)this.GetValue(ReorderListBox.IsReorderEnabledProperty);
            }
            set
            {
                this.SetValue(ReorderListBox.IsReorderEnabledProperty, value);
            }
        }

        // TODO: do we need this style?: private static void BLANKPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        // TODO: changed protected void OnIsReorderEnabledChanged(DependencyPropertyChangedEventArgs e)
        private static void OnIsReorderEnabledChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is ReorderListBox)
            {
                ReorderListBox oLB = o as ReorderListBox;
                if (oLB.dragInterceptor != null)
                {
                    oLB.dragInterceptor.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
                }

                oLB.InvalidateArrange();
            }
        }

        #endregion


        #region AutoScrollMargin DependencyProperty

        public static readonly DependencyProperty AutoScrollMarginProperty = DependencyProperty.Register(
            "AutoScrollMargin", typeof(int), typeof(ReorderListBox), new PropertyMetadata(32));

        /// <summary>
        /// Gets or sets the size of the region at the top and bottom of the list where dragging will
        /// cause the list to automatically scroll.
        /// </summary>
        public double AutoScrollMargin
        {
            get
            {
                return (int)this.GetValue(ReorderListBox.AutoScrollMarginProperty);
            }
            set
            {
                this.SetValue(ReorderListBox.AutoScrollMarginProperty, value);
            }
        }

        #endregion


        #region ItemsControl overrides

        /// <summary>
        /// Applies the control template, gets required template parts, and hooks up the drag events.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.scrollViewer = (ScrollViewer)this.GetTemplateChild(ReorderListBox.ScrollViewerPart);
            this.dragInterceptor = this.GetTemplateChild(ReorderListBox.DragInterceptorPart) as Canvas;
            this.dragIndicator = this.GetTemplateChild(ReorderListBox.DragIndicatorPart) as Image;
            this.rearrangeCanvas = this.GetTemplateChild(ReorderListBox.RearrangeCanvasPart) as Canvas;

            if (this.scrollViewer != null && this.dragInterceptor != null && this.dragIndicator != null)
            {
                this.dragInterceptor.Visibility = this.IsReorderEnabled ? Visibility.Visible : Visibility.Collapsed;

                this.dragInterceptor.ManipulationStarted += this.dragInterceptor_ManipulationStarted;
                this.dragInterceptor.ManipulationDelta += this.dragInterceptor_ManipulationDelta;
                this.dragInterceptor.ManipulationCompleted += this.dragInterceptor_ManipulationCompleted;

                this.scrollViewer.ViewChanged += scrollViewer_ViewChanged;
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ReorderListBoxItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ReorderListBoxItem;
        }

        /// <summary>
        /// Ensures that a possibly-recycled item container (ReorderListBoxItem) is ready to display a list item.
        /// </summary>
        protected override async void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            ReorderListBoxItem itemContainer = (ReorderListBoxItem)element;
            itemContainer.ApplyTemplate();  // Loads visual states.

            // Set this state before binding to avoid showing the visual transition in this case.
            string reorderState = this.IsReorderEnabled ?
                ReorderListBoxItem.ReorderEnabledState : ReorderListBoxItem.ReorderDisabledState;
            VisualStateManager.GoToState(itemContainer, reorderState, false);

            // Porting issue
            // Fixed by adding the "Source = this" parameter to the binding so that all ReorderListBoxItems' .IsReorderEnabled DPs are bound to the parent ReorderListBox
            // TODO: trying to fix this error:
            //Error: BindingExpression path error: 'IsReorderEnabled' property not found on 'Windows.Foundation.IReference`1<String>'. BindingExpression: Path='IsReorderEnabled' DataItem='Windows.Foundation.IReference`1<String>'; target element is 'Helios.ReorderListBoxItem' (Name='null'); target property is 'IsReorderEnabled' (type 'Boolean')
            //Binding iREPBinding = new Binding();
            //iREPBinding.Path = new PropertyPath(ReorderListBox.IsReorderEnabledPropertyName);
            //itemContainer.SetBinding(ReorderListBoxItem.IsReorderEnabledProperty, iREPBinding);
            itemContainer.SetBinding(ReorderListBoxItem.IsReorderEnabledProperty, new Binding
                {
                    Path = new PropertyPath(ReorderListBox.IsReorderEnabledPropertyName), 
                    Source = this
                });

            // TODO: removed this line and replaced with the above (no ability to pass Path directly to Binding in Jupiter)
            //itemContainer.SetBinding(ReorderListBoxItem.IsReorderEnabledProperty,
            //    new Binding(ReorderListBox.IsReorderEnabledPropertyName) { Source = this });

            if (item == this.dragItem)
            {
                itemContainer.IsSelected = this.isDragItemSelected;
                VisualStateManager.GoToState(itemContainer, ReorderListBoxItem.DraggingState, false);

                if (this.dropTargetIndex >= 0)
                {
                    // The item's dragIndicator is currently being moved, so the item itself is hidden. 
                    itemContainer.Visibility = Visibility.Collapsed;
                    this.dragItemContainer = itemContainer;
                }
                else
                {
                    itemContainer.Opacity = 0;
                    // TODO: replaced this line:
                    // this.Dispatcher.BeginInvoke(() => this.AnimateDrop(itemContainer));
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        new CoreDispatcherPriority(), () => this.AnimateDrop(itemContainer));
                }
            }
            else
            {
                VisualStateManager.GoToState(itemContainer, ReorderListBoxItem.NotDraggingState, false);
            }
        }

        /// <summary>
        /// Called when an item container (ReorderListBoxItem) is being removed from the list panel.
        /// This may be because the item was removed from the list or because the item is now outside
        /// the virtualization region (because ListBox uses a VirtualizingStackPanel as its items panel).
        /// </summary>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            ReorderListBoxItem itemContainer = (ReorderListBoxItem)element;
            if (itemContainer == this.dragItemContainer)
            {
                this.dragItemContainer.Visibility = Visibility.Visible;
                // TODO: enable this again; turning off for debuggin
                // this.dragItemContainer = null;
            }
        }

        #endregion


        #region Drag & drop reorder

        /// <summary>
        /// Called when the user presses down on the transparent drag-interceptor. Identifies the targed
        /// drag handle and list item and prepares for a drag operation.
        /// </summary>
        private void dragInterceptor_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (this.dragItem != null)
            {
                return;
            }

            // Access the Panel containing all of the draggable ReorderListBoxItems
            if (this.itemsPanel == null)
            {
                ItemsPresenter scrollItemsPresenter = (ItemsPresenter)this.scrollViewer.Content;
                this.itemsPanel = Utilities.FindChild<StackPanel>(Window.Current.Content, ItemsPanelPart);
                //this.itemsPanel = (Panel)VisualTreeHelper.GetChild(scrollItemsPresenter, 0);
            }

            // Figure out the Point where the user put their finger down
            GeneralTransform interceptorTransform = this.dragInterceptor.TransformToVisual(Window.Current.Content);
            // TODO: replace this line -
            // Point targetPoint = interceptorTransform.Transform(e.ManipulationOrigin);
            Point targetPoint = interceptorTransform.TransformPoint(e.Position);
            targetPoint = ReorderListBox.GetHostCoordinates(targetPoint);

            // Get reference to all items at the Point where the finger is down
            List<UIElement> targetElements = VisualTreeHelper.FindElementsInHostCoordinates(
                targetPoint, this.itemsPanel).ToList();
            // Get the first eligible ReorderListBoxItem
            ReorderListBoxItem targetItemContainer = targetElements.OfType<ReorderListBoxItem>().FirstOrDefault();
            if (targetItemContainer != null && targetElements.Contains(targetItemContainer.DragHandle))
            {
                // Transition to the VisualState for dragging (default is light grey underlay with lower opacity)
                VisualStateManager.GoToState(targetItemContainer, ReorderListBoxItem.DraggingState, true);

                // Position and resize the proxy image "dragIndicator"
                GeneralTransform targetItemTransform = targetItemContainer.TransformToVisual(this.dragInterceptor);
                Point targetItemOrigin = targetItemTransform.TransformPoint(new Point(0, 0));
                Canvas.SetLeft(this.dragIndicator, targetItemOrigin.X);
                Canvas.SetTop(this.dragIndicator, targetItemOrigin.Y);
                this.dragIndicator.Width = targetItemContainer.RenderSize.Width;
                this.dragIndicator.Height = targetItemContainer.RenderSize.Height;

                // Store references to the object being dragged
                this.dragItemContainer = targetItemContainer;
                this.dragItem = this.dragItemContainer.Content;
                this.isDragItemSelected = this.dragItemContainer.IsSelected;

                this.dragInterceptorRect = interceptorTransform.TransformBounds(
                    new Rect(new Point(0, 0), this.dragInterceptor.RenderSize));

                this.dropTargetIndex = -1;
            }
        }

        /// <summary>
        /// Called when the scrollViewer completes a view change (code initated scroll).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void scrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // If the drag delta hasn't changed, call the scrolling method again in a crude loop
            // This was needed for the case where the user stops moving their finger (ManipulationDelta isn't called any longer), but the scrolling
            // still needs to occur (within AutoScroll margins)
            if (this.dragScrollDelta != 0)
            {
                DragScroll(dragScrollDelta);
            }
        }

        /// <summary>
        /// Called when the user drags on (or from) the transparent drag-interceptor.
        /// Moves the item (actually a rendered snapshot of the item) according to the drag delta.
        /// </summary>
        private async void dragInterceptor_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (this.Items.Count <= 1 || this.dragItem == null)
            {
                return;
            }
            if (this.dropTargetIndex == -1)
            {
                if (this.dragItemContainer == null)
                {
                    return;
                }

                // When the drag actually starts, swap out the item for the drag-indicator image of the item.
                // This is necessary because the item itself may be removed from the virtualizing panel
                // if the drag causes a scroll of considerable distance.
                Size dragItemSize = this.dragItemContainer.RenderSize;
                // TODO: removed - 
                //WriteableBitmap writeableBitmap = new WriteableBitmap(
                //    (int)dragItemSize.Width, (int)dragItemSize.Height);

                // Swap states to force the transition to complete.
                VisualStateManager.GoToState(this.dragItemContainer, ReorderListBoxItem.NotDraggingState, false);
                VisualStateManager.GoToState(this.dragItemContainer, ReorderListBoxItem.DraggingState, false);
                // TODO: stopped rendering every time on drag and replaced -
                // writeableBitmap.Render(this.dragItemContainer, null);

                // Render the Image of the dragged control asychronously
                if (this.dragIndicator.Source == null)
                {
                    //Task<RenderTargetBitmap> renderTask = RenderImageSource(this.dragItemContainer, dragItemSize);

                    //// TODO: removed
                    //// writeableBitmap.Invalidate();
                    //// this.dragIndicator.Source = writeableBitmap;
                    //this.dragIndicator.Source = renderTask.Result;

                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                    try
                    {
                        if (dragItemContainer != null && dragItemContainer.Visibility == Visibility.Visible && !drawn)
                        {
                            drawn = true;
                            await renderTargetBitmap.RenderAsync(this.dragItemContainer, (int)dragItemSize.Width, (int)dragItemSize.Height);
                            this.dragIndicator.Source = renderTargetBitmap;


                            // Don't show transitions (second param in UpdateDropTargets) so that the drop targets on either side of the dragged item
                            // are instantly expanded (if they animated, it would be jarring)                
                            if (this.itemsPanel.Children.IndexOf(this.dragItemContainer) < this.itemsPanel.Children.Count - 1)
                            {
                                // Special casing for if the dragged item is the last in the list to grab the item behind the dragged one and expanding its DropAfterSpace
                                this.UpdateDropTarget(Canvas.GetTop(this.dragIndicator) + this.dragIndicator.Height + 1, false);
                            }
                            else
                            {
                                // Normal case of grabbing the item in front of the dragged one and expanding its DropBeforeSpace
                                this.UpdateDropTarget(Canvas.GetTop(this.dragIndicator) - 1, false);
                            }
                        }
                    }
                    catch (ArgumentException error)
                    {
                        Debug.WriteLine("Something went wrong with the async bitmap rendering. " + error.ToString());
                    }

                    // Since this is all async code, make sure that we haven't already completed the manipulation (and thusly null'd dragItemContainer) by the time the RenderAsync completed
                    if (this.dragItemContainer == null)
                    {
                        return;
                    }
                }
            }

            // TODO: moved all the code into check for the dragIndicator Image being null, since many values in the next half of this method
            // depend on the dragIndicator Image being loaded
            // It was essentially a race condition where ManipulationDelta kept getting called and this code was accessed before the Image was rendered            
            if (this.dragIndicator.Source != null)
            {
                // Collapsing dragItemContainer (the source for the Image being created) before RenderAsync completed would have been 
                // meant that the method was looking for pixels to render that were hidden - hence, why it's moved to this part of the method
                this.dragIndicator.Visibility = Visibility.Visible;
                this.dragItemContainer.Visibility = Visibility.Collapsed;

                double dragItemHeight = this.dragIndicator.Height;

                TranslateTransform translation = (TranslateTransform)this.dragIndicator.RenderTransform;
                double top = Canvas.GetTop(this.dragIndicator);

                // Limit the translation to keep the item within the list area.
                // Use different targeting for the top and bottom edges to allow taller items to
                // move before or after shorter items at the edges.
                double y = top + e.Cumulative.Translation.Y;
                if (y < 0)
                {
                    // Special case for dragging to the beginning of the list
                    y = 0;
                    this.UpdateDropTarget(y, true);
                }
                else if (y >= this.dragInterceptorRect.Height - dragItemHeight)
                {
                    // Special case for dragging to the end of the list
                    y = this.dragInterceptorRect.Height - dragItemHeight;
                    this.UpdateDropTarget(this.dragInterceptorRect.Height - 1, true);
                }
                else
                {
                    // Normal case of dragging between two items
                    this.UpdateDropTarget(y + dragItemHeight / 2, true);
                }

                translation.Y = y - top;

                // Check if we're within the margin where auto-scroll needs to happen.
                bool scrolling = (this.dragScrollDelta != 0);
                double autoScrollMargin = this.AutoScrollMargin;
                if (autoScrollMargin > 0 && y < autoScrollMargin)
                {
                    this.dragScrollDelta = y - autoScrollMargin;
                    // Set direction
                    this.DragScroll(dragScrollDelta);
                    if (!scrolling)
                    {
                        VisualStateManager.GoToState(this.scrollViewer, ReorderListBox.ScrollViewerScrollingVisualState, true);

                        // TODO: replaced this line:
                        // this.Dispatcher.BeginInvoke(() => this.DragScroll());
                        //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        //    new CoreDispatcherPriority(), () => this.DragScroll(dragScrollDelta));
                        //this.DragScroll(dragScrollDelta);
                        return;
                    }
                }
                else if (autoScrollMargin > 0 && y + dragItemHeight > this.dragInterceptorRect.Height - autoScrollMargin)
                {
                    this.dragScrollDelta = (y + dragItemHeight - (this.dragInterceptorRect.Height - autoScrollMargin));
                    this.DragScroll(dragScrollDelta);
                    if (!scrolling)
                    {
                        VisualStateManager.GoToState(this.scrollViewer, ReorderListBox.ScrollViewerScrollingVisualState, true);

                        // TODO: replaced this line:
                        // this.Dispatcher.BeginInvoke(() => this.DragScroll());
                        //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        //    new CoreDispatcherPriority(), () => this.DragScroll(dragScrollDelta));
                        //this.DragScroll(dragScrollDelta);
                        return;
                    }
                }
                else
                {
                    // We're not within the auto-scroll margin. This ensures any current scrolling is stopped.
                    this.dragScrollDelta = 0;
                }
            }
        }

        /// <summary>
        /// Called when the user releases a drag. Moves the item within the source list and then resets everything.
        /// </summary>
        private void dragInterceptor_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (this.dragItem == null)
            {
                return;
            }

            if (this.dropTargetIndex >= 0)
            {
                this.MoveItem(this.dragItem, this.dropTargetIndex);
            }

            if (this.dragItemContainer != null)
            {
                this.dragItemContainer.Visibility = Visibility.Visible;
                this.dragItemContainer.Opacity = 0;
                this.AnimateDrop(this.dragItemContainer);
                this.dragItemContainer = null;
            }

            this.dragScrollDelta = 0;
            this.dropTargetIndex = -1;
            this.ClearDropTarget();
        }

        /// <summary>
        /// Slides the drag indicator (item snapshot) to the location of the dropped item,
        /// then performs the visibility swap and removes the dragging visual state.
        /// </summary>
        private void AnimateDrop(ReorderListBoxItem itemContainer)
        {
            GeneralTransform itemTransform = itemContainer.TransformToVisual(this.dragInterceptor);
            Rect itemRect = itemTransform.TransformBounds(new Rect(new Point(0, 0), itemContainer.RenderSize));
            double delta = Math.Abs(itemRect.Y - Canvas.GetTop(this.dragIndicator) -
                ((TranslateTransform)this.dragIndicator.RenderTransform).Y);
            // Added the && case because the itemContainer was being null'd somewhere in the code before this finished
            // TODO: find the place where itemContainer is being set to null
            if (delta > 0)// && itemContainer != null)
            {
                // TODO: reenable the time scaling
                // Adjust the duration based on the distance, so the speed will be constant.
                //TimeSpan duration = TimeSpan.FromSeconds(0.25 * delta / itemRect.Height);
                TimeSpan duration = TimeSpan.FromMilliseconds(250);

                Storyboard dropStoryboard = new Storyboard();
                DoubleAnimation moveToDropAnimation = new DoubleAnimation();
                Storyboard.SetTarget(moveToDropAnimation, this.dragIndicator.RenderTransform);
                // TODO: changed 
                // Storyboard.SetTargetProperty(moveToDropAnimation, new PropertyPath(TranslateTransform.YProperty));
                Storyboard.SetTargetProperty(moveToDropAnimation, TranslateTransform.YProperty.ToString());
                moveToDropAnimation.To = itemRect.Y - Canvas.GetTop(this.dragIndicator);
                moveToDropAnimation.Duration = duration;
                dropStoryboard.Children.Add(moveToDropAnimation);

                // TODO: put at the end of the storyboard again
                //dropStoryboard.Completed += delegate
                //{
                    this.dragItem = null;
                    itemContainer.Opacity = 1;
                    this.dragIndicator.Visibility = Visibility.Collapsed;
                    this.dragIndicator.Source = null;
                    ((TranslateTransform)this.dragIndicator.RenderTransform).Y = 0;
                    VisualStateManager.GoToState(itemContainer, ReorderListBoxItem.NotDraggingState, true);

                //};
                // TODO: renable late
                //dropStoryboard.Begin();
            }
            else
            {
                // There was no need for an animation, so do the visibility swap right now.
                this.dragItem = null;
                itemContainer.Opacity = 1;
                this.dragIndicator.Visibility = Visibility.Collapsed;
                this.dragIndicator.Source = null;
                VisualStateManager.GoToState(itemContainer, ReorderListBoxItem.NotDraggingState, true);
            }

            // reset ability to call RenderAsync
            drawn = false;
        }

        /// <summary>
        /// Automatically scrolls for as long as the drag is held within the margin.
        /// The speed of the scroll is adjusted based on the depth into the margin.
        /// </summary>
        /// 
        // TODO: added a parameter since the dragScrollDelta was 0 when this was called; something weird with the async programming
        private async void DragScroll(double newDragScrollDelta)
        {
            if (newDragScrollDelta != 0)
            {
                double scrollRatio = this.scrollViewer.ViewportHeight / this.scrollViewer.RenderSize.Height;
                double adjustedDelta = newDragScrollDelta * scrollRatio;
                double newOffset = this.scrollViewer.VerticalOffset + adjustedDelta;
                // TODO: changed all of these lines
                // this.scrollViewer.ScrollToVerticalOffset(newOffset);
                await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { bool offset = this.scrollViewer.ChangeView(null, newOffset, null); });


                // TODO: replaced this line:
                // this.Dispatcher.BeginInvoke(() => this.DragScroll());
                // Keep calling the scroll method until it's no longer valid (like a while loop)
                //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                //    new CoreDispatcherPriority(), () => this.DragScroll(this.dragScrollDelta));
                //DragScroll(this.dragScrollDelta);

                // Sorts the dragIndicator Image to the end of the scrollable region so that it shows 
                double dragItemOffset = Canvas.GetTop(this.dragIndicator) +
                    ((TranslateTransform)this.dragIndicator.RenderTransform).Y +
                    this.dragIndicator.Height / 2;
                this.UpdateDropTarget(dragItemOffset, true);
            }
            else
            {
                VisualStateManager.GoToState(this.scrollViewer, ReorderListBox.ScrollViewerNotScrollingVisualState, true);
            }
        }

        /// <summary>
        /// Updates spacing (drop target indicators) surrounding the targeted region.
        /// </summary>
        /// <param name="dragItemOffset">Vertical offset into the items panel where the drag is currently targeting.</param>
        /// <param name="showTransition">True if the drop-indicator transitions should be shown.</param>
        private void UpdateDropTarget(double dragItemOffset, bool showTransition)
        {
            // Get a reference to the ReorderListBoxItem at the given offset using the same technique as in ManipulationDelta to
            // find the drag item
            Point dragPoint = ReorderListBox.GetHostCoordinates(
                new Point(this.dragInterceptorRect.Left, this.dragInterceptorRect.Top + dragItemOffset));
            IEnumerable<UIElement> targetElements = VisualTreeHelper.FindElementsInHostCoordinates(dragPoint, this.itemsPanel);
            ReorderListBoxItem targetItem = targetElements.OfType<ReorderListBoxItem>().FirstOrDefault();
            if (targetItem != null)
            {
                GeneralTransform targetTransform = targetItem.DragHandle.TransformToVisual(this.dragInterceptor);
                Rect targetRect = targetTransform.TransformBounds(new Rect(new Point(0, 0), targetItem.DragHandle.RenderSize));
                double targetCenter = (targetRect.Top + targetRect.Bottom) / 2;

                int targetIndex = this.itemsPanel.Children.IndexOf(targetItem);
                int childrenCount = this.itemsPanel.Children.Count;
                // Experiencing some double comparison issues when UpdateDropTarget's dragItemOffset was set to 0 for the 
                // case of dragging to the beginning/front of the list
                // So in the dragging to the "beginning" case, an Epsilon value is added to err on the side of !after
                bool after = dragItemOffset + (targetIndex == 0 ? EPSILON : 0) > targetCenter;
                ReorderListBoxItem indicatorItem = null;
                if (!after && targetIndex > 0)
                {
                    ReorderListBoxItem previousItem = (ReorderListBoxItem)this.itemsPanel.Children[targetIndex - 1];
                    if (previousItem.Tag as string == ReorderListBoxItem.DropAfterIndicatorState)
                    {
                        indicatorItem = previousItem;
                    }
                }
                else if (after && targetIndex < childrenCount - 1)
                {
                    ReorderListBoxItem nextItem = (ReorderListBoxItem)this.itemsPanel.Children[targetIndex + 1];
                    if (nextItem.Tag as string == ReorderListBoxItem.DropBeforeIndicatorState)
                    {
                        indicatorItem = nextItem;
                    }
                }

                if (indicatorItem == null)
                {
                    targetItem.DropIndicatorHeight = this.dragIndicator.Height;
                    string dropIndicatorState = after ?
                        ReorderListBoxItem.DropAfterIndicatorState : ReorderListBoxItem.DropBeforeIndicatorState;
                    VisualStateManager.GoToState(targetItem, dropIndicatorState, showTransition);
                    targetItem.Tag = dropIndicatorState;
                    indicatorItem = targetItem;
                }

                // Animate closing the drag state of nearby items (+-5 from the drop index)
                // Allows for a flowing effect while dragging
                for (int i = targetIndex - 5; i <= targetIndex + 5; i++)
                {
                    if (i >= 0 && i < childrenCount)
                    {
                        ReorderListBoxItem nearbyItem = (ReorderListBoxItem)this.itemsPanel.Children[i];
                        if (nearbyItem != indicatorItem)
                        {
                            VisualStateManager.GoToState(nearbyItem, ReorderListBoxItem.NoDropIndicatorState, showTransition);
                            nearbyItem.Tag = ReorderListBoxItem.NoDropIndicatorState;
                        }
                    }
                }

                this.UpdateDropTargetIndex(targetItem, after);
            }
        }

        /// <summary>
        /// Updates the targeted index -- that is the index where the item will be moved to if dropped at this point.
        /// </summary>
        private void UpdateDropTargetIndex(ReorderListBoxItem targetItemContainer, bool after)
        {
            int dragItemIndex = this.Items.IndexOf(this.dragItem);
            int targetItemIndex = this.Items.IndexOf(targetItemContainer.Content);

            int newDropTargetIndex;
            if (targetItemIndex == dragItemIndex)
            {
                newDropTargetIndex = dragItemIndex;
            }
            else
            {
                newDropTargetIndex = targetItemIndex + (after ? 1 : 0) - (targetItemIndex >= dragItemIndex ? 1 : 0);
            }

            if (newDropTargetIndex != this.dropTargetIndex)
            {
                this.dropTargetIndex = newDropTargetIndex;
            }
        }

        /// <summary>
        /// Hides any drop-indicators that are currently visible.
        /// </summary>
        private void ClearDropTarget()
        {
            foreach (ReorderListBoxItem itemContainer in this.itemsPanel.Children)
            {
                VisualStateManager.GoToState(itemContainer, ReorderListBoxItem.NoDropIndicatorState, false);
                itemContainer.Tag = null;
            }
        }

        /// <summary>
        /// Moves an item to a specified index in the source list.
        /// </summary>
        private async void /*bool*/ MoveItem(object item, int toIndex)
        {
            object itemsSource = this.ItemsSource;

            IList sourceList = itemsSource as IList;
            if (!(sourceList is INotifyCollectionChanged))
            {
                // If the source does not implement INotifyCollectionChanged, then there's no point in
                // changing the source because changes to it will not be synchronized with the list items.
                // So, just change the ListBox's view of the items.
                // TODO: changed from -
                // sourceList = this.Items;
                sourceList = this.Items.ToList();
            }

            int fromIndex = sourceList.IndexOf(item);
            if (fromIndex != toIndex)
            {
                double scrollOffset = this.scrollViewer.VerticalOffset;

                sourceList.RemoveAt(fromIndex);
                sourceList.Insert(toIndex, item);

                if (fromIndex <= scrollOffset && toIndex > scrollOffset)
                {
                    // Correct the scroll offset for the removed item so that the list doesn't appear to jump.
                    await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => { bool offset = this.scrollViewer.ChangeView(null, scrollOffset - 1, null); });
                }
                //return true;
            }
            else
            {
                //return false;
            }
        }

        #endregion


        #region View range detection

        /// <summary>
        /// Gets the indices of the first and last items in the view based on the current scroll position.
        /// </summary>
        /// <param name="includePartial">True to include items that are partially obscured at the top and bottom,
        /// false to include only items that are completely in view.</param>
        /// <param name="firstIndex">Returns the index of the first item in view (or -1 if there are no items).</param>
        /// <param name="lastIndex">Returns the index of the last item in view (or -1 if there are no items).</param>
        public void GetViewIndexRange(bool includePartial, out int firstIndex, out int lastIndex)
        {
            if (this.Items.Count > 0)
            {
                firstIndex = 0;
                lastIndex = this.Items.Count - 1;

                if (this.scrollViewer != null && this.Items.Count > 1)
                {
                    Thickness scrollViewerPadding = new Thickness(
                        this.scrollViewer.BorderThickness.Left + this.scrollViewer.Padding.Left,
                        this.scrollViewer.BorderThickness.Top + this.scrollViewer.Padding.Top,
                        this.scrollViewer.BorderThickness.Right + this.scrollViewer.Padding.Right,
                        this.scrollViewer.BorderThickness.Bottom + this.scrollViewer.Padding.Bottom);

                    GeneralTransform scrollViewerTransform = this.scrollViewer.TransformToVisual(
                        Window.Current.Content);
                    Rect scrollViewerRect = scrollViewerTransform.TransformBounds(
                        new Rect(new Point(0, 0), this.scrollViewer.RenderSize));

                    Point topPoint = ReorderListBox.GetHostCoordinates(new Point(
                        scrollViewerRect.Left + scrollViewerPadding.Left,
                        scrollViewerRect.Top + scrollViewerPadding.Top));
                    IEnumerable<UIElement> topElements = VisualTreeHelper.FindElementsInHostCoordinates(
                        topPoint, this.scrollViewer);
                    ReorderListBoxItem topItem = topElements.OfType<ReorderListBoxItem>().FirstOrDefault();
                    if (topItem != null)
                    {
                        GeneralTransform itemTransform = topItem.TransformToVisual(Window.Current.Content);
                        Rect itemRect = itemTransform.TransformBounds(new Rect(new Point(0, 0), topItem.RenderSize));

                        // TODO: replaced all of these
                        // firstIndex = this.ItemContainerGenerator.IndexFromContainer(topItem);
                        firstIndex = this.IndexFromContainer(topItem);
                        if (!includePartial && firstIndex < this.Items.Count - 1 &&
                            itemRect.Top < scrollViewerRect.Top && itemRect.Bottom < scrollViewerRect.Bottom)
                        {
                            firstIndex++;
                        }
                    }

                    Point bottomPoint = ReorderListBox.GetHostCoordinates(new Point(
                        scrollViewerRect.Left + scrollViewerPadding.Left,
                        scrollViewerRect.Bottom - scrollViewerPadding.Bottom - 1));
                    IEnumerable<UIElement> bottomElements = VisualTreeHelper.FindElementsInHostCoordinates(
                        bottomPoint, this.scrollViewer);
                    ReorderListBoxItem bottomItem = bottomElements.OfType<ReorderListBoxItem>().FirstOrDefault();
                    if (bottomItem != null)
                    {
                        GeneralTransform itemTransform = bottomItem.TransformToVisual(Window.Current.Content);
                        Rect itemRect = itemTransform.TransformBounds(
                            new Rect(new Point(0, 0), bottomItem.RenderSize));

                        lastIndex = this.IndexFromContainer(bottomItem);
                        if (!includePartial && lastIndex > firstIndex &&
                            itemRect.Bottom > scrollViewerRect.Bottom && itemRect.Top > scrollViewerRect.Top)
                        {
                            lastIndex--;
                        }
                    }
                }
            }
            else
            {
                firstIndex = -1;
                lastIndex = -1;
            }
        }

        #endregion


        #region Rearrange

        /// <summary>
        /// Private helper class for keeping track of each item involved in a rearrange.
        /// </summary>
        private class RearrangeItemInfo
        {
            public object Item = null;
            public int FromIndex = -1;
            public int ToIndex = -1;
            public double FromY = Double.NaN;
            public double ToY = Double.NaN;
            public double Height = Double.NaN;
        }

        /// <summary>
        /// Animates movements, insertions, or deletions in the list. 
        /// </summary>
        /// <param name="animationDuration">Duration of the animation.</param>
        /// <param name="rearrangeAction">Performs the actual rearrange on the list source.</param>
        /// <remarks>
        /// The animations are as follows:
        ///   - Inserted items fade in while later items slide down to make space.
        ///   - Removed items fade out while later items slide up to close the gap.
        ///   - Moved items slide from their previous location to their new location.
        ///   - Moved items which move out of or in to the visible area also fade out / fade in while sliding.
        /// <para>
        /// The rearrange action callback is called in the middle of the rearrange process. That
        /// callback may make any number of changes to the list source, in any order. After the rearrange
        /// action callback returns, the net result of all changes will be detected and included in a dynamically
        /// generated rearrange animation.
        /// </para><para>
        /// Multiple calls to this method in quick succession will be automatically queued up and executed in turn
        /// to avoid any possibility of conflicts. (If simultaneous rearrange animations are desired, use a single
        /// call to AnimateRearrange with a rearrange action callback that does both operations.)
        /// </para>
        /// </remarks>
        public async void AnimateRearrange(Duration animationDuration, Action rearrangeAction)
        {
            if (rearrangeAction == null)
            {
                throw new ArgumentNullException("rearrangeAction");
            }

            if (this.rearrangeCanvas == null)
            {
                throw new InvalidOperationException("ReorderListBox control template is missing " +
                    "a part required for rearrange: " + ReorderListBox.RearrangeCanvasPart);
            }

            if (this.rearrangeQueue == null)
            {
                this.rearrangeQueue = new Queue<KeyValuePair<Action, Duration>>();
                //this.scrollViewer.ChangeView(null, this.scrollViewer.VerticalOffset, null); // Stop scrolling.

                await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => { bool offset = this.scrollViewer.ChangeView(null, this.scrollViewer.VerticalOffset, null); });
                
                // TODO: replaced this line:
                // this.Dispatcher.BeginInvoke(() =>
                //    this.AnimateRearrangeInternal(rearrangeAction, animationDuration));
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    new CoreDispatcherPriority(), () => this.AnimateRearrangeInternal(rearrangeAction, animationDuration));
            }
            else
            {
                this.rearrangeQueue.Enqueue(new KeyValuePair<Action, Duration>(rearrangeAction, animationDuration));
            }
        }

        /// <summary>
        /// Orchestrates the rearrange animation process.
        /// </summary>
        private async void AnimateRearrangeInternal(Action rearrangeAction, Duration animationDuration)
        {
            // Find the indices of items in the view. Animations are optimzed to only include what is visible.
            int viewFirstIndex, viewLastIndex;
            this.GetViewIndexRange(true, out viewFirstIndex, out viewLastIndex);

            // Collect information about items and their positions before any changes are made.
            RearrangeItemInfo[] rearrangeMap = this.BuildRearrangeMap(viewFirstIndex, viewLastIndex);

            // Call the rearrange action callback which actually makes the changes to the source list.
            // Assuming the source list is properly bound, the base class will pick up the changes.
            rearrangeAction();

            this.rearrangeCanvas.Visibility = Visibility.Visible;

            // Update the layout (positions of all items) based on the changes that were just made.
            this.UpdateLayout();

            // Find the NEW last-index in view, which may have changed if the items are not constant heights
            // or if the view includes the end of the list.
            viewLastIndex = this.FindViewLastIndex(viewFirstIndex);

            // Collect information about the NEW items and their NEW positions, linking up to information
            // about items which existed before.
            RearrangeItemInfo[] rearrangeMap2 = this.BuildRearrangeMap2(rearrangeMap,
                viewFirstIndex, viewLastIndex);

            // Find all the movements that need to be animated.
            IEnumerable<RearrangeItemInfo> movesWithinView = rearrangeMap
                .Where(rii => !Double.IsNaN(rii.FromY) && !Double.IsNaN(rii.ToY));
            IEnumerable<RearrangeItemInfo> movesOutOfView = rearrangeMap
                .Where(rii => !Double.IsNaN(rii.FromY) && Double.IsNaN(rii.ToY));
            IEnumerable<RearrangeItemInfo> movesInToView = rearrangeMap2
                .Where(rii => Double.IsNaN(rii.FromY) && !Double.IsNaN(rii.ToY));
            IEnumerable<RearrangeItemInfo> visibleMoves =
                movesWithinView.Concat(movesOutOfView).Concat(movesInToView);

            // Set a clip rect so the animations don't go outside the listbox.
            this.rearrangeCanvas.Clip = new RectangleGeometry() { Rect = new Rect(new Point(0, 0), this.rearrangeCanvas.RenderSize) };

            // Create the animation storyboard.
            Storyboard rearrangeStoryboard = this.CreateRearrangeStoryboard(visibleMoves, animationDuration);
            if (rearrangeStoryboard.Children.Count > 0)
            {
                // The storyboard uses an overlay canvas with item snapshots.
                // While that is playing, hide the real items.
                this.scrollViewer.Visibility = Visibility.Collapsed;

                rearrangeStoryboard.Completed += delegate
                {
                    rearrangeStoryboard.Stop();
                    this.rearrangeCanvas.Children.Clear();
                    this.rearrangeCanvas.Visibility = Visibility.Collapsed;
                    this.scrollViewer.Visibility = Visibility.Visible;

                    this.AnimateNextRearrange();
                };
                
                // TODO: replaced this line:
                // this.Dispatcher.BeginInvoke(() => rearrangeStoryboard.Begin());
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    new CoreDispatcherPriority(), () => rearrangeStoryboard.Begin());
            }
            else
            {
                this.rearrangeCanvas.Visibility = Visibility.Collapsed;
                this.AnimateNextRearrange();
            }
        }

        /// <summary>
        /// Checks if there's another rearrange action waiting in the queue, and if so executes it next.
        /// </summary>
        private async void AnimateNextRearrange()
        {
            if (this.rearrangeQueue.Count > 0)
            {
                KeyValuePair<Action, Duration> nextRearrange = this.rearrangeQueue.Dequeue();
                

                // TODO: replaced this line:
                // this.Dispatcher.BeginInvoke(() =>
                //    this.AnimateRearrangeInternal(nextRearrange.Key, nextRearrange.Value));
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    new CoreDispatcherPriority(), () => this.AnimateRearrangeInternal(nextRearrange.Key, nextRearrange.Value));
            }
            else
            {
                this.rearrangeQueue = null;
            }
        }

        /// <summary>
        /// Collects information about items and their positions before any changes are made.
        /// </summary>
        private RearrangeItemInfo[] BuildRearrangeMap(int viewFirstIndex, int viewLastIndex)
        {
            RearrangeItemInfo[] map = new RearrangeItemInfo[this.Items.Count];

            for (int i = 0; i < map.Length; i++)
            {
                object item = this.Items[i];

                RearrangeItemInfo info = new RearrangeItemInfo()
                {
                    Item = item,
                    FromIndex = i,
                };

                // The precise item location is only important if it's within the view.
                if (viewFirstIndex <= i && i <= viewLastIndex)
                {
                    ReorderListBoxItem itemContainer = (ReorderListBoxItem)
                        this.ContainerFromIndex(i);
                    if (itemContainer != null)
                    {
                        GeneralTransform itemTransform = itemContainer.TransformToVisual(this.rearrangeCanvas);
                        Point itemPoint = itemTransform.TransformPoint(new Point(0, 0));
                        info.FromY = itemPoint.Y;
                        info.Height = itemContainer.RenderSize.Height;
                    }
                }

                map[i] = info;
            }

            return map;
        }

        /// <summary>
        /// Collects information about the NEW items and their NEW positions after changes were made.
        /// </summary>
        private RearrangeItemInfo[] BuildRearrangeMap2(RearrangeItemInfo[] map,
            int viewFirstIndex, int viewLastIndex)
        {
            RearrangeItemInfo[] map2 = new RearrangeItemInfo[this.Items.Count];

            for (int i = 0; i < map2.Length; i++)
            {
                object item = this.Items[i];

                // Try to find the same item in the pre-rearrange info.
                RearrangeItemInfo info = map.FirstOrDefault(rii => rii.ToIndex < 0 && rii.Item == item);
                if (info == null)
                {
                    info = new RearrangeItemInfo()
                    {
                        Item = item,
                    };
                }

                info.ToIndex = i;

                // The precise item location is only important if it's within the view.
                if (viewFirstIndex <= i && i <= viewLastIndex)
                {
                    ReorderListBoxItem itemContainer = (ReorderListBoxItem)
                        this.ContainerFromIndex(i);
                    if (itemContainer != null)
                    {
                        GeneralTransform itemTransform = itemContainer.TransformToVisual(this.rearrangeCanvas);
                        // TODO: replaced all itemTransform.Transform with itemTransform.TransformPoint
                        Point itemPoint = itemTransform.TransformPoint(new Point(0, 0));
                        info.ToY = itemPoint.Y;
                        info.Height = itemContainer.RenderSize.Height;
                    }
                }

                map2[i] = info;
            }

            return map2;
        }

        /// <summary>
        /// Finds the index of the last visible item by starting at the first index and
        /// comparing the bounds of each following item to the ScrollViewer bounds.
        /// </summary>
        /// <remarks>
        /// This method is less efficient than the hit-test method used by GetViewIndexRange() above,
        /// but it works when the controls haven't actually been rendered yet, while the other doesn't.
        /// </remarks>
        private int FindViewLastIndex(int firstIndex)
        {
            int lastIndex = firstIndex;

            // TODO: replaced all instances of Application.Current.RootVisual with Window.Current.Content
            // GeneralTransform scrollViewerTransform = this.scrollViewer.TransformToVisual(
            //    Application.Current.RootVisual);
            GeneralTransform scrollViewerTransform = this.scrollViewer.TransformToVisual(
                Window.Current.Content);

            Rect scrollViewerRect = scrollViewerTransform.TransformBounds(
                new Rect(new Point(0, 0), this.scrollViewer.RenderSize));

            while (lastIndex < this.Items.Count - 1)
            {
                ReorderListBoxItem itemContainer = (ReorderListBoxItem)
                    this.ContainerFromIndex(lastIndex + 1);
                if (itemContainer == null)
                {
                    break;
                }

                GeneralTransform itemTransform = itemContainer.TransformToVisual(
                    Window.Current.Content);
                Rect itemRect = itemTransform.TransformBounds(new Rect(new Point(0, 0), itemContainer.RenderSize));
                itemRect.Intersect(scrollViewerRect);
                if (itemRect == Rect.Empty)
                {
                    break;
                }

                lastIndex++;
            }

            return lastIndex;
        }

        /// <summary>
        /// Creates a storyboard to animate the visible moves of a rearrange.
        /// </summary>
        private Storyboard CreateRearrangeStoryboard(IEnumerable<RearrangeItemInfo> visibleMoves,
            Duration animationDuration)
        {
            Storyboard storyboard = new Storyboard();

            ReorderListBoxItem temporaryItemContainer = null;

            foreach (RearrangeItemInfo move in visibleMoves)
            {
                Size itemSize = new Size(this.rearrangeCanvas.RenderSize.Width, move.Height);

                ReorderListBoxItem itemContainer = null;
                if (move.ToIndex >= 0)
                {
                    itemContainer = (ReorderListBoxItem)this.ContainerFromIndex(move.ToIndex);
                }
                if (itemContainer == null)
                {
                    if (temporaryItemContainer == null)
                    {
                        temporaryItemContainer = new ReorderListBoxItem();
                    }

                    itemContainer = temporaryItemContainer;
                    itemContainer.Width = itemSize.Width;
                    itemContainer.Height = itemSize.Height;
                    this.rearrangeCanvas.Children.Add(itemContainer);
                    this.PrepareContainerForItemOverride(itemContainer, move.Item);
                    itemContainer.UpdateLayout();
                }

                // TODO: had to replace the WriteableBitmap with the itemSnapshot
                //WriteableBitmap itemSnapshot = new WriteableBitmap((int)itemSize.Width, (int)itemSize.Height);
                //itemSnapshot.Render(itemContainer, null);
                //itemSnapshot.Invalidate();
                Image itemImage = new Image();
                if (itemImage.Source == null)
                {
                    Task<RenderTargetBitmap> renderTask = RenderImageSource(itemContainer, itemSize);
                    itemImage.Source = renderTask.Result;
                }

                //Image itemImage = new Image();
                itemImage.Width = itemSize.Width;
                itemImage.Height = itemSize.Height;
                //itemImage.Source = renderTask.Result;
                itemImage.RenderTransform = new TranslateTransform();
                this.rearrangeCanvas.Children.Add(itemImage);

                if (itemContainer == temporaryItemContainer)
                {
                    this.rearrangeCanvas.Children.Remove(itemContainer);
                }

                if (!Double.IsNaN(move.FromY) && !Double.IsNaN(move.ToY))
                {
                    Canvas.SetTop(itemImage, move.FromY);
                    if (move.FromY != move.ToY)
                    {
                        DoubleAnimation moveAnimation = new DoubleAnimation();
                        moveAnimation.Duration = animationDuration;
                        Storyboard.SetTarget(moveAnimation, itemImage.RenderTransform);
                        Storyboard.SetTargetProperty(moveAnimation, TranslateTransform.YProperty.ToString());
                        moveAnimation.To = move.ToY - move.FromY;
                        storyboard.Children.Add(moveAnimation);
                    }
                }
                else if (Double.IsNaN(move.FromY) != Double.IsNaN(move.ToY))
                {
                    if (move.FromIndex >= 0 && move.ToIndex >= 0)
                    {
                        DoubleAnimation moveAnimation = new DoubleAnimation();
                        moveAnimation.Duration = animationDuration;
                        Storyboard.SetTarget(moveAnimation, itemImage.RenderTransform);
                        Storyboard.SetTargetProperty(moveAnimation, TranslateTransform.YProperty.ToString());

                        const double animationDistance = 200;
                        if (!Double.IsNaN(move.FromY))
                        {
                            Canvas.SetTop(itemImage, move.FromY);
                            if (move.FromIndex < move.ToIndex)
                            {
                                moveAnimation.To = animationDistance;
                            }
                            else if (move.FromIndex > move.ToIndex)
                            {
                                moveAnimation.To = -animationDistance;
                            }
                        }
                        else
                        {
                            Canvas.SetTop(itemImage, move.ToY);
                            if (move.FromIndex < move.ToIndex)
                            {
                                moveAnimation.From = -animationDistance;
                            }
                            else if (move.FromIndex > move.ToIndex)
                            {
                                moveAnimation.From = animationDistance;
                            }
                        }

                        storyboard.Children.Add(moveAnimation);
                    }

                    DoubleAnimation fadeAnimation = new DoubleAnimation();
                    fadeAnimation.Duration = animationDuration;
                    Storyboard.SetTarget(fadeAnimation, itemImage);
                    // TODO: this might not get the right property: see commented original code
                    // Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTargetProperty(fadeAnimation, UIElement.OpacityProperty.ToString());

                    if (Double.IsNaN(move.FromY))
                    {
                        itemImage.Opacity = 0.0;
                        fadeAnimation.To = 1.0;
                        Canvas.SetTop(itemImage, move.ToY);
                    }
                    else
                    {
                        itemImage.Opacity = 1.0;
                        fadeAnimation.To = 0.0;
                        Canvas.SetTop(itemImage, move.FromY);
                    }

                    storyboard.Children.Add(fadeAnimation);
                }
            }

            return storyboard;
        }

        #endregion


        #region Private utility methods

        /// <summary>
        /// Gets host coordinates, adjusting for orientation. This is helpful when identifying what
        /// controls are under a point.
        /// </summary>
        private static Point GetHostCoordinates(Point point)
        {
            Frame frame = ((Frame)Window.Current.Content);

            switch (DisplayInformation.GetForCurrentView().CurrentOrientation.GetPageOrientation())
            {
                // TODO: this might be backwards - test
                case PageOrientations.Landscape: return new Point(frame.RenderSize.Width - point.Y, point.X);
                case PageOrientations.LandscapeFlipped: return new Point(point.Y, frame.RenderSize.Height - point.X);
                default: return point;
            }
        }

        private async Task<RenderTargetBitmap> RenderImageSource(UIElement element, Size size)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(element, (int)size.Width, (int)size.Height);

            return renderTargetBitmap;
        }

        #endregion


        #region Event handlers

        private void ItemsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Have to use this method instead of a TemplatePart, since the item is part of the ItemsPanelTemplate
            // See http://stackoverflow.com/questions/4786006/gettemplatechild-always-returns-null
            itemsPanel = sender as StackPanel;
        }

        #endregion
    }
}
