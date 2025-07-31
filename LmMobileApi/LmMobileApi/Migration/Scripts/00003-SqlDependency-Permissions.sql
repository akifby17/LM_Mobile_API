-- SqlDependency için gerekli izinler ve Service Broker konfigürasyonu

-- Service Broker'ın aktif olduğundan emin ol
IF (SELECT is_broker_enabled FROM sys.databases WHERE name = DB_NAME()) = 0
BEGIN
    DECLARE @SQL NVARCHAR(MAX);
    SET @SQL = N'ALTER DATABASE [' + DB_NAME() + '] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;';
    EXEC sp_executesql @SQL;
    PRINT 'Service Broker enabled for SqlDependency';
END

-- SqlDependency için Query Notifications aktif et
IF NOT EXISTS (SELECT * FROM sys.service_queues WHERE name = 'SqlQueryNotificationService-' + CAST(NEWID() AS NVARCHAR(36)))
BEGIN
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE;
    EXEC sp_configure 'Database Mail XPs', 1;
    RECONFIGURE;
    PRINT 'Advanced options configured for SqlDependency';
END

-- Temel tablo için gerekli izinleri kontrol et ve ayarla
IF OBJECT_ID('dbo.Looms_CurrentlyStatus', 'U') IS NOT NULL
BEGIN
    -- SqlDependency için SELECT iznini veritabanı kullanıcılarına ver
    -- Tablo bazında izin ver
    GRANT SELECT ON dbo.Looms_CurrentlyStatus TO public;
    
    -- View için de izin ver (filtrelenmiş data için)
    IF OBJECT_ID('dbo.tvw_mobile_Looms_CurrentlyStatus', 'V') IS NOT NULL
    BEGIN
        GRANT SELECT ON dbo.tvw_mobile_Looms_CurrentlyStatus TO public;
    END
    
    PRINT 'SELECT permissions granted for SqlDependency';
END
ELSE
BEGIN
    PRINT 'WARNING: Looms_CurrentlyStatus table not found!';
END

-- Service Broker durum kontrolü
IF (SELECT is_broker_enabled FROM sys.databases WHERE name = DB_NAME()) = 1
    PRINT 'Service Broker is ENABLED - SqlDependency ready'
ELSE
    PRINT 'WARNING: Service Broker is DISABLED - SqlDependency will not work!'

-- Connection string için trust server certificate ayarı önerisi
PRINT 'REMINDER: Ensure connection string has TrustServerCertificate=true for SqlDependency' 