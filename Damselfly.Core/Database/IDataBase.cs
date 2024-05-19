using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Damselfly.Core.DBAbstractions;
using Microsoft.EntityFrameworkCore;

namespace Damselfly.Core.Interfaces;

/// <summary>
///     Interface representing a type of DB (e.g., Sqlite, MySql or SQL Server.
///     It provides a common interface to all DB types, for the operations we need.
/// </summary>
public interface IDataBase
{
    void Init(BaseDBModel db);
    void Configure(DbContextOptionsBuilder options);
    void FlushDBWriteCache(BaseDBModel db);

    Task<bool> BulkUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class;
    Task<bool> BulkInsert<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class;
    Task<bool> BulkDelete<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToDelete) where T : class;

    Task<bool> BulkInsertOrUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave, Func<T, bool> isNew)
        where T : class;

    Task<int> BatchDelete<T>(BaseDBModel db, IQueryable<T> query) where T : class;

    Task<int> BatchUpdate<T>(BaseDBModel db, IQueryable<T> query, Expression<Func<T, T>> updateExpression)
        where T : class;

    IQueryable<T> ImageSearch<T>(DbSet<T> resultSet, string query, bool includeAITags) where T : class;
}
