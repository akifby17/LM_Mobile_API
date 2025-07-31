IF (SELECT is_broker_enabled FROM sys.databases WHERE name = DB_NAME()) = 0
BEGIN
    DECLARE @SQL NVARCHAR(MAX);

    SET @SQL = N'ALTER DATABASE [' + DB_NAME() + '] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;';

    EXEC sp_executesql @SQL;
END
