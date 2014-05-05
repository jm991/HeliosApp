using Helios.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Helios
{
    public static class Utilities
    {
        public static async void MessageBox(string msg)
        {
            var msgDlg = new Windows.UI.Popups.MessageDialog(msg);
            msgDlg.DefaultCommandIndex = 1;
            await msgDlg.ShowAsync();
        }

        public static string GetMemberName<T>(Expression<Func<T>> memberExpression)
        {
            MemberExpression expressionBody = (MemberExpression)memberExpression.Body;
            return expressionBody.Member.Name;
        }

        /// <summary>
        /// Extension method for a FrameworkElement that searches for a child element by type and name.
        /// </summary>
        /// <typeparam name="T">The type of the child element to search for.</typeparam>
        /// <param name="element">The parent framework element.</param>
        /// <param name="sChildName">The name of the child element to search for.</param>
        /// <returns>The matching child element, or null if none found.</returns>
        public static T FindElementByName<T>(this FrameworkElement element, string sChildName) where T : FrameworkElement
        {
            Debug.WriteLine("[FindElementByName] ==> element [{0}] sChildName [{1}] T [{2}]", element, sChildName, typeof(T).ToString());

            T childElement = null;

            //
            // Spin through immediate children of the starting element.
            //
            var nChildCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < nChildCount; i++)
            {
                // Get next child element.
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                Debug.WriteLine("Found child [{0}]", child);

                // Do we have a child?
                if (child == null)
                    continue;

                // Is child of desired type and name?
                if (child is T && child.Name.Equals(sChildName))
                {
                    // Bingo! We found a match.
                    childElement = (T)child;
                    Debug.WriteLine("Found matching element [{0}]", childElement);
                    break;
                } // if

                // Recurse and search through this child's descendants.
                childElement = FindElementByName<T>(child, sChildName);

                // Did we find a matching child?
                if (childElement != null)
                    break;
            } // for

            Debug.WriteLine("[FindElementByName] <== childElement [{0}]", childElement);
            return childElement;
        }

        public static Point Add(this Point point1, Point point2)
        {
            return new Point(point1.X + point2.X, point1.Y + point2.Y);
        }
    }
}
