using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Dashboard
{
    public class SolarBulkCustomersDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true);
        }

        public SolarBulkCustomersSummary GetSummary()
        {
            var summary = new SolarBulkCustomersSummary
            {
                TotalCustomers = 0,
                NetType1Customers = 0,
                NetType2Customers = 0,
                NetType3Customers = 0,
                NetType4Customers = 0,
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    summary.TotalCustomers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type<>'0'");

                    summary.NetType1Customers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type='1'");

                    summary.NetType2Customers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type='2'");

                    summary.NetType3Customers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type='3'");

                    summary.NetType4Customers = ExecuteCount(conn,
                        "SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type='4'");
                }

                return summary;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching solar bulk customers summary");
                summary.ErrorMessage = ex.Message;
                return summary;
            }
        }

        public SolarBulkCustomersCount GetTotalCustomersCount()
        {
            return GetCountResult("SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type<>'0'");
        }

        public SolarBulkCustomersCount GetNetType1CustomersCount()
        {
            return GetCountResult("SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type='1'");
        }

        public SolarBulkCustomersCount GetNetType2CustomersCount()
        {
            return GetCountResult("SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type='2'");
        }

        public SolarBulkCustomersCount GetNetType3CustomersCount()
        {
            return GetCountResult("SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type='3'");
        }

        public SolarBulkCustomersCount GetNetType4CustomersCount()
        {
            return GetCountResult("SELECT COUNT(*) FROM customer WHERE cst_st='0' AND net_type='4'");
        }

        private SolarBulkCustomersCount GetCountResult(string sql)
        {
            var result = new SolarBulkCustomersCount
            {
                CustomersCount = 0,
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    result.CustomersCount = ExecuteCount(conn, sql);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching solar bulk customers count");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private int ExecuteCount(OleDbConnection conn, string sql)
        {
            using (var cmd = new OleDbCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt32(result);
            }
        }
    }
}