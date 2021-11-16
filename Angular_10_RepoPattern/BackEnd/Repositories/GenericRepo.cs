using Angular_10_RepoPattern.BackEnd.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Angular_10_RepoPattern.BackEnd.Repositories
{
    public abstract class GenericRepo<T> : IGenericReposiory<T> where T : class
    {
        private readonly string _tableName;
        private readonly string _connection;

        //Get The properties of the object to parse it into a query.
        private IEnumerable<PropertyInfo> GetProperties => typeof(T).GetProperties();
        private static List<string> GenerateListOfProperties(IEnumerable<PropertyInfo> listOfProperties)
        {
            return (from prop in listOfProperties
                    let attributes = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    where attributes.Length <= 0 || (attributes[0] as DescriptionAttribute)?.Description != "ignore"
                    select prop.Name).ToList();
        }

        protected GenericRepo(string tableName, string connection)
        {
            _tableName = tableName;
            _connection = connection;
        }

        private SqlConnection SqlConnection()
        {
            return new SqlConnection(_connection);
        }

        private IDbConnection CreateConnection()
        {
            var conn = SqlConnection();
            conn.Open();
            return conn;
        }

        ///<summary>
        ///Get All elemnts in table
        ///</summary>
        ///<returns></returns>
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<T>($"SELECT * FROM {_tableName}");
            }
        }

        ///<summary>
        ///Delete Row in table
        ///</summary>
        ///<returns></returns>
        public async Task DeleteRowAsync(int id)
        {
            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync($"DELETE FROM {_tableName} WHERE Id=@Id", new { Id = id });
            }
        }

        ///<summary>
        ///Get Row By Id
        ///</summary>
        ///<returns></returns>
        public async Task<T> GetAsync(int id)
        {
            using (var connection = CreateConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<T>($"SELECT * FROM {_tableName} WHERE Id=@Id", new { Id = id });
                if (result == null)
                    throw new KeyNotFoundException($"{_tableName} with id [{id}] could not be found");

                return result;
            }
        }

        ///<summary>
        ///Save multiple rows
        ///</summary>
        ///<returns></returns>
        public async Task<int> SaveRangeAsync(IEnumerable<T> list, string[] fields)
        {
            var inserted = 0;
            var query = GenerateInsertQuery(fields);
            using (var connection = CreateConnection())
            {
                inserted += await connection.ExecuteAsync(query, list);
            }

            return inserted;
        }

        ///<summary>
        ///Insert query
        ///</summary>
        ///<returns></returns>
        public async Task InsertAsync(T t, string[] fields)
        {
            var insertQuery = GenerateInsertQuery(fields);

            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(insertQuery, t);
            }
        }
        private string GenerateInsertQuery(string[] fields)
        {
            var insertQuery = new StringBuilder($"INSERT INTO {_tableName} ");
            insertQuery.Append("(");

            var properties = GenerateListOfProperties(GetProperties);
            properties.ForEach(prop =>
            {
                if (fields.Contains(prop))
                {
                    insertQuery.Append($"[{prop}],");
                }
            });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(") VALUES (");

            properties.ForEach(prop =>
            {
                if (fields.Contains(prop) && prop != "Id")
                {
                    insertQuery.Append($"@{prop},");
                }
            });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(")");

            return insertQuery.ToString();
        }

        ///<summary>
        ///Update query
        ///</summary>
        ///<returns></returns>
        public async Task UpdateAsync(T t, string[] fields)
        {
            var updateQuery = GenerateUpdateQuery(fields);

            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(updateQuery, t);
            }
        }
        private string GenerateUpdateQuery(string[] fields)
        {
            var updateQuery = new StringBuilder($"UPDATE {_tableName} SET ");
            var properties = GenerateListOfProperties(GetProperties);

            properties.ForEach(property =>
            {
                if (fields.Contains(property) && property != "Id")
                {
                    updateQuery.Append($"[{property}] = @{property},");
                }
            });

            updateQuery.Remove(updateQuery.Length - 1, 1);//Remove last comma
            updateQuery.Append(" WHERE Id=@Id");

            return updateQuery.ToString();
        }

        ///<summary>
        ///Get Row by given columns
        ///</summary>
        ///<returns></returns>
        public async Task<IEnumerable<T>> GetAsyncByGiven(T t, string[] fields)
        {
            var query = GenerateSelectQuery(fields);

            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<T>(query, t);
            }
        }
        private string GenerateSelectQuery(string[] fields)
        {
            var selectQuery = new StringBuilder($"SELECT * FROM {_tableName} WHERE ");
            var properties = GenerateListOfProperties(GetProperties);

            properties.ForEach(property =>
            {
                if (fields.Contains(property))
                {
                    selectQuery.Append($"[{property}] = @{property} and ");
                }
                else if (fields.Contains("!" + property))
                {
                    selectQuery.Append($"[{property}] != @{property} and ");
                }
            });

            selectQuery.Remove(selectQuery.Length - 4, 4);//Remove last _and

            return selectQuery.ToString();
        }

        ///<summary>
        ///Get by stored procedure
        ///</summary>
        ///<returns></returns>
        public async Task<string> StoredProcedure(string sp)
        {
            var procedure = sp;

            using (var connection = CreateConnection())
            {
                var result = await connection.QueryAsync(procedure, commandType: CommandType.StoredProcedure);
                var str = JsonConvert.SerializeObject(result);
                return str;
            }
        }

        ///<summary>
        ///Get by stored procedure with param
        ///</summary>
        ///<returns></returns>
        public async Task<string> StoredProcedureParam(string sp, object parameters)
        {
            var procedure = sp;
            var values = parameters;

            using (var connection = CreateConnection())
            {
                var result = await connection.QueryAsync(procedure, values, commandType: CommandType.StoredProcedure);
                var str = JsonConvert.SerializeObject(result);
                return str;
            }
        }

        ///<summary>
        ///Get Scope Identity
        ///</summary>
        ///<returns></returns>
        public async Task<int> getScopeIdentity()
        {
            using (var connection = CreateConnection())
            {
                return await connection.ExecuteScalarAsync<int>("SELECT Top 1 Id from AAP_Tickets.dbo.[Ticket] order by Id desc");
            }
        }
    }
}
