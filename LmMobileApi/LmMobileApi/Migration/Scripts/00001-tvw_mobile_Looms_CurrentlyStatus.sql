CREATE VIEW
    tvw_mobile_Looms_CurrentlyStatus AS
SELECT
    LCS.LoomNo,
    CS.LoomEffByTime AS Efficiency,
    ISNULL (OC.Name, '') AS OperationName,
    ISNULL (P.PersonnelName, '') AS OperatorName,
    ISNULL (W.PersonnelName, '') AS WeaverName,
    LCS.EventID AS EventId,
    LCS.LoomSpeed
FROM
    Looms_CurrentlyStatus LCS
WITH
    (NOLOCK)
    INNER JOIN Looms L
WITH
    (NOLOCK) ON L.LoomNo = LCS.LoomNo
    AND L.IsVirtual = 0
    LEFT JOIN OperationCodes OC
WITH
    (NOLOCK) ON OC.Code = LCS.OperationCode
    LEFT JOIN Personnel P
WITH
    (NOLOCK) ON P.PersonnelID = LCS.PID
    LEFT JOIN Personnel W
WITH
    (NOLOCK) ON W.PersonnelID = LCS.WID
    INNER JOIN tvw_Analysis_CurrentShift_LoomA CS
WITH
    (NOLOCK) ON CS.LoomNo = LCS.LoomNo