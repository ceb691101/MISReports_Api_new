using MISReports_Api.DBAccess;
using MISReports_Api.Models.CustomerDetails;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;

namespace MISReports_Api.DAL.CustomerDetails
{
    public class PaymentInquiryDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = null;

            try
            {
                using (var conn = GetConnection())
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
                using (var conn = GetConnection())
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

        private static OleDbConnection GetConnection()
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["InformixPmntConsld"];

            if (connectionStringSettings == null || string.IsNullOrWhiteSpace(connectionStringSettings.ConnectionString))
            {
                throw new ConfigurationErrorsException("InformixPmntConsld connection string is missing from Web.config");
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
