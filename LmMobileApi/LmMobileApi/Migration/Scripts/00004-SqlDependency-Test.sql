-- SqlDependency kurulumu test script'i

PRINT '=== SQL DEPENDENCY TEST SCRIPT ==='
PRINT 'Testing SqlDependency setup and configuration...'
PRINT ''

-- 1. Service Broker durumu kontrolü
PRINT '1. Service Broker Status Check:'
IF (SELECT is_broker_enabled FROM sys.databases WHERE name = DB_NAME()) = 1
    PRINT '   ✓ Service Broker is ENABLED'
ELSE
    PRINT '   ✗ Service Broker is DISABLED - SqlDependency will not work!'

-- 2. Temel tablo varlık kontrolü
PRINT ''
PRINT '2. Base Table Existence Check:'
IF OBJECT_ID('dbo.Looms_CurrentlyStatus', 'U') IS NOT NULL
    PRINT '   ✓ Base table Looms_CurrentlyStatus exists'
ELSE
    PRINT '   ✗ Base table Looms_CurrentlyStatus NOT FOUND!'

-- 3. View varlık kontrolü
PRINT ''
PRINT '3. View Existence Check:'
IF OBJECT_ID('dbo.tvw_mobile_Looms_CurrentlyStatus', 'V') IS NOT NULL
    PRINT '   ✓ View tvw_mobile_Looms_CurrentlyStatus exists'
ELSE
    PRINT '   ✗ View tvw_mobile_Looms_CurrentlyStatus NOT FOUND!'

-- 4. İzin kontrolü
PRINT ''
PRINT '4. Permission Check:'
BEGIN TRY
    -- Test query for base table
    IF OBJECT_ID('dbo.Looms_CurrentlyStatus', 'U') IS NOT NULL
    BEGIN
        DECLARE @TestCount INT
        SELECT TOP 1 @TestCount = COUNT(*) FROM dbo.Looms_CurrentlyStatus WITH (NOLOCK)
        PRINT '   ✓ SELECT permission on base table works'
    END
    
    -- Test query for view
    IF OBJECT_ID('dbo.tvw_mobile_Looms_CurrentlyStatus', 'V') IS NOT NULL
    BEGIN
        SELECT TOP 1 @TestCount = COUNT(*) FROM dbo.tvw_mobile_Looms_CurrentlyStatus WITH (NOLOCK)
        PRINT '   ✓ SELECT permission on view works'
    END
END TRY
BEGIN CATCH
    PRINT '   ✗ Permission test failed: ' + ERROR_MESSAGE()
END CATCH

-- 5. SqlDependency test sorgusu
PRINT ''
PRINT '5. SqlDependency Query Test:'
BEGIN TRY
    IF OBJECT_ID('dbo.Looms_CurrentlyStatus', 'U') IS NOT NULL
    BEGIN
        -- Bu sorgu SqlDependency tarafından kullanılacak
        DECLARE @TestResult TABLE (
            LoomNo NVARCHAR(50),
            EventID INT,
            LoomSpeed INT,
            PID INT,
            WID INT,
            OperationCode NVARCHAR(50),
            ShiftNo INT,
            ShiftPickCounter BIGINT,
            StyleWorkOrderNo NVARCHAR(50),
            WarpWorkOrderNo NVARCHAR(50)
        )
        
        INSERT INTO @TestResult
        SELECT TOP 5 
            LoomNo, EventID, LoomSpeed, PID, WID, OperationCode, 
            ShiftNo, ShiftPickCounter, StyleWorkOrderNo, WarpWorkOrderNo 
        FROM dbo.Looms_CurrentlyStatus WITH (NOLOCK)
        
        DECLARE @RecordCount INT
        SELECT @RecordCount = COUNT(*) FROM @TestResult
        PRINT '   ✓ SqlDependency base query test successful - Found ' + CAST(@RecordCount AS NVARCHAR(10)) + ' records'
    END
END TRY
BEGIN CATCH
    PRINT '   ✗ SqlDependency query test failed: ' + ERROR_MESSAGE()
END CATCH

-- 6. Service Queue kontrolü
PRINT ''
PRINT '6. Service Queue Check:'
IF EXISTS (SELECT * FROM sys.service_queues WHERE schema_id = SCHEMA_ID('dbo'))
    PRINT '   ✓ Service queues are available'
ELSE
    PRINT '   ! No service queues found (this is normal before first SqlDependency.Start())'

-- 7. Connection string önerileri
PRINT ''
PRINT '7. Connection String Recommendations:'
PRINT '   • Include TrustServerCertificate=true'
PRINT '   • Include MultipleActiveResultSets=true'
PRINT '   • Set appropriate timeout values'
PRINT '   • Example: "Server=.;Database=YourDB;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;"'

PRINT ''
PRINT '=== TEST COMPLETED ==='
PRINT 'If all checks show ✓, SqlDependency should work correctly.'
PRINT 'If any checks show ✗, please fix those issues before using SqlDependency.' 