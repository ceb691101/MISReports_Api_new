using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class JobRegisterCCDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Same overflow-safe decimal reader used on the other reports (unbounded NUMBER
        // columns can exceed .NET decimal's ~28-29 digit range and make
        // Convert.ToDecimal throw).
        private static decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            OracleDecimal od = reader.GetOracleDecimal(ordinal);
            try
            {
                od = OracleDecimal.SetPrecision(od, 28);
                return od.Value;
            }
            catch (OverflowException)
            {
                return (decimal)(double)od;
            }
        }

        public List<JobRegisterCCModel> GetJobRegisterCC(DateTime fromDate, DateTime toDate, string costCtr, string jobType)
        {
            var result = new List<JobRegisterCCModel>();

            const string query = @"
                SELECT  APPLICATIONS.APPLICATION_NO,
                        APPLICATIONS.APPLICATION_Sub_Type,
                        PCESTHMT.PROJECT_NO,
                        APPLICANT.FIRST_NAME,
                        APPLICANT.LAST_NAME,
                        WIRING_LAND_DETAIL.PHASE,
                        WIRING_LAND_DETAIL.CONNECTION_TYPE,
                        WIRING_LAND_DETAIL.TARIFF_CAT_CODE,
                        WIRING_LAND_DETAIL.TARIFF_CODE,
                        WIRING_LAND_DETAIL.SERVICE_STREET_ADDRESS,
                        WIRING_LAND_DETAIL.SERVICE_SUBURB,
                        WIRING_LAND_DETAIL.SERVICE_CITY,
                        PCESTHMT.std_cost,
                        PCESTHMT.DESCR,
                        PCESTHMT.PRJ_ASS_DT,
                        (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME
                FROM      PCESTHMT
                JOIN      APPLICATIONS ON TRIM(PCESTHMT.ESTIMATE_NO) = TRIM(APPLICATIONS.APPLICATION_NO)
                JOIN      APPLICANT ON APPLICATIONS.ID_NO = APPLICANT.ID_NO
                JOIN      WIRING_LAND_DETAIL ON APPLICATIONS.APPLICATION_ID = WIRING_LAND_DETAIL.APPLICATION_ID
                WHERE     TRIM(APPLICATIONS.DEPT_ID) = TRIM(:costctr)
                  AND     PCESTHMT.PRJ_ASS_DT >= :fromdate
                  AND     PCESTHMT.PRJ_ASS_DT <  :todateexcl
                  AND     APPLICATIONS.APPLICATION_TYPE = :jobtype
                ORDER BY PCESTHMT.PROJECT_NO";

            // Fixes/notes vs. the original query:
            // 1. "AND APPLICATIONS.APPLICATION_TYPE=@JobType" used SQL Server-style '@'
            //    parameter syntax instead of the Oracle $P!{...} style used everywhere
            //    else in this query. Treated as the intended report parameter and bound
            //    as a normal Oracle bind variable (:jobtype) here.
            // 2. APPLICATIONS.DEPT_ID / gldeptm.dept_id are wrapped in TRIM() on both
            //    sides -- same fix applied to the other reports, since dept_id is a
            //    fixed-length CHAR column elsewhere in this schema and a Varchar2 bind
            //    variable otherwise gets non-padded comparison semantics against the
            //    blank-padded stored value.
            // 3. Dates are bound as real OracleDbType.Date parameters instead of
            //    TO_DATE(:param,'yyyy/mm/dd') string parsing, and toDate is treated as
            //    inclusive of the whole day (PRJ_ASS_DT < toDate + 1 day).
            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string jobTypeTrimmed = (jobType ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });
                cmd.Parameters.Add(new OracleParameter("jobtype", OracleDbType.Varchar2) { Value = jobTypeTrimmed });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new JobRegisterCCModel
                        {
                            ApplicationNo = reader["APPLICATION_NO"] == DBNull.Value ? null : reader["APPLICATION_NO"].ToString(),
                            ApplicationSubType = reader["APPLICATION_Sub_Type"] == DBNull.Value ? null : reader["APPLICATION_Sub_Type"].ToString(),
                            ProjectNo = reader["PROJECT_NO"] == DBNull.Value ? null : reader["PROJECT_NO"].ToString(),
                            FirstName = reader["FIRST_NAME"] == DBNull.Value ? null : reader["FIRST_NAME"].ToString(),
                            LastName = reader["LAST_NAME"] == DBNull.Value ? null : reader["LAST_NAME"].ToString(),
                            Phase = reader["PHASE"] == DBNull.Value ? null : reader["PHASE"].ToString(),
                            ConnectionType = reader["CONNECTION_TYPE"] == DBNull.Value ? null : reader["CONNECTION_TYPE"].ToString(),
                            TariffCatCode = reader["TARIFF_CAT_CODE"] == DBNull.Value ? null : reader["TARIFF_CAT_CODE"].ToString(),
                            TariffCode = reader["TARIFF_CODE"] == DBNull.Value ? null : reader["TARIFF_CODE"].ToString(),
                            ServiceStreetAddress = reader["SERVICE_STREET_ADDRESS"] == DBNull.Value ? null : reader["SERVICE_STREET_ADDRESS"].ToString(),
                            ServiceSuburb = reader["SERVICE_SUBURB"] == DBNull.Value ? null : reader["SERVICE_SUBURB"].ToString(),
                            ServiceCity = reader["SERVICE_CITY"] == DBNull.Value ? null : reader["SERVICE_CITY"].ToString(),
                            StdCost = SafeGetDecimal(reader, "STD_COST"),
                            Descr = reader["DESCR"] == DBNull.Value ? null : reader["DESCR"].ToString(),
                            PrjAssDt = reader["PRJ_ASS_DT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["PRJ_ASS_DT"]),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}