using GuruOps.ES.LinqToDsl.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GuruOps.ES.LinqToDsl.DAL
{
    public interface IServiceBase<T>
    {
        Task<Result<T>> CreateAsync(T document);

        Task<ResultList<T>> BulkCreateAsync(List<T> documents, int batchCount = 100);

        Task<Result<T>> UpsertAsync(T document);

        Task<ResultList<T>> BulkUpsertAsync(List<T> documents, List<T> currentDocuments = null, int batchCount = 100);

        Task<ResultList<T>> UpsertAllAsync(List<T> documents, List<T> currentDocuments = null, int batchCount = 10);

        Task<Result<T>> DeleteAsync(T document);

        Task<ResultList<T>> BulkDeleteAsync(List<T> documents, int batchCount = 100);

        Task<ResultList<T>> DeleteAllAsync(List<T> documents,
            int batchCount = 10);

        Task<Result<T>> GetAsync(string id, bool system = false);

        Task<ResultList<T>> GetByIdsAsync(
            List<string> ids,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false,
            bool system = false);

        Task<ResultList<T>> GetAllAsync(
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false,
            bool system = false);

        Task<ResultList<T>> GetAllWhereAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false,
            bool system = false);

        Task<ResultList<T>> GetWhereAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false,
            bool system = false);

        Task<ResultList<T>> GetWithPagingAsync(
            Expression<Func<T, bool>> predicate = null,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            int itemCount = 10,
            string continuationToken = "",
            bool includeDeleted = false,
            string searchText = null,
            bool system = false);

        Task<Result<T>> CountAsync(
            Expression<Func<T, bool>> predicate = null,
            bool includeDeleted = false,
            string searchText = "",
            bool system = false);
    }
}