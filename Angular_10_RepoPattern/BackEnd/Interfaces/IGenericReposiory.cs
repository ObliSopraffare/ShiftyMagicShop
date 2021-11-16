using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Angular_10_RepoPattern.BackEnd.Interfaces
{
    public interface IGenericReposiory<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task DeleteRowAsync(int id);
        Task<T> GetAsync(int id);
        Task<IEnumerable<T>> GetAsyncByGiven(T t, string[] fields);
        Task<int> SaveRangeAsync(IEnumerable<T> list, string[] fields);
        Task UpdateAsync(T t, string[] fields);
        Task InsertAsync(T t, string[] fields);
        Task<string> StoredProcedure(string sp);
        Task<string> StoredProcedureParam(string sp, object parameters);
        Task<int> getScopeIdentity();
    }
}
