using System;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.UI;
namespace Helios.Model
{
    public class DataItem
    {
        public string Title
        {
            get;
            private set;
        }

        public MediaClip Clip
        {
            get;
            set;
        }

        public bool MediaLoaded
        {
            get;
            private set;
        }

        public DataItem(string title)
        {
            //Task task = new Task(LoadTestVideo);
            //task.Start();
            //task.Wait();


            Title = title;

            MediaLoaded = true;

            // Clip = MediaClip.CreateFromColor(Color.FromArgb(byte.MaxValue, byte.MinValue, byte.MaxValue, byte.MinValue), TimeSpan.FromSeconds(5));
        }

        //private async void LoadTestVideo()
        //{
        //    var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Video.mp4", UriKind.Absolute));
        //    Clip = await MediaClip.CreateFromFileAsync(file);
        //}
    }
}