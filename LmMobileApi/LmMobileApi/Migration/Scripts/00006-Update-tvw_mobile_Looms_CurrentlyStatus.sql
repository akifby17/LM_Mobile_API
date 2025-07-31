---- Update the Loom monitoring view to include additional properties
---- for the enhanced filtering and JSON response structure

--ALTER VIEW tvw_mobile_Looms_CurrentlyStatus AS
--SELECT
--    LCS.LoomNo,
--    CS.LoomEffByTime AS Efficiency,
--    ISNULL(OC.Name, '') AS OperationName,
--    ISNULL(P.PersonnelName, '') AS OperatorName,
--    ISNULL(W.PersonnelName, '') AS WeaverName,
--    LCS.EventID AS EventId,
--    LCS.LoomSpeed,
    
--    -- Additional properties for enhanced filtering
--    ISNULL(H.HallName, '') AS HallName,
--    ISNULL(LM.MarkName, '') AS MarkName,
--    ISNULL(LM.ModelName, '') AS ModelName,
--    ISNULL(PG.GroupName, '') AS GroupName,
--    ISNULL(PC.ClassName, '') AS ClassName,
--    ISNULL(WA.WarpName, '') AS WarpName,
--    ISNULL(WO.VariantNo, '') AS VariantNo,
--    ISNULL(ST.StyleName, '') AS StyleName,
    
--    -- Efficiency and duration properties
--    CS.LoomEffByTime AS WeaverEff,
--    ISNULL(FORMAT(LCS.EventDuration, 'HH\:mm'), '00:00') AS EventDuration,
--    ISNULL(LCS.ProductedLength, 0) AS ProductedLength,
--    ISNULL(WO.TotalLength, 0) AS TotalLength,
--    ISNULL(ET.EventNameTR, '') AS EventNameTR,
--    ISNULL(FORMAT(LCS.OpDuration, 'd\d\ HH\:mm'), '00:00') AS OpDuration

--FROM
--    Looms_CurrentlyStatus LCS WITH (NOLOCK)
--    INNER JOIN Looms L WITH (NOLOCK) ON L.LoomNo = LCS.LoomNo AND L.IsVirtual = 0
--    LEFT JOIN OperationCodes OC WITH (NOLOCK) ON OC.Code = LCS.OperationCode
--    LEFT JOIN Personnel P WITH (NOLOCK) ON P.PersonnelID = LCS.PID
--    LEFT JOIN Personnel W WITH (NOLOCK) ON W.PersonnelID = LCS.WID
--    INNER JOIN tvw_Analysis_CurrentShift_LoomA CS WITH (NOLOCK) ON CS.LoomNo = LCS.LoomNo
    
--    -- Additional joins for new properties
--    LEFT JOIN Halls H WITH (NOLOCK) ON H.HallID = L.HallID
--    LEFT JOIN LoomMarks LM WITH (NOLOCK) ON LM.LoomMarkID = L.LoomMarkID
--    LEFT JOIN ProductGroups PG WITH (NOLOCK) ON PG.GroupID = L.GroupID
--    LEFT JOIN ProductClasses PC WITH (NOLOCK) ON PC.ClassID = L.ClassID
--    LEFT JOIN WarpWorkOrders WO WITH (NOLOCK) ON WO.WarpWorkOrderNo = LCS.WarpWorkOrderNo
--    LEFT JOIN Warps WA WITH (NOLOCK) ON WA.WarpID = WO.WarpID
--    LEFT JOIN Styles ST WITH (NOLOCK) ON ST.StyleID = WO.StyleID
--    LEFT JOIN EventTypes ET WITH (NOLOCK) ON ET.EventID = LCS.EventID

---- Note: This script assumes the existence of related tables like Halls, LoomMarks, ProductGroups, etc.
---- If some of these tables don't exist in your database, you may need to adjust the JOIN statements
---- or provide alternative ways to get this data (like hardcoded values or computed columns)