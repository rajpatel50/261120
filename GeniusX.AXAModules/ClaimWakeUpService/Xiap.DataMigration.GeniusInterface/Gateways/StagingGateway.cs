namespace Xiap.DataMigration.GeniusInterface.AXACS.Gateways
{
	using System.Collections.Generic;
	using System.Data;
	using System.Data.SqlClient;
	using System.Linq;

	using Dapper;

	using Xiap.DataMigration.GeniusInterface.AXACS.Entities;

    public class StagingGateway : IStagingGateway
    {
        private readonly string _connectionString;

        #region Queries

        private const string GetPolicyReferencesQuery = @"
SELECT 
DISTINCT
PolicyNumber
FROM (
    SELECT 
        L.Policy_No AS PolicyNumber
    FROM Ref.CMSClaimToGeniusXClaim
    LEFT JOIN LIB_INCIDENT L ON LiabilityIncidentNbr = LIB_INC_NO
    WHERE 
        GeniusXClaimHandler IS NOT NULL
    AND L.Policy_No IS NOT NULL
    UNION
    SELECT 
        MN.Policy_No AS PolicyNumber
    FROM Ref.CMSClaimToGeniusXClaim
    LEFT JOIN MOTOR_NOTIFICATI MN ON MotorNotificationNbr = MOT_NOTIFCTN_NO
    JOIN MOTOR_CLAIM MC ON MC.MOT_NOTIFCTN_NO = MN.MOT_NOTIFCTN_NO
    WHERE
        GeniusXClaimHandler IS NOT NULL
    AND MN.Policy_No IS NOT NULL
) X";

		private const string GetClaimByPolicyCountQuery =
@"SELECT 
    ClaimReference 
,   PolicyNumber
FROM 
(
    SELECT 
        L.Policy_No AS PolicyNumber
   ,	CASE 
        WHEN L.FILE_STATUS = 'O' THEN 1 
        WHEN L.FILE_STATUS = 'R' THEN 1 
        ELSE 0 END AS ClaimStatus
    ,   ClaimReference
    FROM Ref.CMSClaimToGeniusXClaim
    LEFT JOIN LIB_INCIDENT L ON LiabilityIncidentNbr = LIB_INC_NO
    WHERE 
        @IncludeLiability=1
    AND GeniusXClaimHandler IS NOT NULL
    AND L.Policy_No IS NOT NULL
    UNION
    SELECT 
        MN.Policy_No AS PolicyNumber
    ,	CASE 
        WHEN MC.MOT_CLAIM_STATUS = 'O' THEN 1 
        WHEN MC.MOT_CLAIM_STATUS = 'R' THEN 1 
        ELSE 0 END AS ClaimStatus
    ,   ClaimReference
    FROM Ref.CMSClaimToGeniusXClaim
    LEFT JOIN MOTOR_NOTIFICATI MN ON MotorNotificationNbr = MOT_NOTIFCTN_NO
    JOIN MOTOR_CLAIM MC ON MC.MOT_NOTIFCTN_NO = MN.MOT_NOTIFCTN_NO
    WHERE 
        @IncludeMotor=1
    AND GeniusXClaimHandler IS NOT NULL
    AND MN.Policy_No IS NOT NULL
) X
WHERE 
    (
        (@IncludeClosed = 1 AND @IncludeOpen = 0) AND ClaimStatus = 0) 
    OR	((@IncludeClosed = 0 AND @IncludeOpen = 1) AND ClaimStatus = 1) 
    OR	((@IncludeClosed = 1 AND @IncludeOpen = 1) AND ClaimStatus IN (1,0)
    )
    AND ((@IncludePolicies = 1 AND PolicyNumber IN @PoliciesToInclude) OR @IncludePolicies = 0)
    AND ((@ExcludePolicies = 1 AND PolicyNumber NOT IN @PoliciesToExclude) OR @ExcludePolicies = 0)";

        private const string GetClaimByClaimCountQuery =
@"SELECT 
   Count(1)
FROM 
(
    SELECT 
        L.Policy_No AS PolicyNumber
     ,	CASE 
        WHEN L.FILE_STATUS = 'O' THEN 1 
        WHEN L.FILE_STATUS = 'R' THEN 1 
        ELSE 0 END AS ClaimStatus
    ,   ClaimReference
    FROM Ref.CMSClaimToGeniusXClaim
    LEFT JOIN LIB_INCIDENT L ON LiabilityIncidentNbr = LIB_INC_NO
    WHERE 
        @IncludeLiability=1
    AND LiabilityIncidentNbr > 0
    AND  GeniusXClaimHandler IS NOT NULL
    AND L.Policy_No IS NOT NULL
    AND @IncludeClaims = 1 AND ClaimReference IN @ClaimsToInclude

    UNION

    SELECT 
        MN.Policy_No AS PolicyNumber
    ,	CASE
        WHEN MC.MOT_CLAIM_STATUS = 'O' THEN 1 
        WHEN MC.MOT_CLAIM_STATUS = 'R' THEN 1 
        ELSE 0 END AS ClaimStatus
    ,   ClaimReference
    FROM Ref.CMSClaimToGeniusXClaim
    LEFT JOIN MOTOR_NOTIFICATI MN ON MotorNotificationNbr = MOT_NOTIFCTN_NO
    JOIN MOTOR_CLAIM MC ON MC.MOT_NOTIFCTN_NO = MN.MOT_NOTIFCTN_NO
    WHERE 
        @IncludeMotor=1
    AND MotorNotificationNbr > 0
    AND  GeniusXClaimHandler IS NOT NULL
    AND MN.Policy_No IS NOT NULL
    AND @IncludeClaims = 1 AND ClaimReference IN @ClaimsToInclude
) X
WHERE 
    (
       (@IncludeClosed = 1 AND ClaimStatus = 0)
    OR	(@IncludeOpen = 1 AND ClaimStatus = 1) 
    )";

		private const string GetClaimsByPolicyPagedQuery =
@"
SELECT
	TOP(@PageSize)
	Row
,	ClaimReference
,   GeniusXHeaderId
,	ClaimStatus
,	GeniusXClaimHandler
,	PolicyNumber
,	ProductType 
FROM (
	SELECT
	ROW_NUMBER() OVER(ORDER BY ClaimStatus DESC, ClaimReference) AS Row	
	,	ClaimReference
	,   GeniusXHeaderId
	,	ClaimStatus
	,	GeniusXClaimHandler
	,	PolicyNumber
	,	ProductType 
	FROM (
		SELECT 
            TOP 100 PERCENT
			R.ClaimReference
		,   R.GeniusXHeaderId
		,	CASE 
                WHEN L.FILE_STATUS = 'O' THEN 1 
                WHEN L.FILE_STATUS = 'R' THEN 1 
                ELSE 0 END AS ClaimStatus
		,	R.GeniusXClaimHandler
		,	L.POLICY_NO as PolicyNumber
        ,   L.POL_UW_YEAR
		,	'LIB' AS ProductType 
		FROM Ref.CMSClaimToGeniusXClaim R
		JOIN LIB_INCIDENT L on R.LiabilityIncidentNbr = L.LIB_INC_NO
		WHERE 
            @IncludeLiability=1
        AND R.GeniusXClaimHandler IS NOT NULL
		AND L.POLICY_NO IS NOT NULL 
		UNION
		SELECT 
            TOP 100 PERCENT
			R.ClaimReference
		,   R.GeniusXHeaderId
		,	CASE 
                WHEN MC.MOT_CLAIM_STATUS = 'O' THEN 1 
                WHEN MC.MOT_CLAIM_STATUS = 'R' THEN 1 
                ELSE 0 END AS ClaimStatus
		,	R.GeniusXClaimHandler
		,	MN.POLICY_NO as PolicyNumber
        ,   MN.POL_UW_YEAR
		,	'MOT' AS ProductType 
		FROM Ref.CMSClaimToGeniusXClaim R
		JOIN MOTOR_NOTIFICATI MN on R.MotorNotificationNbr = MN.MOT_NOTIFCTN_NO
		JOIN MOTOR_CLAIM MC ON MC.MOT_NOTIFCTN_NO = MN.MOT_NOTIFCTN_NO
		WHERE 
            @IncludeMotor=1
        AND R.GeniusXClaimHandler IS NOT NULL
		AND MN.POLICY_NO IS NOT NULL
        ORDER BY POL_UW_YEAR DESC
	) Y 
    WHERE
	(
      (@IncludeClosed = 1 AND ClaimStatus = 0)
    OR	(@IncludeOpen = 1 AND ClaimStatus = 1) 
	)
    AND ((@IncludePolicies = 1 AND PolicyNumber IN @PoliciesToInclude) OR @IncludePolicies = 0)
    AND ((@ExcludePolicies = 1 AND PolicyNumber NOT IN @PoliciesToExclude) OR @ExcludePolicies = 0)
) X
WHERE [Row] > @FirstRecord
";

        private const string GetClaimsByClaimPagedQuery =
        @"
SELECT
	TOP(@PageSize)
	Row
,	ClaimReference
,   GeniusXHeaderId
,	ClaimStatus
,	GeniusXClaimHandler
,	PolicyNumber
,	ProductType 
FROM (
	SELECT
	ROW_NUMBER() OVER(ORDER BY ClaimStatus DESC, ClaimReference) AS Row	
	,	ClaimReference
	,   GeniusXHeaderId
	,	ClaimStatus
	,	GeniusXClaimHandler
	,	PolicyNumber
	,	ProductType 
	FROM (
		SELECT 
            TOP 100 PERCENT
			R.ClaimReference
		,   R.GeniusXHeaderId
		,	CASE 
                WHEN L.FILE_STATUS = 'O' THEN 1 
                WHEN L.FILE_STATUS = 'R' THEN 1 
                ELSE 0 END AS ClaimStatus
		,	R.GeniusXClaimHandler
		,	L.POLICY_NO as PolicyNumber
        ,   L.POL_UW_YEAR
		,	'LIB' AS ProductType 
		FROM Ref.CMSClaimToGeniusXClaim R
		JOIN LIB_INCIDENT L on R.LiabilityIncidentNbr = L.LIB_INC_NO
		WHERE 
            @IncludeLiability=1
        AND R.LiabilityIncidentNbr > 0
        AND R.GeniusXClaimHandler IS NOT NULL
		AND L.POLICY_NO IS NOT NULL
        AND @IncludeClaims = 1 AND ClaimReference IN @ClaimsToInclude

		UNION

		SELECT 
            TOP 100 PERCENT
			R.ClaimReference
		,   R.GeniusXHeaderId
		,	CASE 
                WHEN MC.MOT_CLAIM_STATUS = 'O' THEN 1 
                WHEN MC.MOT_CLAIM_STATUS = 'R' THEN 1 
                ELSE 0 END AS ClaimStatus
		,	R.GeniusXClaimHandler
		,	MN.POLICY_NO as PolicyNumber
        ,   MN.POL_UW_YEAR
		,	'MOT' AS ProductType 
		FROM Ref.CMSClaimToGeniusXClaim R
		JOIN MOTOR_NOTIFICATI MN on R.MotorNotificationNbr = MN.MOT_NOTIFCTN_NO
		JOIN MOTOR_CLAIM MC ON MC.MOT_NOTIFCTN_NO = MN.MOT_NOTIFCTN_NO
		WHERE 
            @IncludeMotor=1
        AND R.MotorNotificationNbr > 0
        AND R.GeniusXClaimHandler IS NOT NULL
		AND MN.POLICY_NO IS NOT NULL
        AND @IncludeClaims = 1 AND ClaimReference IN @ClaimsToInclude

        ORDER BY POL_UW_YEAR DESC
	) Y 
    WHERE
	(
      (@IncludeClosed = 1 AND ClaimStatus = 0)
    OR	(@IncludeOpen = 1 AND ClaimStatus = 1) 
	)
) X
WHERE [Row] > @FirstRecord
";

		private const string GetClaimDetailQuery =
@"select 
	A.liabilityincidentnbr
,	A.liabilityclaimant
,	A.motornotificationnbr
,	A.motortpno
,	A.geniusxheaderid
,	A.geniusxdetailid
,	A.claimdetailtype
,	A.GeniusXClaimHandler
,	A.claimreference
,	COALESCE(B.POLICY_NO, C.POLICY_NO) as PolicyNumber 
,   D.SourceStr1 as CmsPolicySectionCode
,	D.TargetStr1 as PolicySectionCode
,   D.TargetStr3 as SectionDetailIdentifier
,	D.TargetStr2 as CoverageCode
,	CASE WHEN A.MotorNotificationNbr > 0 THEN 'MOT' ELSE 'LIB' END as ProductType
from Ref.CMSClaimToGeniusXClaim A
left join Motor_notificati B on A.MotorNotificationNbr = B.Mot_Notifctn_no
left join LIB_INCIDENT C on A.liabilityincidentnbr = C.lib_inc_no
join Ref.CodeCrossReference D on D.ListName = N'PolSecCodeToGeniusSecCvg' and D.SourceStr1 = COALESCE(B.POL_SECTION_CODE, C.POL_SECTION_CODE)
where A.GeniusXDetailID IS NOT NULL AND A.ClaimReference = @ClaimReference";
		#endregion

        public StagingGateway(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int GetClaimByPolicyCount(
            bool includeOpenClaims, bool includeClosedClaims,
            bool includeLiability, bool includeMotor,
            string[] policiesToInclude, string[] policiesToExclude)
		{
			using (var connection = new SqlConnection(_connectionString))
			{
                connection.Open();
                var result = connection.Query<Claim>(GetClaimByPolicyCountQuery,
                      new
                        {
                            IncludeLiability = includeLiability,
                            IncludeMotor = includeMotor,
                            IncludeOpen = includeOpenClaims,
                            IncludeClosed = includeClosedClaims,
                            IncludePolicies = policiesToInclude.Any(),
                            PoliciesToInclude = policiesToInclude.ToArray(),
                            ExcludePolicies = policiesToExclude.Any(),
                            PoliciesToExclude = policiesToExclude.ToArray()        
                        });

                return result.Count();
			}
		}

        public int GetClaimByClaimCount(
            bool includeOpenClaims, bool includeClosedClaims,
            bool includeLiability, bool includeMotor,
            string[] claimsToInclude, string[] claimsToExclude)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                int? result = connection.Query<int>(GetClaimByClaimCountQuery,
                     new
                     {
                         IncludeLiability = includeLiability,
                         IncludeMotor = includeMotor,
                         IncludeOpen = includeOpenClaims,
                         IncludeClosed = includeClosedClaims,
                         IncludeClaims = claimsToInclude.Any(),
                         ClaimsToInclude = claimsToInclude.ToArray(),
                         ExcludeClaims = claimsToExclude.Any(),
                         ClaimsToExclude = claimsToExclude.ToArray()
                     }).SingleOrDefault();

                return result.GetValueOrDefault(0);
            }
        }

        public IEnumerable<Claim> GetClaimsByPolicy(int size, int firstRecord, 
            bool includeOpenClaims, bool includeClosedClaims, 
            bool includeLiability, bool includeMotor,
            string[] policiesToInclude, string[] policiesToExclude)
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				connection.Open();
				return connection.Query<Claim>(
					GetClaimsByPolicyPagedQuery,
                    new
                        {
                            PageSize = size, 
                            FirstRecord = firstRecord,
                            IncludeLiability = includeLiability,
                            IncludeMotor = includeMotor,
                            IncludeOpen = includeOpenClaims,
                            IncludeClosed = includeClosedClaims,
                            IncludePolicies = policiesToInclude.Any(),
                            PoliciesToInclude = policiesToInclude.ToArray(),
                            ExcludePolicies = policiesToExclude.Any(),
                            PoliciesToExclude = policiesToExclude.ToArray()
                        },
                    null,
                    true,
                    600);
			}
		}

        public IEnumerable<Claim> GetClaimsByClaim(int size, int firstRecord, 
            bool includeOpenClaims, bool includeClosedClaims,
            bool includeLiability, bool includeMotor,
            string[] claimsToInclude, string[] claimsToExclude)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                return connection.Query<Claim>(
                    GetClaimsByClaimPagedQuery,
                    new
                    {
                        PageSize = size,
                        FirstRecord = firstRecord,
                        IncludeLiability = includeLiability,
                        IncludeMotor = includeMotor,
                        IncludeOpen = includeOpenClaims,
                        IncludeClosed = includeClosedClaims,
                        IncludeClaims = claimsToInclude.Any(),
                        ClaimsToInclude = claimsToInclude.ToArray(),
                        ExcludeClaims = claimsToExclude.Any(),
                        ClaimsToExclude = claimsToExclude.ToArray()
                    },
                    null,
                    true,
                    600);
            }
        }

		public IEnumerable<ClaimDetail> GetClaimDetails(string claimReference)
		{
			using (var connection = new SqlConnection(_connectionString))
			{
				connection.Open();
				return connection.Query<ClaimDetail>(GetClaimDetailQuery, new { ClaimReference = claimReference });
			}
  
		}

        public IEnumerable<string> GetPolicyReferences()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                return connection.Query<string>(GetPolicyReferencesQuery);
            }
        }

        private static string CreateOpenAndClosedCriteriaClause(bool includeOpenClaims, bool includeClosedClaims)
        {
            if (includeOpenClaims && includeClosedClaims) return string.Empty;
            if (includeOpenClaims) return "AND ClaimStatus = 1";
            if (includeClosedClaims) return "AND ClaimStatus = 0";
            return string.Empty;
        }

        private static string IncludePolicies(string[] policiesToInclude)
        {
            if (!policiesToInclude.Any()) return string.Empty;
            return string.Format("AND PolicyNumber IN ({0})",
                string.Join(",", policiesToInclude.Select(s => string.Format("'{0}'", s))));
        }

        private static string ExcludePolicies(string[] policiesToExclude)
        {
            if (!policiesToExclude.Any()) return string.Empty;
            return string.Format("AND PolicyNumber NOT IN ({0})",
                string.Join(",", policiesToExclude.Select(s => string.Format("'{0}'", s))));
        }


        private static string IncludeClaims(string[] claimsToInclude)
        {
            if (!claimsToInclude.Any()) return string.Empty;
            return string.Format("AND ClaimReference IN ({0})",
                string.Join(",", claimsToInclude.Select(s => string.Format("'{0}'", s))));
        }

        private static string ExcludeClaims(string[] claimsToExclude)
        {
            if (!claimsToExclude.Any()) return string.Empty;
            return string.Format("AND ClaimReference NOT IN ({0})",
                string.Join(",", claimsToExclude.Select(s => string.Format("'{0}'", s))));
        }

	}
}
