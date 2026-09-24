// Copyright © Erickson Lopez. MIT License.
using System;

namespace AnotherNamespace
{
    public class SqlException : Exception
    {
    }
}

namespace AnotherNamespaceWithStr
{
    public class SqlException : Exception
    {
        public string? Number { get; set; }
    }
}

namespace AnotherNamespaceNpgsql
{
    public class NpgsqlException : Exception
    {
    }
}

namespace AnotherNamespaceMySql
{
    public class MySqlException : Exception
    {
    }
}

namespace Polly.Timeout
{
    public class TimeoutRejectedException : Exception
    {
    }
}
