using GuruOps.ES.LinqToDsl.Models;
using System;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL
{
    public interface IFilterQuery<T> where T : Document
    {
        Expression<Func<T, bool>> GetQuery();
    }
}
