// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    public async Task<SqlConnection> GetOpenConnectionAsync()
    {
        var connectionString = GetConnectionString();

        // Let ADO.Net handle connection pooling
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static string GetConnectionString()
    {
        return "Server=.;Database=Northwind;Trusted_Connection=True;TrustServerCertificate=True";
    }
}
