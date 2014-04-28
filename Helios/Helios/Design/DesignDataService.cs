using System.Threading.Tasks;
using Helios.Model;

namespace Helios.Design
{
    public class DesignDataService : IDataService
    {
        public Task<DataItem> GetData()
        {
            // Use this to create design time data

            DataItem item = new DataItem("Welcome to MVVM Light [design testing]");
            return Task.FromResult(item);
        }
    }
}