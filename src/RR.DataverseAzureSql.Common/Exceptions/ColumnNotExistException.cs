namespace RR.DataverseAzureSql.Common.Exceptions;

public class ColumnNotExistException : Exception
{
    public string TableName { get; }
    public string ColumnName { get; }

    public ColumnNotExistException(string tableName, string columnName)
        : base("The specified column does not exist.")
    {
        TableName = tableName;
        ColumnName = columnName;
    }

    public ColumnNotExistException(string tableName, string columnName, Exception inner)
        : base("The specified column does not exist.", inner)
    {
        TableName = tableName;
        ColumnName = columnName;
    }
}

