-- Bu script sadece mevcut tabloları kontrol eder
-- Eğer tablolar yoksa bu script çalışır, varsa pas geçer

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
    PRINT 'Users tablosu bulunamadı.'
END
ELSE
BEGIN
    PRINT 'Users tablosu mevcut ✅'
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Personnel')
BEGIN
    PRINT 'Personnel tablosu bulunamadı.'
END
ELSE
BEGIN
    PRINT 'Personnel tablosu mevcut ✅'
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Looms_CurrentlyStatus')
BEGIN
    PRINT 'Looms_CurrentlyStatus tablosu bulunamadı.'
END
ELSE
BEGIN
    PRINT 'Looms_CurrentlyStatus tablosu mevcut ✅'
END

IF (SELECT is_broker_enabled FROM sys.databases WHERE name = DB_NAME()) = 0
BEGIN
    PRINT 'SQL Server Broker kapalı. SignalR için açılması gerekiyor.'
    DECLARE @SQL VARCHAR(MAX) = ''
    SET @SQL += 'ALTER DATABASE ';
    SET @SQL +=  DB_NAME();
    SET @SQL += ' SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;';
    EXEC (@SQL)
    PRINT 'SQL Server Broker etkinleştirildi ✅'
END
ELSE
BEGIN
    PRINT 'SQL Server Broker zaten etkin ✅'
END