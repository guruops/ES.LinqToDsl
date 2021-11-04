using System.Threading.Tasks;

namespace GuruOps.ES.LinqToDsl.DAL
{
    public interface IElasticSearchUserContext
    {
        Task<string> GetUserId();
    }
}