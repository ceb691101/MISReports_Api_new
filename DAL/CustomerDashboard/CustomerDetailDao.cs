using MISReports_Api.Models.CustomerDashboard;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Text;

namespace MISReports_Api.DAL.CustomerDashboard
{
    public class CustomerDetailDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string ConnectionName = "Informixstndordr";

        public CustomerDetailResponse GetCustomerDetails(string accNumber = null)
        {
            var response = new CustomerDetailResponse
            {
                Records = new List<CustomerDetailRecord>(),
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    bool hasFilter = !string.IsNullOrWhiteSpace(accNumber);

                    string columns = "c.bank_code, c.bran_code, c.acct_number, c.bill_cycle, c.status1, " +
                                     "c.last_proc_date, c.last_out_bal, c.calc_cycle, c.new_stat, " +
                                     "b.bran_name, b.bran_add1, b.bran_add2, b.bran_add3, b.bran_telno, b.bran_email";

                    string sql;
                    if (hasFilter)
                    {
                        sql = $"SELECT {columns} FROM stod_cust c " +
                              $"LEFT JOIN bank_name b ON c.bank_code = b.bank_code AND c.bran_code = b.bran_code " +
                              $"WHERE c.status1 = 'Q' AND c.acct_number = ?";
                    }
                    else
                    {
                        sql = $"SELECT FIRST 100 {columns} FROM stod_cust c " +
                              $"LEFT JOIN bank_name b ON c.bank_code = b.bank_code AND c.bran_code = b.bran_code " +
                              $"WHERE c.status1 = 'Q'";
                    }

                    using (var cmd = new OdbcCommand(sql, conn))
                    {
                        if (hasFilter)
                        {
                            cmd.Parameters.Add(new OdbcParameter("acct_number", accNumber.Trim()));
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                response.Records.Add(new CustomerDetailRecord
                                {
                                    AcctNumber = GetStringValue(reader, "acct_number"),
                                    BillCycle = GetStringValue(reader, "bill_cycle"),
                                    Status1 = GetStringValue(reader, "status1"),
                                    BankCode = GetStringValue(reader, "bank_code"),
                                    BranCode = GetStringValue(reader, "bran_code"),
                                    BranName = GetStringValue(reader, "bran_name"),
                                    LastProcDate = GetStringValue(reader, "last_proc_date"),
                                    LastOutBal = GetStringValue(reader, "last_out_bal"),
                                    CalcCycle = GetStringValue(reader, "calc_cycle"),
                                    NewStat = GetStringValue(reader, "new_stat"),
                                    BranAdd1 = GetStringValue(reader, "bran_add1"),
                                    BranAdd2 = GetStringValue(reader, "bran_add2"),
                                    BranAdd3 = GetStringValue(reader, "bran_add3"),
                                    BranTelno = GetStringValue(reader, "bran_telno"),
                                    BranEmail = GetStringValue(reader, "bran_email")
                                });
                            }
                        }
                    }
                }
            }
            catch (OdbcException odbcEx)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Odbc Exception: {odbcEx.Message} (ErrorCode: 0x{odbcEx.ErrorCode:X8})");
                if (odbcEx.Errors != null && odbcEx.Errors.Count > 0)
                {
                    foreach (OdbcError err in odbcEx.Errors)
                    {
                        sb.AppendLine($"  - Details: {err.Message} (SQLState: {err.SQLState}, NativeError: {err.NativeError})");
                    }
                }
                logger.Error(odbcEx, "Odbc error while fetching standing order customer details");
                response.ErrorMessage = sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "General error while fetching standing order customer details");
                response.ErrorMessage = $"Error: {ex.Message}";
            }

            return response;
        }

        public int GetTotalCustomerCount()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM stod_cust WHERE status1 = 'Q'";
                    using (var cmd = new OdbcCommand(sql, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out int total))
                        {
                            return total;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching total standing order customer count (status1 = 'Q')");
            }
            return 0;
        }

        private static OdbcConnection GetConnection()
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[ConnectionName];

            if (connectionStringSettings == null || string.IsNullOrWhiteSpace(connectionStringSettings.ConnectionString))
            {
                throw new ConfigurationErrorsException($"{ConnectionName} connection string is missing from Web.config");
            }

            return new OdbcConnection(connectionStringSettings.ConnectionString);
        }

        private static string GetStringValue(OdbcDataReader reader, string columnName)
        {
            try
            {
                object value = reader[columnName];
                return value == null || value == DBNull.Value ? string.Empty : value.ToString().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
