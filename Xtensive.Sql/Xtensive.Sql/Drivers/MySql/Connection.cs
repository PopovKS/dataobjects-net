﻿using System.Data;
using System.Data.Common;
using System.Security;
using MySql.Data.MySqlClient;

namespace Xtensive.Sql.Drivers.MySql
{
    internal class Connection : SqlConnection
    {
        private MySqlConnection underlyingConnection;
        private MySqlTransaction activeTransaction;

        /// <inheritdoc/>
        public override DbConnection UnderlyingConnection { get { return underlyingConnection; } }

        /// <inheritdoc/>
        public override DbTransaction ActiveTransaction { get { return activeTransaction; } }

        /// <inheritdoc/>
#if NET40
        [SecuritySafeCritical]
#endif
        public override DbParameter CreateParameter()
        {
            return new MySqlParameter();
        }

        /// <inheritdoc/>
#if NET40
        [SecuritySafeCritical]
#endif
        public override void BeginTransaction()
        {
            EnsureTrasactionIsNotActive();
            activeTransaction = underlyingConnection.BeginTransaction();
        }

        /// <inheritdoc/>
#if NET40
        [SecuritySafeCritical]
#endif
        public override void BeginTransaction(IsolationLevel isolationLevel)
        {
            EnsureTrasactionIsNotActive();
            activeTransaction = underlyingConnection.BeginTransaction(SqlHelper.ReduceIsolationLevel(isolationLevel));
        }

        /// <inheritdoc/>
        public override void MakeSavepoint(string name)
        {
            EnsureTransactionIsActive();
            var commandText = string.Format("SAVEPOINT {0}", name);
            using (var command = CreateCommand(commandText))
                command.ExecuteNonQuery();
        }

        /// <inheritdoc/>
        public override void RollbackToSavepoint(string name)
        {
            EnsureTransactionIsActive();
            var commandText = string.Format("ROLLBACK TO SAVEPOINT {0}; RELEASE SAVEPOINT {0};", name);
            using (var command = CreateCommand(commandText))
                command.ExecuteNonQuery();
        }

        /// <inheritdoc/>
        public override void ReleaseSavepoint(string name)
        {
            EnsureTransactionIsActive();
            var commandText = string.Format("RELEASE SAVEPOINT {0}", name);
            using (var command = CreateCommand(commandText))
                command.ExecuteNonQuery();
        }

        /// <inheritdoc/>
        protected override void ClearActiveTransaction()
        {
            activeTransaction = null;
        }


        // Constructors

#if NET40
        [SecuritySafeCritical]
#endif
        public Connection(SqlDriver driver, string connectionString)
            : base(driver, connectionString)
        {
            underlyingConnection = new MySqlConnection(connectionString);
        }
    }
}
