using MISReports_Api.DBAccess;
using MISReports_Api.Models.CustomerDetails;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.CustomerDetails
{
    public class PaymentInquiryDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string PmntConnectionName = "InformixPmntConsld";
        private const string BillsmryConnectionName = "InformixConnection";
        private const string BulkConnectionName = "InformixBulkConnection";

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = null;

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public LatestUpdateTimeResponse GetLatestUpdateTimes()
        {
            var response = new LatestUpdateTimeResponse
            {
                Records = new List<LatestUpdateTimeRecord>(),
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();
                    response.Records = GetLatestUpdateTimes(conn);
                }

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching latest update times");
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private List<LatestUpdateTimeRecord> GetLatestUpdateTimes(OleDbConnection conn)
        {
            var records = new List<LatestUpdateTimeRecord>();

            const string sql = @"
				SELECT a.agent,
					   a.center,
					   a.last_update,
					   b.agent_name,
					   b.center_name
				FROM latest_updates a, agent_details b
				WHERE a.agent = b.agent
				  AND a.center = b.center
				ORDER BY a.agent, a.center";

            using (var cmd = new OleDbCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    records.Add(new LatestUpdateTimeRecord
                    {
                        Agent = GetStringValue(reader, "agent"),
                        Center = GetStringValue(reader, "center"),
                        LastUpdate = GetDateTimeValue(reader, "last_update"),
                        AgentName = GetStringValue(reader, "agent_name"),
                        CenterName = GetStringValue(reader, "center_name")
                    });
                }
            }

            return records;
        }

        public PaymentInquiryResponse GetFullReport(PaymentInquiryRequest request)
        {
            return BuildPaymentInquiryResponse(request, true);
        }

        public PaymentInquiryResponse GetPaymentsOnly(PaymentInquiryRequest request)
        {
            return BuildPaymentInquiryResponse(request, false);
        }

        private PaymentInquiryResponse BuildPaymentInquiryResponse(PaymentInquiryRequest request, bool includeSummary)
        {
            var response = new PaymentInquiryResponse
            {
                PaymentRecords = new List<PaymentInquiryPaymentRecord>(),
                ErrorMessage = string.Empty
            };

            try
            {
                if (request == null)
                {
                    response.ErrorMessage = "Request body is required.";
                    return response;
                }

                var accountNumber = (request.AcctNo ?? string.Empty).Trim();
                var fromDate = NormalizeDateString(request.FromDate);

                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    response.ErrorMessage = "Account number is required.";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(fromDate))
                {
                    response.ErrorMessage = "From date is required.";
                    return response;
                }

                response.AccountNumber = accountNumber;
                response.FromDate = fromDate;
                response.ToDate = DateTime.Today.ToString("yyyy-MM-dd");

                if (includeSummary)
                {
                    LoadSummaryFields(response, accountNumber);
                }

                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();
                    response.PaymentRecords = GetPaymentRecords(conn, accountNumber, fromDate);
                }

                response.TotalAmount = response.PaymentRecords.Sum(record => GetDecimalValue(record.TransAmt));
                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, includeSummary ? "Error while fetching full payment inquiry report" : "Error while fetching payments only inquiry report");
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private void LoadSummaryFields(PaymentInquiryResponse response, string accountNumber)
        {
            response.AreaName = GetAreaName(accountNumber);

            if (IsBulkAccount(accountNumber))
            {
                response.CustomerType = "Bulk";
                LoadBulkCustomer(response, accountNumber);
            }
            else
            {
                response.CustomerType = "Ordinary";
                LoadOrdinaryCustomer(response, accountNumber);
            }
        }

        private void LoadOrdinaryCustomer(PaymentInquiryResponse response, string accountNumber)
        {
            try
            {
                using (var conn = GetConnection(BillsmryConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT cust_fname,
                               cust_lname,
                               address_1,
                               address_2,
                               address_3
                        FROM prn_dat_1
                        WHERE acct_number = ?
                        ORDER BY bill_cycle DESC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@acctNo", accountNumber);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                var firstName = GetStringValue(reader, "cust_fname");
                                var lastName = GetStringValue(reader, "cust_lname");
                                response.CustomerName = string.Join(" ", new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
                                response.Address1 = GetStringValue(reader, "address_1");
                                response.Address2 = GetStringValue(reader, "address_2");
                                response.Address3 = GetStringValue(reader, "address_3");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load ordinary customer summary for account {0}", accountNumber);
            }
        }

        private void LoadBulkCustomer(PaymentInquiryResponse response, string accountNumber)
        {
            try
            {
                using (var conn = GetConnection(BulkConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT name,
                               address_l1,
                               address_l2,
                               city
                        FROM customer
                        WHERE acc_nbr = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@acctNo", accountNumber);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                response.CustomerName = GetStringValue(reader, "name");
                                response.Address1 = GetStringValue(reader, "address_l1");
                                response.Address2 = GetStringValue(reader, "address_l2");
                                response.Address3 = GetStringValue(reader, "city");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load bulk customer summary for account {0}", accountNumber);
            }
        }

        private string GetAreaName(string accountNumber)
        {
            var areaName = GetAreaNameFromBillsmry(accountNumber);
            if (!string.IsNullOrWhiteSpace(areaName))
            {
                return areaName;
            }

            return GetAreaNameFromPayments(accountNumber);
        }

        private string GetAreaNameFromBillsmry(string accountNumber)
        {
            try
            {
                using (var conn = GetConnection(BillsmryConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT b.area_name
                        FROM cust_hq a, areas b
                        WHERE a.acct_number = ?
                          AND a.area_code = b.area_code";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@acctNo", accountNumber);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return GetStringValue(reader, "area_name");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load area name from billsmry for account {0}", accountNumber);
            }

            return string.Empty;
        }

        private string GetAreaNameFromPayments(string accountNumber)
        {
            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT b.area_name
                        FROM online_payments a, areas b
                        WHERE a.acc_no = ?
                          AND a.area_code = b.area_code
                        UNION ALL
                        SELECT b.area_name
                        FROM offline_payments a, areas b
                        WHERE a.acc_no = ?
                          AND a.area_code = b.area_code
                        UNION ALL
                        SELECT b.area_name
                        FROM online_bank a, areas b
                        WHERE a.acc_no = ?
                          AND a.area_code = b.area_code";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@acctNo1", accountNumber);
                        cmd.Parameters.AddWithValue("@acctNo2", accountNumber);
                        cmd.Parameters.AddWithValue("@acctNo3", accountNumber);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return GetStringValue(reader, "area_name");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load fallback area name from payments for account {0}", accountNumber);
            }

            return string.Empty;
        }

        private List<PaymentInquiryPaymentRecord> GetPaymentRecords(OleDbConnection conn, string accountNumber, string fromDate)
        {
            var records = new List<PaymentInquiryPaymentRecord>();

            const string sql = @"
                SELECT trans_date,
                       trans_amt,
                       center,
                       count_no,
                       pay_mode,
                       trans_time,
                       trans_type,
                       stub_no,
                       agent,
                       usr_lot
                FROM online_payments
                WHERE acc_no = ?
                  AND trans_date >= ?
                  AND trans_type = '0'
                UNION ALL
                SELECT trans_date,
                       trans_amt,
                       center,
                       count_no,
                       pay_mode,
                       trans_time,
                       trans_type,
                       stub_no,
                       agent,
                       usr_lot
                FROM offline_payments
                WHERE acc_no = ?
                  AND trans_date >= ?
                UNION ALL
                SELECT trans_date,
                       trans_amt,
                       center,
                       count_no,
                       pay_mode,
                       trans_time,
                       trans_type,
                       stub_no,
                       agent,
                       usr_lot
                FROM online_bank
                WHERE acc_no = ?
                  AND trans_date >= ?
                ORDER BY 1";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@acctNo1", accountNumber);
                cmd.Parameters.AddWithValue("@fromDate1", fromDate);
                cmd.Parameters.AddWithValue("@acctNo2", accountNumber);
                cmd.Parameters.AddWithValue("@fromDate2", fromDate);
                cmd.Parameters.AddWithValue("@acctNo3", accountNumber);
                cmd.Parameters.AddWithValue("@fromDate3", fromDate);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var transDate = GetDateValue(reader, "trans_date");
                        var transAmt = GetStringValue(reader, "trans_amt");
                        var center = GetStringValue(reader, "center");
                        var countNo = GetStringValue(reader, "count_no");
                        var payMode = GetStringValue(reader, "pay_mode");
                        var transTime = GetStringValue(reader, "trans_time");
                        var transType = GetStringValue(reader, "trans_type");
                        var stubNo = GetStringValue(reader, "stub_no");
                        var agent = GetStringValue(reader, "agent");
                        var usrLot = GetStringValue(reader, "usr_lot");

                        var agentDetails = GetAgentDetails(agent, center);
                        var counterDetails = GetCounterDetails(countNo);
                        var codeDescription = GetCodeDescription(payMode);
                        var chequeMoneyOrderNo = string.Equals(payMode, "Q", StringComparison.OrdinalIgnoreCase)
                            ? GetChequeMoneyOrderNo(accountNumber, transDate, countNo, stubNo)
                            : string.Empty;

                        records.Add(new PaymentInquiryPaymentRecord
                        {
                            TransDate = transDate,
                            TransAmt = transAmt,
                            Center = center,
                            CountNo = countNo,
                            PayMode = payMode,
                            TransTime = transTime,
                            TransType = transType,
                            StubNo = stubNo,
                            Agent = agent,
                            UsrLot = usrLot,
                            AgentName = agentDetails.AgentName,
                            CenterName = agentDetails.CenterName,
                            CounterName = counterDetails,
                            CodeDescription = codeDescription,
                            ChequeMoneyOrderNo = chequeMoneyOrderNo
                        });
                    }
                }
            }

            return records;
        }

        private (string AgentName, string CenterName) GetAgentDetails(string agent, string center)
        {
            if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(center))
            {
                return (string.Empty, string.Empty);
            }

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT agent_name,
                               center_name
                        FROM agent_details
                        WHERE agent = ?
                          AND center = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@agent", agent);
                        cmd.Parameters.AddWithValue("@center", center);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return (GetStringValue(reader, "agent_name"), GetStringValue(reader, "center_name"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load agent details for agent {0}, center {1}", agent, center);
            }

            return (string.Empty, string.Empty);
        }

        private string GetCounterDetails(string countNo)
        {
            if (string.IsNullOrWhiteSpace(countNo))
            {
                return string.Empty;
            }

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT counter,
                               counter_name
                        FROM counter_details
                        WHERE counter = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@counter", countNo);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                var counter = GetStringValue(reader, "counter");
                                var counterName = GetStringValue(reader, "counter_name");
                                return string.IsNullOrWhiteSpace(counterName) ? counter : $"{counter} - {counterName}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load counter details for counter {0}", countNo);
            }

            return string.Empty;
        }

        private string GetCodeDescription(string payMode)
        {
            if (string.IsNullOrWhiteSpace(payMode))
            {
                return string.Empty;
            }

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT code_discrp
                        FROM code_paymode
                        WHERE pay_mode = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@payMode", payMode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return GetStringValue(reader, "code_discrp");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load pay mode description for pay mode {0}", payMode);
            }

            return string.Empty;
        }

        private string GetChequeMoneyOrderNo(string accountNumber, string transDate, string countNo, string stubNo)
        {
            if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(transDate) || string.IsNullOrWhiteSpace(countNo) || string.IsNullOrWhiteSpace(stubNo))
            {
                return string.Empty;
            }

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT chq_mny_no
                        FROM chq_mnyord
                        WHERE acno_pivno = ?
                          AND trans_date = ?
                          AND count_no = ?
                          AND stub_no = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                        cmd.Parameters.AddWithValue("@transDate", transDate);
                        cmd.Parameters.AddWithValue("@countNo", countNo);
                        cmd.Parameters.AddWithValue("@stubNo", stubNo);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return GetStringValue(reader, "chq_mny_no");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load cheque money order number for account {0}", accountNumber);
            }

            return string.Empty;
        }

        private static bool IsBulkAccount(string accountNumber)
        {
            return !string.IsNullOrWhiteSpace(accountNumber) && accountNumber.Length >= 3 && accountNumber[2] == '7';
        }

        private static string NormalizeDateString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (DateTime.TryParse(value, out DateTime parsedDate))
            {
                return parsedDate.ToString("yyyy-MM-dd");
            }

            return value.Trim();
        }

        private static string GetDateValue(OleDbDataReader reader, string columnName)
        {
            object value = reader[columnName];

            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd");
            }

            if (DateTime.TryParse(value.ToString(), out DateTime parsedDateTime))
            {
                return parsedDateTime.ToString("yyyy-MM-dd");
            }

            return value.ToString().Trim();
        }

        private static decimal GetDecimalValue(string value)
        {
            return decimal.TryParse(value, out decimal parsed) ? parsed : 0m;
        }

        private static OleDbConnection GetConnection(string connectionName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionName];

            if (connectionStringSettings == null || string.IsNullOrWhiteSpace(connectionStringSettings.ConnectionString))
            {
                throw new ConfigurationErrorsException($"{connectionName} connection string is missing from Web.config");
            }

            return new OleDbConnection(connectionStringSettings.ConnectionString);
        }

        private static string GetStringValue(OleDbDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value == null || value == DBNull.Value ? string.Empty : value.ToString().Trim();
        }

        private static string GetDateTimeValue(OleDbDataReader reader, string columnName)
        {
            object value = reader[columnName];

            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (DateTime.TryParse(value.ToString(), out DateTime parsedDateTime))
            {
                return parsedDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            return value.ToString().Trim();
        }
    }
}
