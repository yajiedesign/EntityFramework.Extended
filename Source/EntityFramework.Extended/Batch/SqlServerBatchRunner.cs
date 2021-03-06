using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using EntityFramework.Extensions;
using EntityFramework.Mapping;
using EntityFramework.Reflection;

namespace EntityFramework.Batch
{
    /// <summary>
    /// A batch execution runner for SQL Server.
    /// </summary>
    public class SqlServerBatchRunner : IBatchRunner
    {
        /// <summary>
        /// Create and run a batch delete statement.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="objectContext">The <see cref="ObjectContext"/> to get connection and metadata information from.</param>
        /// <param name="entityMap">The <see cref="EntityMap"/> for <typeparamref name="TEntity"/>.</param>
        /// <param name="query">The query to create the where clause from.</param>
        /// <returns>
        /// The number of rows deleted.
        /// </returns>
        public int Delete<TEntity>(ObjectContext objectContext, EntityMap entityMap, ObjectQuery<TEntity> query)
            where TEntity : class
        {
            DbConnection deleteConnection = null;
            DbTransaction deleteTransaction = null;
            DbCommand deleteCommand = null;
            bool ownConnection = false;
            bool ownTransaction = false;

            try
            {
                // get store connection and transaction
                var store = GetStore(objectContext);
                deleteConnection = store.Item1;
                deleteTransaction = store.Item2;

                if (deleteConnection.State != ConnectionState.Open)
                {
                    deleteConnection.Open();
                    ownConnection = true;
                }

                if (deleteTransaction == null)
                {
                    deleteTransaction = deleteConnection.BeginTransaction();
                    ownTransaction = true;
                }


                deleteCommand = deleteConnection.CreateCommand();
                deleteCommand.Transaction = deleteTransaction;
                if (objectContext.CommandTimeout.HasValue)
                    deleteCommand.CommandTimeout = objectContext.CommandTimeout.Value;

                var innerSelect = GetSelectSql(query, entityMap, deleteCommand);

                var sqlBuilder = new StringBuilder(innerSelect.Length * 2);

                sqlBuilder.Append("DELETE ");
                sqlBuilder.Append(entityMap.TableName);
                sqlBuilder.AppendLine();

                sqlBuilder.AppendFormat("FROM {0} AS j0 INNER JOIN (", entityMap.TableName);
                sqlBuilder.AppendLine();
                sqlBuilder.AppendLine(innerSelect);
                sqlBuilder.Append(") AS j1 ON (");

                bool wroteKey = false;
                foreach (var keyMap in entityMap.KeyMaps)
                {
                    if (wroteKey)
                        sqlBuilder.Append(" AND ");

                    sqlBuilder.AppendFormat("j0.[{0}] = j1.[{0}]", keyMap.ColumnName);
                    wroteKey = true;
                }
                sqlBuilder.Append(")");

                deleteCommand.CommandText = sqlBuilder.ToString();

                int result = deleteCommand.ExecuteNonQuery();

                // only commit if created transaction
                if (ownTransaction)
                    deleteTransaction.Commit();

                return result;
            }
            finally
            {
                if (deleteCommand != null)
                    deleteCommand.Dispose();

                if (deleteTransaction != null && ownTransaction)
                    deleteTransaction.Dispose();

                if (deleteConnection != null && ownConnection)
                    deleteConnection.Close();
            }
        }

        /// <summary>
        /// Create and run a batch update statement.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="objectContext">The <see cref="ObjectContext"/> to get connection and metadata information from.</param>
        /// <param name="entityMap">The <see cref="EntityMap"/> for <typeparamref name="TEntity"/>.</param>
        /// <param name="query">The query to create the where clause from.</param>
        /// <param name="updateExpression">The update expression.</param>
        /// <returns>
        /// The number of rows updated.
        /// </returns>
        public int Update<TEntity>(ObjectContext objectContext, EntityMap entityMap, ObjectQuery<TEntity> query, Expression<Func<TEntity, TEntity>> updateExpression)
            where TEntity : class
        {
            DbConnection updateConnection = null;
            DbTransaction updateTransaction = null;
            DbCommand updateCommand = null;
            bool ownConnection = false;
            bool ownTransaction = false;

            try
            {
                // get store connection and transaction
                var store = GetStore(objectContext);
                updateConnection = store.Item1;
                updateTransaction = store.Item2;

                if (updateConnection.State != ConnectionState.Open)
                {
                    updateConnection.Open();
                    ownConnection = true;
                }

                // use existing transaction or create new
                if (updateTransaction == null)
                {
                    updateTransaction = updateConnection.BeginTransaction();
                    ownTransaction = true;
                }

                updateCommand = updateConnection.CreateCommand();
                updateCommand.Transaction = updateTransaction;
                if (objectContext.CommandTimeout.HasValue)
                    updateCommand.CommandTimeout = objectContext.CommandTimeout.Value;

                var innerSelect = GetSelectSql(query, entityMap, updateCommand);
                var sqlBuilder = new StringBuilder(innerSelect.Length * 2);

                sqlBuilder.Append("UPDATE ");
                sqlBuilder.Append(entityMap.TableName);
                sqlBuilder.AppendLine(" SET ");

                var memberInitExpression = updateExpression.Body as MemberInitExpression;
                if (memberInitExpression == null)
                    throw new ArgumentException("The update expression must be of type MemberInitExpression.", "updateExpression");

                int nameCount = 0;
                bool wroteSet = false;
                foreach (MemberBinding binding in memberInitExpression.Bindings)
                {
                    IPropertyMapElement propertyMap =
                        entityMap.PropertyMaps.SingleOrDefault(p => p.PropertyName == binding.Member.Name);
                    if (propertyMap is ComplexPropertyMap)
                    {
                        ComplexPropertyMap cpm = propertyMap as ComplexPropertyMap;
                        var memberAssignment = binding as MemberAssignment;
                        if (memberAssignment == null)
                            throw new ArgumentException("The update expression MemberBinding must only by type MemberAssignment.", "updateExpression");
                        var expr = memberAssignment.Expression as MemberInitExpression;
                        if (expr == null)
                            throw new ArgumentException("The update expression MemberBinding must only by type MemberAssignment.", "updateExpression");
                        foreach (var subBinding in expr.Bindings)
                        {
                            AddUpdateRow<TEntity>(objectContext, entityMap, subBinding, sqlBuilder, updateCommand,
                                                  cpm.TypeElements, ref nameCount, ref wroteSet);
                        }
                    }
                    else
                    {
//<<<<<<< HEAD
                      
//=======
                        AddUpdateRow<TEntity>(objectContext, entityMap, binding, sqlBuilder, updateCommand,
                                              entityMap.PropertyMaps, ref nameCount, ref wroteSet);
//>>>>>>> 037c30c4c6a87e08606f9f680b60b63c8ca81da4
                    }
                }

                sqlBuilder.AppendLine(" ");
                sqlBuilder.AppendFormat("FROM {0} AS j0 INNER JOIN (", entityMap.TableName);
                sqlBuilder.AppendLine();
                sqlBuilder.AppendLine(innerSelect);
                sqlBuilder.Append(") AS j1 ON (");

                bool wroteKey = false;
                foreach (var keyMap in entityMap.KeyMaps)
                {
                    if (wroteKey)
                        sqlBuilder.Append(" AND ");

                    sqlBuilder.AppendFormat("j0.[{0}] = j1.[{0}]", keyMap.ColumnName);
                    wroteKey = true;
                }
                sqlBuilder.Append(")");

                updateCommand.CommandText = sqlBuilder.ToString();

                int result = updateCommand.ExecuteNonQuery();

                // only commit if created transaction
                if (ownTransaction)
                    updateTransaction.Commit();

                return result;
            }
            finally
            {
                if (updateCommand != null)
                    updateCommand.Dispose();
                if (updateTransaction != null && ownTransaction)
                    updateTransaction.Dispose();
                if (updateConnection != null && ownConnection)
                    updateConnection.Close();
            }
        }


        private static void AddUpdateRow<TEntity>(ObjectContext objectContext, EntityMap entityMap, MemberBinding binding, StringBuilder sqlBuilder, DbCommand updateCommand, IEnumerable<IPropertyMapElement> propertyMap, ref int nameCount, ref bool wroteSet)
            where TEntity : class
        {
            if (wroteSet)
                sqlBuilder.AppendLine(", ");

            string propertyName = binding.Member.Name;
            PropertyMap property =
                propertyMap.SingleOrDefault(p => p.PropertyName == propertyName) as PropertyMap;
            Debug.Assert(property != null, "property != null");
            string columnName = property.ColumnName;
            var memberAssignment = binding as MemberAssignment;
            if (memberAssignment == null)
                throw new ArgumentException("The update expression MemberBinding must only by type MemberAssignment.", "binding");

            Expression memberExpression = memberAssignment.Expression;

            ParameterExpression parameterExpression = null;
            memberExpression.Visit((ParameterExpression p) =>
            {
                if (p.Type == entityMap.EntityType)
                    parameterExpression = p;

                return p;
            });


            if (parameterExpression == null)
            {
                object value;

                if (memberExpression.NodeType == ExpressionType.Constant)
                {
                    var constantExpression = memberExpression as ConstantExpression;
                    if (constantExpression == null)
                        throw new ArgumentException(
                            "The MemberAssignment expression is not a ConstantExpression.", "binding");

                    value = constantExpression.Value;
                }
                else
                {
                    LambdaExpression lambda = Expression.Lambda(memberExpression, null);
                    value = lambda.Compile().DynamicInvoke();
                }

                if (value != null)
                {
                    string parameterName = "p__update__" + nameCount++;
                    var parameter = updateCommand.CreateParameter();
                    parameter.ParameterName = parameterName;
                    parameter.Value = value;
                    updateCommand.Parameters.Add(parameter);

                    sqlBuilder.AppendFormat("[{0}] = @{1}", columnName, parameterName);
                }
                else
                {
                    sqlBuilder.AppendFormat("[{0}] = NULL", columnName);
                }
            }
            else
            {
                // create clean objectset to build query from
                var objectSet = objectContext.CreateObjectSet<TEntity>();

                Type[] typeArguments = new[] { entityMap.EntityType, memberExpression.Type };

                ConstantExpression constantExpression = Expression.Constant(objectSet);
                LambdaExpression lambdaExpression = Expression.Lambda(memberExpression, parameterExpression);

                MethodCallExpression selectExpression = Expression.Call(
                    typeof(Queryable),
                    "Select",
                    typeArguments,
                    constantExpression,
                    lambdaExpression);

                // create query from expression
                var selectQuery = objectSet.CreateQuery(selectExpression, entityMap.EntityType);
                string sql = selectQuery.ToTraceString();

                // parse select part of sql to use as update
                const string regex = @"SELECT\s*\r\n(?<ColumnValue>.+)?\s*AS\s*(?<ColumnAlias>\[\w+\])\r\nFROM\s*(?<TableName>\[\w+\]\.\[\w+\]|\[\w+\])\s*AS\s*(?<TableAlias>\[\w+\])";
                Match match = Regex.Match(sql, regex);
                if (!match.Success)
                    throw new ArgumentException("The MemberAssignment expression could not be processed.", "binding");

                string value = match.Groups["ColumnValue"].Value;
                string alias = match.Groups["TableAlias"].Value;

                value = value.Replace(alias + ".", "");

                foreach (ObjectParameter objectParameter in selectQuery.Parameters)
                {
                    string parameterName = "p__update__" + nameCount++;

                    var parameter = updateCommand.CreateParameter();
                    parameter.ParameterName = parameterName;
                    parameter.Value = objectParameter.Value;
                    updateCommand.Parameters.Add(parameter);

                    value = value.Replace(objectParameter.Name, parameterName);
                }
                sqlBuilder.AppendFormat("[{0}] = {1}", columnName, value);
            }
            wroteSet = true;
        }

        private static Tuple<DbConnection, DbTransaction> GetStore(ObjectContext objectContext)
        {
            DbConnection dbConnection = objectContext.Connection;
            var entityConnection = dbConnection as EntityConnection;

            // by-pass entity connection
            if (entityConnection == null)
                return new Tuple<DbConnection, DbTransaction>(dbConnection, null);

            DbConnection connection = entityConnection.StoreConnection;

            // get internal transaction
            dynamic connectionProxy = new DynamicProxy(entityConnection);
            dynamic entityTransaction = connectionProxy.CurrentTransaction;
            if (entityTransaction == null)
                return new Tuple<DbConnection, DbTransaction>(connection, null);

            DbTransaction transaction = entityTransaction.StoreTransaction;
            return new Tuple<DbConnection, DbTransaction>(connection, transaction);
        }

        private static string GetSelectSql<TEntity>(ObjectQuery<TEntity> query, EntityMap entityMap, DbCommand command)
            where TEntity : class
        {
            // changing query to only select keys
            var selector = new StringBuilder(50);
            selector.Append("new(");
            foreach (var propertyMap in entityMap.KeyMaps)
            {
                if (selector.Length > 4)
                    selector.Append((", "));

                selector.Append(propertyMap.PropertyName);
            }
            selector.Append(")");

            var selectQuery = DynamicQueryable.Select(query, selector.ToString());
            var objectQuery = selectQuery as ObjectQuery;

            if (objectQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery.", "query");

            string innerJoinSql = objectQuery.ToTraceString();

            // create parameters
            foreach (var objectParameter in objectQuery.Parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = objectParameter.Name;

                // set the parameter value, ensure null values are replaced with DBNull.Value
                parameter.Value = (objectParameter.Value == null)
                    ? DBNull.Value
                    : objectParameter.Value;

                
                command.Parameters.Add(parameter);
            }

            return innerJoinSql;
        }
    }
}
