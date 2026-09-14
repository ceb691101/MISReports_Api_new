using MISReports_Api.Models.CustomerDashboard;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc; // ✅ Uses installed IBM INFORMIX ODBC DRIVER
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

                    string sql;
                    bool hasFilter = !string.IsNullOrWhiteSpace(accNumber);

                    if (hasFilter)
                    {
                        sql = "SELECT acct_number, cust_fname FROM mnth_bill WHERE acct_number = ?";
                    }
                    else
                    {
                        sql = "SELECT FIRST 100 acct_number, cust_fname FROM mnth_bill";
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
                                    AccNumber = GetStringValue(reader, "acct_number"),
                                    CustFname = GetStringValue(reader, "cust_fname")
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
                logger.Error(odbcEx, "Odbc error while fetching customer details");
                response.ErrorMessage = sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "General error while fetching customer details");
                response.ErrorMessage = $"Error: {ex.Message}";
            }

            return response;
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
