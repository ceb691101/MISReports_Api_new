using MISReports_Api.Models.SolarJobs;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.SolarJobs
{
    public class CcApplicationRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<CcApplicationModel>> GetApplicationsAsync(DateTime fromDate, DateTime toDate, string costctr)
        {
            var results = new List<CcApplicationModel>();

            const string sql = @"
select  a.application_id, a.application_no,c.piv_receipt_no,c.PIV_NO,c.Piv_amount , (b.first_name||'  '||b.last_name ) as name ,
( case when a.application_sub_type in ('NA' ) then 'Net Accounting'  when a.application_sub_type in ('NM' ) then 'Net Metering' when a.application_sub_type in ('NP' ) then 'Net Plus'
else null end) as application_sub_type,
(select c1.paid_date from piv_detail c1 where   trim(a.application_id)=trim(c1.reference_no)
and c1.Id_no= a.Id_no
and a.dept_id=c1.dept_id and  trim(c1.reference_type)='APP') as PIV1_date,
(select c1.PIV_NO from piv_detail c1 where   trim(a.application_no)=trim(c1.reference_no)
and c1.Id_no= a.Id_no
and a.dept_id=c1.dept_id and  trim(c1.reference_type) ='APP') as PIV1_No,
(select c1.piv_receipt_no from piv_detail c1 where   trim(a.application_id)=trim(c1.reference_no)
and c1.Id_no= a.Id_no
and a.dept_id=c1.dept_id and  trim(c1.reference_type) ='APP') as piv1_receipt_no,
 b.street_address, b.suburb, b.city , c.paid_date , d.tariff_cat_code, d.phase,d.Connection_type,e.projectno,
(select T4.existing_acc_no from WIRING_LAND_DETAIL T4 where Trim(T4.application_id)=trim(a.application_id) ) as acc_no,
(select dept_nm from gldeptm where dept_id =  :costctr) AS CCT_NAME 
from applications a, applicant b ,  piv_detail c ,  wiring_land_detail d ,  (application_reference e
	LEFT OUTER JOIN spestcnd L ON  trim(e.projectno)= trim(L.project_no) )
where b.Id_no= a.Id_no
and trim(a.application_no)=trim(c.reference_no)
and c.Id_no= a.Id_no
and a.dept_id=c.dept_id
and trim(a.application_id)=trim(d.application_id)
and a.dept_id=d.dept_id
and a.application_id = e.application_id
and a.dept_id=e.dept_id
and c.reference_type='EST'
and c.status in ('C', 'P')
and a.dept_id=  :costctr
and c.confirmed_date >=  :fromDate
and c.confirmed_date <=  :toDate
and a.application_sub_type in ('NA', 'NM','NP')
order by a.application_id";

            using (var conn = new OracleConnection(connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costctr.Trim();
                    cmd.Parameters.Add("fromDate", OracleDbType.Date).Value = fromDate.Date;
                    cmd.Parameters.Add("toDate", OracleDbType.Date).Value = toDate.Date;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new CcApplicationModel
                            {
                                ApplicationId = GetString(reader, "application_id"),
                                ApplicationNo = GetString(reader, "application_no"),
                                PivReceiptNo = GetString(reader, "piv_receipt_no"),
                                PivNo = GetString(reader, "PIV_NO"),
                                PivAmount = GetDecimal(reader, "Piv_amount"),
                                Name = GetString(reader, "name"),
                                ApplicationSubType = GetString(reader, "application_sub_type"),
                                Piv1Date = GetDateTime(reader, "PIV1_date"),
                                Piv1No = GetString(reader, "PIV1_No"),
                                Piv1ReceiptNo = GetString(reader, "piv1_receipt_no"),
                                StreetAddress = GetString(reader, "street_address"),
                                Suburb = GetString(reader, "suburb"),
                                City = GetString(reader, "city"),
                                PaidDate = GetDateTime(reader, "paid_date"),
                                TariffCatCode = GetString(reader, "tariff_cat_code"),
                                Phase = GetString(reader, "phase"),
                                ConnectionType = GetString(reader, "Connection_type"),
                                ProjectNo = GetString(reader, "projectno"),
                                AccNo = GetString(reader, "acc_no"),
                                CctName = GetString(reader, "CCT_NAME")
                            });
                        }
                    }
                }
            }

            return results;
        }

        private static string GetString(OracleDataReader reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? null : value.ToString();
        }

        private static DateTime? GetDateTime(OracleDataReader reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }

        private static decimal? GetDecimal(OracleDataReader reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? (decimal?)null : Convert.ToDecimal(value);
        }
    }
}