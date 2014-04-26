using System.Threading.Tasks;

namespace Helios.Model
{
    public interface IDataService
    {
        Task<DataItem> GetData();
    }
}