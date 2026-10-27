using MISReports_Api.Models.CustomerDashboard;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Text;

namespace MISReports_Api.DAL.CustomerDashboard
{
    public class CustomerDetailDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string ConnectionName = "Informixstndordr";

        static CustomerDetailDao()
        {
            try
            {
                // Ensure local/corrupt sqlhosts environment variables don't prevent Ifxoledbc from discovering hqinfdb10
                Environment.SetEnvironmentVariable("INFORMIXSQLHOSTS", null, EnvironmentVariableTarget.Process);
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INFORMIXSERVER")))
                {
                    Environment.SetEnvironmentVariable("INFORMIXSERVER", "hqinfdb10", EnvironmentVariableTarget.Process);
                }
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLIENT_LOCALE")))
                {
                    Environment.SetEnvironmentVariable("CLIENT_LOCALE", "en_US.CP1252", EnvironmentVariableTarget.Process);
                }
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_LOCALE")))
                {
                    Environment.SetEnvironmentVariable("DB_LOCALE", "en_US.819", EnvironmentVariableTarget.Process);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to initialize Informix process environment variables");
            }
        }

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
                              $"WHERE c.status1 = 'A' AND c.acct_number = ?";
                    }
                    else
                    {
                        sql = $"SELECT {columns} FROM stod_cust c " +
                              $"LEFT JOIN bank_name b ON c.bank_code = b.bank_code AND c.bran_code = b.bran_code " +
                              $"WHERE c.status1 = 'A'";
                    }

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 180;

                        if (hasFilter)
                        {
                            cmd.Parameters.Add(new OleDbParameter("acct_number", accNumber.Trim()));
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
            catch (OleDbException oleEx)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"OleDb Exception: {oleEx.Message} (ErrorCode: 0x{oleEx.ErrorCode:X8})");
                if (oleEx.Errors != null && oleEx.Errors.Count > 0)
                {
                    foreach (OleDbError err in oleEx.Errors)
                    {
                        sb.AppendLine($"  - Details: {err.Message} (SQLState: {err.SQLState}, NativeError: {err.NativeError})");
                    }
                }
                logger.Error(oleEx, "OleDb error while fetching standing order customer details");

                if (response.Records.Count == 0)
                {
                    response.ErrorMessage = sb.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "General error while fetching standing order customer details");

                if (response.Records.Count == 0)
                {
                    response.ErrorMessage = $"Error: {ex.Message}";
                }
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
                    string sql = "SELECT COUNT(*) FROM stod_cust WHERE status1 = 'A'";
                    using (var cmd = new OleDbCommand(sql, conn))
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
                logger.Error(ex, "Error fetching total standing order customer count (status1 = 'A')");
            }
            return 0;
        }

        public List<BankCountRecord> GetCountByBank()
        {
            var results = new List<BankCountRecord>();

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    string sql =
                        "SELECT c.bank_code, c.bran_code, " +
                        "       b.bran_name, " +
                        "       COUNT(*) AS record_count " +
                        "FROM stod_cust c " +
                        "LEFT JOIN bank_name b " +
                        "       ON c.bank_code = b.bank_code AND c.bran_code = b.bran_code " +
                        "WHERE c.status1 = 'A' " +
                        "GROUP BY c.bank_code, c.bran_code, b.bran_name " +
                        "ORDER BY record_count DESC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string bankCode = GetStringValue(reader, "bank_code");
                            string branCode = GetStringValue(reader, "bran_code");
                            string branName = GetStringValue(reader, "bran_name");

                            string label = !string.IsNullOrWhiteSpace(branName)
                                ? branName
                                : string.Format("{0} / {1}", bankCode, branCode);

                            int count = 0;
                            object rawCount = reader["record_count"];
                            if (rawCount != null && rawCount != DBNull.Value)
                                int.TryParse(rawCount.ToString(), out count);

                            results.Add(new BankCountRecord
                            {
                                BankCode  = bankCode,
                                BranCode  = branCode,
                                BankLabel = label,
                                Count     = count
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching per-bank standing order customer counts");
            }

            return results;
        }

        public List<MnthBillRecord> GetMnthBillsByAccount(string acctNumber)
        {
            var records = new List<MnthBillRecord>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT ref_id, bank_code, bran_code, acct_number, cust_fname, cust_lname, " +
                                 "address_1, address_2, address_3, bill_cycle, bill_mon, frm_date, to_date, " +
                                 "kwh_units, kwh_charge, tax, fac, payments, debit, credit, open_bal, close_bal, " +
                                 "proc_date, proc_time, reqst_stat, reqst_time, paid_amount, paid_date " +
                                 "FROM mnth_bill WHERE acct_number = ? " +
                                 "ORDER BY bill_mon DESC, ref_id DESC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.Add(new OleDbParameter("acct_number", acctNumber.Trim()));
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                records.Add(new MnthBillRecord
                                {
                                    RefId       = GetStringValue(reader, "ref_id"),
                                    BankCode    = GetStringValue(reader, "bank_code"),
                                    BranCode    = GetStringValue(reader, "bran_code"),
                                    AcctNumber  = GetStringValue(reader, "acct_number"),
                                    CustFname   = GetStringValue(reader, "cust_fname"),
                                    CustLname   = GetStringValue(reader, "cust_lname"),
                                    Address1    = GetStringValue(reader, "address_1"),
                                    Address2    = GetStringValue(reader, "address_2"),
                                    Address3    = GetStringValue(reader, "address_3"),
                                    BillCycle   = GetStringValue(reader, "bill_cycle"),
                                    BillMon     = GetStringValue(reader, "bill_mon"),
                                    FrmDate     = GetStringValue(reader, "frm_date"),
                                    ToDate      = GetStringValue(reader, "to_date"),
                                    KwhUnits    = GetStringValue(reader, "kwh_units"),
                                    KwhCharge   = GetStringValue(reader, "kwh_charge"),
                                    Tax         = GetStringValue(reader, "tax"),
                                    Fac         = GetStringValue(reader, "fac"),
                                    Payments    = GetStringValue(reader, "payments"),
                                    Debit       = GetStringValue(reader, "debit"),
                                    Credit      = GetStringValue(reader, "credit"),
                                    OpenBal     = GetStringValue(reader, "open_bal"),
                                    CloseBal    = GetStringValue(reader, "close_bal"),
                                    ProcDate    = GetStringValue(reader, "proc_date"),
                                    ProcTime    = GetStringValue(reader, "proc_time"),
                                    ReqstStat   = GetStringValue(reader, "reqst_stat"),
                                    ReqstTime   = GetStringValue(reader, "reqst_time"),
                                    PaidAmount  = GetStringValue(reader, "paid_amount"),
                                    PaidDate    = GetStringValue(reader, "paid_date"),
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error fetching mnth_bill records for account {acctNumber}");
            }
            return records;
        }

        private static OleDbConnection GetConnection()
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[ConnectionName];

            if (connectionStringSettings == null || string.IsNullOrWhiteSpace(connectionStringSettings.ConnectionString))
            {
                throw new ConfigurationErrorsException($"{ConnectionName} connection string is missing from Web.config");
            }

            return new OleDbConnection(connectionStringSettings.ConnectionString);
        }

        private static string GetStringValue(OleDbDataReader reader, string columnName)
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
