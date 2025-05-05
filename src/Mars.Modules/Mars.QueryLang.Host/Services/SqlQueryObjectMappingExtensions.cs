using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mars.Host.QueryLang;

public static class SqlQueryObjectMappingExtensions
{
    public static IList<T> SqlQuery<T>(this DbContext db, string sql, params object[] parameters) where T : class
    {
        using (var db2 = new ContextForQueryType<T>(db.Database.GetDbConnection()))
        {
            // share the current database transaction, if one exists
            var transaction = db.Database.CurrentTransaction;
            if (transaction != null)
                db2.Database.UseTransaction(transaction.GetDbTransaction());
            return db2.Set<T>().FromSqlRaw(sql, parameters).ToList();
        }
    }

    private class ContextForQueryType<T> : DbContext where T : class
    {
        private readonly DbConnection connection;

        public ContextForQueryType(DbConnection connection)
        {
            this.connection = connection;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connection, options => options.EnableRetryOnFailure());

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<T>().HasNoKey();
            base.OnModelCreating(modelBuilder);
        }
    }
}
