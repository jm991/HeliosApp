using System.Threading.Tasks;
using Helios.Model;
using Windows.Media.Editing;
using Windows.UI;
using System;

namespace Helios.Design
{
    public class DesignDataService : IDataService
    {
        public Task<DataItem> GetData()
        {
            // Use this to create design time data

            DataItem item = new DataItem("Welcome to MVVM Light [design testing]");
            item.Clip = MediaClip.CreateFromColor(Color.FromArgb(byte.MaxValue, byte.MinValue, byte.MaxValue, byte.MinValue), TimeSpan.FromSeconds(5));
            return Task.FromResult(item);
        }
    }
}