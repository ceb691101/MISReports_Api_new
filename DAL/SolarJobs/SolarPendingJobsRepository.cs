using MISReports_Api.Models.SolarJobs;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.SolarJobs
{
    public class SolarPendingJobsRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<SolarPendingJobsModel>> GetPendingJobsAsync(DateTime fromDate, DateTime toDate, string provinceId)
        {
            var results = new List<SolarPendingJobsModel>();

            const string sql = @"
select distinct 
    a.application_id, 
    a.application_no, 
    a.submit_date,  
    e.projectno, 
    c.piv_date,
    (case when a.application_sub_type in ('NA' ) then 'Net Accounting'
          when a.application_sub_type in ('NM' ) then 'Net Metering'
          when a.application_sub_type in ('NP' ) then 'Net Plus'
          when a.application_sub_type in ('BA' ) then 'Bulk Net Accounting'
          when a.application_sub_type in ('BM' ) then 'Bulk Net Metering'
          when a.application_sub_type in ('BP' ) then 'Bulk Net Plus'
          when a.application_sub_type in ('AC' ) then 'Net Accounting Conversion'
          when a.application_sub_type in ('PC' ) then 'Net Plus Conversion'
          when a.application_sub_type in ('NT' ) then 'Net Metering TOU'
          when a.application_sub_type in ('AT' ) then 'Net Accounting TOU'
          when a.application_sub_type in ('PP' ) then 'Net Plus Plus (With Acoount No.)'
          when a.application_sub_type in ('PB' ) then 'Bulk Net Plus Plus'
          when a.application_sub_type in ('PN' ) then 'Net Plus Plus (Without Acoount No.)'
          else null end) as application_sub_type,
    c.paid_date,  
    (select min(c2.paid_date) 
     from piv_detail c2 
     where c2.reference_type='EST' 
       and trim(c2.reference_no)=trim(a.application_no)
       and c2.status in ('C', 'P','T','M','Y')
       and c2.dept_id = a.dept_id) as piv2_paid_date,
    (select existing_acc_no 
     from WIRING_LAND_DETAIL 
     where application_id=a.application_id) as existing_acc_no,
    (case when c1.status=33 then 'Job No to be created'
          when c1.status=22 then 'Contractor to be Allocated'
          else 'Not Energized' end) as status,
    a.dept_id,
    (select dept_nm from gldeptm where dept_id = a.dept_id) AS CCT_NAME,
    (select comp_nm from glcompm where trim(comp_id) = :provinceId) AS PROVINCE_NAME
from applications a, piv_detail c, pcesthtt c1, application_reference e
where trim(a.application_no)=trim(c.reference_no)
  and a.dept_id = c.dept_id
  and a.application_id = e.application_id
  and a.dept_id=e.dept_id
  and c.reference_type='APP'
  and a.application_type in ('CR')
  and c1.status in (33,22)
  and trim(e.application_no)=trim(c1.estimate_no)
  and a.application_sub_type in ('NM','NP','NA','BM','BP','BA','NT','AC','PC','PP','PN','PB')
  and c.status in ('C', 'P','T','M','Y')
  and a.dept_id in (
      select dept_id 
      from gldeptm 
      where comp_id in (
          select comp_id 
          from glcompm 
          where trim(comp_id) = :provinceId 
             or trim(parent_id) = :provinceId
      )
  )
  and a.submit_date >= :fromDate
  and a.submit_date <= :toDate
  and not exists (
      select 1 
      from spodrcrd s 
      where trim(s.PROJECT_NO) = trim(c1.PROJECT_NO)
  )
order by e.projectno, a.application_no";

            using (var conn = new OracleConnection(connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("provinceId", OracleDbType.Varchar2).Value = provinceId.Trim();
                    cmd.Parameters.Add("fromDate", OracleDbType.Date).Value = fromDate.Date;
                    cmd.Parameters.Add("toDate", OracleDbType.Date).Value = toDate.Date;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new SolarPendingJobsModel
                            {
                                ApplicationId = GetString(reader, "application_id"),
                                ApplicationNo = GetString(reader, "application_no"),
                                SubmitDate = GetDateTime(reader, "submit_date"),
                                ProjectNo = GetString(reader, "projectno"),
                                PivDate = GetDateTime(reader, "piv_date"),
                                ApplicationSubType = GetString(reader, "application_sub_type"),
                                PaidDate = GetDateTime(reader, "paid_date"),
                                Piv2PaidDate = GetDateTime(reader, "piv2_paid_date"),
                                ExistingAccNo = GetString(reader, "existing_acc_no"),
                                Status = GetString(reader, "status"),
                                DeptId = GetString(reader, "dept_id"),
                                CctName = GetString(reader, "CCT_NAME"),
                                ProvinceName = GetString(reader, "PROVINCE_NAME")
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
            return value == DBNull.Value ? null : value.ToString().Trim();
        }

        private static DateTime? GetDateTime(OracleDataReader reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }
    }
}
