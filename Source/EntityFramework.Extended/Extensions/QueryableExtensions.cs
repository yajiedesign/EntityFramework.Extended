﻿using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;

namespace EntityFramework.Extensions
{
    /// <summary>
    /// Extension methods for IQueryable to support INSERT ... and SELECT INSERT ... SQL statements without loading objects into RAM. 
    /// Contributed by Agile Design LLC ( http://agiledesignllc.com/ ).
    /// </summary>
    public static class QueryableExtensions
    {
        private const string FromTableSqlExpression = @"\bFROM\b";

        /// <summary>
        /// Captures SELECT ... FROM ... SQL from IDbSet, converts it into SELECT ... INTO ... T-SQL and executes it via context.ExecuteStoreCommand().<br/>
        /// No objects are being brought into RAM / Context. <br/>
        /// Only MS SQL Server and Sybase T-SQL RDBMS are supported.
        /// Contributed by Agile Design LLC ( http://agiledesignllc.com/ ).
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="source">DbSet of entities</param>
        /// <param name="tableName">Target table name to insert into</param>
        public static void SelectInsert<TEntity>(this IDbSet<TEntity> source, string tableName)
            where TEntity : class
        {
            source.GetContext()
                .ExecuteStoreCommand(source.SelectInsertSql(tableName));
        }

        /// <summary>
        /// Captures SELECT ... FROM ... SQL from IQueryable, converts it into SELECT ... INTO ... T-SQL and executes it on sqlCommand.<br/>
        /// No objects are being brought into RAM / Context. <br/>
        /// Only MS SQL Server and Sybase T-SQL RDBMS are supported.
        /// Contributed by Agile Design LLC ( http://agiledesignllc.com/ ).
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="tableName">Target table name to insert into</param>
        /// <param name="source">DbSet of entities (or any IQueryable that returns SQL from its ToString() implementation</param>
        /// <param name="connection">Creates IDbCommand which executes generated T-SQL</param>
        public static void SelectInsert<TEntity>(this IQueryable<TEntity> source, string tableName, IDbConnection connection)
            where TEntity : class
        {
            var sqlCommand = connection.CreateCommand();
            sqlCommand.CommandText = source.SelectInsertSql(tableName);
            sqlCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Captures SELECT ... FROM ... SQL from IQueryable, converts it into SELECT ... INTO ... T-SQL and executes it on sqlCommand.<br/>
        /// No objects are being brought into RAM / Context. <br/>
        /// Only MS SQL Server and Sybase T-SQL RDBMS are supported.
        /// Contributed by Agile Design LLC ( http://agiledesignllc.com/ ).
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="sqlConnection">Creates IDbCommand which executes generated T-SQL</param>
        /// <param name="tableName">Target table name to insert into</param>
        /// <param name="source">DbSet of entities (or any IQueryable that returns SQL from its ToString() implementation</param>
        public static void SelectInsert<TEntity>(this IDbConnection sqlConnection, string tableName, IQueryable<TEntity> source)
            where TEntity : class
        {
            source.SelectInsert(tableName, sqlConnection);
        }

        /// <summary>
        /// Captures SELECT ... FROM ... SQL from IQueryable and converts it into SELECT ... INTO ... T-SQL. <br/>
        /// No objects are being brought into RAM / Context. <br/>
        /// Only MS SQL Server and Sybase T-SQL RDBMS are supported.
        /// Contributed by Agile Design LLC ( http://agiledesignllc.com/ ).
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="source">DbSet of entities (or any IQueryable that returns SQL from its ToString() implementation</param>
        /// <param name="tableName">Target table name to insert into</param>
        /// <returns>SELECT ... INSERT ... T-SQL statement</returns>
        public static string SelectInsertSql<TEntity>(this IQueryable<TEntity> source, string tableName)
        {
            var regex = new Regex(FromTableSqlExpression);
            string selectInsertSql = regex.Replace(
                source.ToString()
                , string.Format(" INTO {0} FROM ", tableName)
                , 1);

            return selectInsertSql;
        }

        /// <summary>
        /// Captures SELECT ... FROM ... SQL from IQueryable and converts it into INSERT INTO ... SELECT FROM ... ANSI-SQL. <br/>
        /// No objects are being brought into RAM / Context. <br/>
        /// Contributed by Agile Design LLC ( http://agiledesignllc.com/ ).
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="source">DbSet of entities (or any IQueryable that returns SQL from its ToString() implementation</param>
        /// <param name="tableName">Target table name to insert into</param>
        /// <param name="columnList">Optional parameter for a list of columns to insert into</param>
        /// <returns>INSERT INTO ... SELECT FROM ANSI-SQL statement</returns>
        public static string InsertIntoSql<TEntity>(this IQueryable<TEntity> source, string tableName, string columnList = "")
        {
            string originalSql = source.ToString();
            if (! string.IsNullOrWhiteSpace(columnList))
            {
                columnList = string.Format("({0})", columnList);
            }
            return string.Format("INSERT INTO {0} {1}\r\n{2}"
                                 , tableName
                                 , columnList
                                 , originalSql);
        }
    }
}