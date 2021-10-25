using GuruOps.ES.LinqToDsl.Models;
using System;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL
{
    public class PaginationRequest<T> where T : Document
    {
        public PaginationRequest()
        {
            ItemsPerPage = 10;
            PageNumber = 1;
            IncludeDeleted = false;
            SearchText = null;
            OrderByDesc = t => t.Modified;
        }

        public Expression<Func<T, object>> OrderBy { get; set; }
        public Expression<Func<T, object>> OrderByDesc { get; set; }
        public int ItemsPerPage { get; set; }
        public int PageNumber { get; set; }
        public bool IncludeDeleted { get; set; }
        public string SearchText { get; set; }
    }
}
