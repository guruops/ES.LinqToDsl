namespace GuruOps.ES.LinqToDsl.DAL.Repositories
{
    public interface IIndexNameResolver
    {
        string GetIndexName(string indexName);
    }
}