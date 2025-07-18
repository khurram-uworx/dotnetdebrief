// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

/// <summary>
/// Defines a factory interface for creating SQL database connections.
/// </summary>
public interface ISqlConnectionFactory
{
    Task<SqlConnection> GetOpenConnectionAsync();
}