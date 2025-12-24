namespace RR.DataverseAzureSql.Common.Exceptions;

public class TableNotExistException : Exception
{
    public string TableName { get; }

    public TableNotExistException(string tableName)
        : base("The specified table does not exist.")
    {
        TableName = tableName;
    }

    public TableNotExistException(string tableName, Exception inner)
        : base("The specified table does not exist.", inner)
    {
        TableName = tableName;
    }
}

