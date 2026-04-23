using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;

namespace MISReports_Api.DAL.Dashboard
{
    public class KioskCollectionDao
    {
        private readonly string _connectionString;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public KioskCollectionDao()
        {
            var connection = ConfigurationManager.ConnectionStrings["InformixPosPayment"];

            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
            {
                throw new ConfigurationErrorsException("InformixPosPayment connection string is missing or empty in configuration.");
            }

            _connectionString = connection.ConnectionString;
        }

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = null;

            try
            {
                using (var conn = new OdbcConnection(_connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (OdbcException ex)
            {
                var message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Odbc connection failed with no message."
                    : ex.Message;

                errorMessage = $"{message} (HResult: 0x{ex.ErrorCode:X8})";
                logger.Error(ex, "Kiosk POS DB connection test failed (Odbc).");
                return false;
            }
            catch (Exception ex)
            {
                var message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Database connection failed with no message."
                    : ex.Message;

                errorMessage = $"{message} (HResult: 0x{ex.HResult:X8})";
                logger.Error(ex, "Kiosk POS DB connection test failed.");
                return false;
            }
        }

        public List<KioskCollectionModel> GetKioskCollection(string userId)
        {
            var rows = new List<KioskCollectionModel>();

            try
            {
                var toDate = DateTime.Today.AddDays(-1);
                var fromDate = toDate.AddDays(-6);
                logger.Info($"=== START GetKioskCollection userId={userId}, from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd} ===");
                //logger.Info($"=== START GetKioskCollection userId={userId}, from {fromDate:dd-MM-yyyy} to {toDate:dd-MM-yyyy} ===");

                rows = QueryKioskCollection(userId: userId);

                logger.Info($"=== END GetKioskCollection (Success) - {rows.Count} records ===");
                return rows;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetKioskCollection");
                throw;
            }
        }

        private List<KioskCollectionModel> QueryKioskCollection(string userId)
        {
            var rows = new List<KioskCollectionModel>();

                        // Match financial dashboard logic: use DB-side rolling window for the last 7 days ending yesterday.
            const string sql = @"
                                SELECT DATE(trans_date) AS trans_date,
                       SUM(trans_amt) AS collection
                FROM   cus_tran
                                WHERE  userid = ?
                                    AND  trans_date >= TODAY - 7
                                    AND  trans_date <  TODAY
                  AND  bill_type = 'O'
                GROUP BY 1
                ORDER BY 1";

            using (var conn = new OdbcConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new OdbcCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new KioskCollectionModel
                            {
                                TransDate = GetDateStringValue(reader, "trans_date"),
                                CollectionAmount = GetLongValue(reader, "collection"),
                                ErrorMessage = string.Empty
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private string GetDateStringValue(OdbcDataReader reader, string column)
        {
            try
            {
                var value = reader[column];
                if (value == DBNull.Value)
                    return string.Empty;

                return Convert.ToDateTime(value).ToString("yyyy-MM-dd");
                //return Convert.ToDateTime(value).ToString("dd-MM-yyyy");
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return string.Empty;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid date format in column '{column}'");
                return string.Empty;
            }
        }

        private long GetLongValue(OdbcDataReader reader, string column)
        {
            try
            {
                var value = reader[column];
                return value == DBNull.Value ? 0L : Convert.ToInt64(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return 0L;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid whole number format in column '{column}'");
                return 0L;
            }
        }
    }
}
