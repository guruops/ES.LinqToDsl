using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GuruOps.ES.LinqToDsl.Models;
using GuruOps.ES.LinqToDsl.Models.QueryModels;

namespace GuruOps.ES.LinqToDsl.DAL.Repositories
{
    public interface IRepository<T> where T : DocumentBase
    {
        Task<Result> UpsertAsync(T newDocument, T currentDocument, bool disableAudit = false);

        Task<Result> AddAsync(T document);

        Task<ResultList> GetAllAsync(
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false);

        Task<Result> GetAsync(string id);

        Task<ResultList> GetWhereAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false);

        Task<ResultList> GetWhereWithPagingAsync(
            Expression<Func<T, bool>> predicate = null,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            int itemCount = 10,
            string continuationToken = "",
            bool includeDeleted = false,
            string searchText = null,
            int pageTake = 0,
            int pageSkip = 0);

        Task<Result> DeleteAsync(T document, bool disableAudit = false);

        Task<int> CountAsync(
            Expression<Func<T, bool>> predicate = null, 
            bool includeDeleted = false,
            string searchText = null);

        Task<ResultList> BulkCreateAsync(List<T> documents);

        Task<ResultList> BulkUpsertAsync(List<T> documents);

        Task<ResultList> BulkDeleteAsync(List<T> documents);
    }
}
