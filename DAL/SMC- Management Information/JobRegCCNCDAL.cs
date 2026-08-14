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
    public class JobRegCCNCDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Overflow-safe decimal reader, same as the other reports (unbounded NUMBER
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

        // Several columns here (PIV_NO, contractor_id, account_code, account_no, etc.)
        // are exposed as strings on the model but may actually be NUMBER columns in the
        // database. Reading them via the plain reader["x"] indexer risks the same
        // decimal-overflow exception seen on other reports if the underlying value has
        // more digits than .NET decimal supports. This reads the value type-aware and
        // always returns a safe string.
        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            Type fieldType = reader.GetFieldType(ordinal);
            if (fieldType == typeof(decimal))
            {
                return SafeGetDecimal(reader, columnName)?.ToString();
            }

            return reader.GetValue(ordinal)?.ToString();
        }

        public List<JobRegCCNCModel> GetJobRegCCNC(DateTime fromDate, DateTime toDate, string costCtr)
        {
            var result = new List<JobRegCCNCModel>();

            const string query = @"
                SELECT  a.application_no,
                        c.piv_receipt_no,
                        c.PIV_NO,
                        c.Piv_amount,
                        p.account_code,
                        p.amount,
                        (b.first_name || '  ' || b.last_name) AS name,
                        b.street_address,
                        b.suburb,
                        b.city,
                        c.paid_date,
                        d.tariff_cat_code,
                        d.phase,
                        d.Connection_type,
                        e.projectno,
                        L.allocated_date,
                        L.contractor_id,
                        L.FINISHED_DATE,
                        (SELECT L1.CONTRACTOR_NAME
                           FROM SPESTCNT L1
                          WHERE L1.contractor_id = L.contractor_id
                            AND TRIM(L1.dept_id) = TRIM(:costctr)) AS CONTRACTOR_NAME,
                        (SELECT CASE WHEN T2.status IN (1) THEN ''
                                     ELSE TO_CHAR(T2.conf_dt, 'yyyy/mm/dd')
                                END
                           FROM pcesthmt T2
                          WHERE TRIM(T2.project_no) = TRIM(e.projectno)
                            AND a.dept_id = T2.dept_id) AS conf_dt,
                        (SELECT T4.account_no
                           FROM spexpjob T4
                          WHERE TRIM(T4.project_no) = TRIM(e.projectno)
                            AND e.dept_id = T4.dept_id) AS acc_no,
                        (SELECT T4.acc_created_date
                           FROM spexpjob T4
                          WHERE TRIM(T4.project_no) = TRIM(e.projectno)
                            AND e.dept_id = T4.dept_id) AS acc_date,
                        (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME
                FROM      applications a, applicant b, piv_detail c, piv_amount p, wiring_land_detail d,
                          (application_reference e
                             LEFT OUTER JOIN spestcnd L ON TRIM(e.projectno) = TRIM(L.project_no))
                WHERE     b.Id_no = a.Id_no
                  AND     TRIM(a.application_no) = TRIM(c.reference_no)
                  AND     c.Id_no = a.Id_no
                  AND     c.piv_no = p.piv_no
                  AND     c.dept_id = p.dept_id
                  AND     a.dept_id = c.dept_id
                  AND     TRIM(a.application_id) = TRIM(d.application_id)
                  AND     a.dept_id = d.dept_id
                  AND     a.application_id = e.application_id
                  AND     a.dept_id = e.dept_id
                  AND     c.reference_type IN ('EST', 'ELN')
                  AND     c.status IN ('C', 'P')
                  AND     TRIM(a.dept_id) = TRIM(:costctr)
                  AND     c.confirmed_date >= :fromdate
                  AND     c.confirmed_date <  :todateexcl
                ORDER BY a.application_id";

            // Notes vs. the new query you supplied:
            // 1. SECURITY_DEPOSIT / SER_CONN_OR_ELEC_SCH (from piv_detail c) are gone;
            //    replaced with account_code / amount from the newly joined piv_amount p
            //    table (joined on c.piv_no = p.piv_no AND c.dept_id = p.dept_id), matching
            //    the model's new AccountCode / Amount fields.
            // 2. c.reference_type filter widened from ('EST') to ('EST','ELN') as supplied.
            // 3. TRIM() kept around every dept_id comparison (a.dept_id, L1.dept_id,
            //    gldeptm.dept_id) against the :costctr bind variable, same fix applied
            //    across the other reports since dept_id is a fixed-length CHAR column.
            // 4. dept_id and dates are still bound as real OracleDbType parameters instead
            //    of inline literals. toDate is treated as inclusive of the whole day
            //    (confirmed_date < toDate + 1 day) rather than a literal "<=" bound on a
            //    date-only value, since confirmed_date may carry a time component and a
            //    literal <= could drop same-day afternoon rows. Let me know if you want a
            //    literal <= :todate instead.
            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used three times) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new JobRegCCNCModel
                        {
                            ApplicationNo = SafeGetString(reader, "application_no"),
                            PivReceiptNo = SafeGetString(reader, "piv_receipt_no"),
                            PivNo = SafeGetString(reader, "PIV_NO"),
                            PivAmount = SafeGetDecimal(reader, "Piv_amount"),
                            AccountCode = SafeGetString(reader, "account_code"),
                            Amount = SafeGetDecimal(reader, "amount"),
                            Name = SafeGetString(reader, "name"),
                            StreetAddress = SafeGetString(reader, "street_address"),
                            Suburb = SafeGetString(reader, "suburb"),
                            City = SafeGetString(reader, "city"),
                            PaidDate = reader["paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["paid_date"]),
                            TariffCatCode = SafeGetString(reader, "tariff_cat_code"),
                            Phase = SafeGetString(reader, "phase"),
                            ConnectionType = SafeGetString(reader, "Connection_type"),
                            ProjectNo = SafeGetString(reader, "projectno"),
                            AllocatedDate = reader["allocated_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["allocated_date"]),
                            ContractorId = SafeGetString(reader, "contractor_id"),
                            FinishedDate = reader["FINISHED_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FINISHED_DATE"]),
                            ContractorName = SafeGetString(reader, "CONTRACTOR_NAME"),
                            ConfDt = SafeGetString(reader, "conf_dt"),
                            AccNo = SafeGetString(reader, "acc_no"),
                            AccDate = reader["acc_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["acc_date"]),
                            CctName = SafeGetString(reader, "CCT_NAME")
                        });
                    }
                }
            }

            return result;
        }
    }
}