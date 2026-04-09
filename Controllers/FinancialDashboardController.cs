using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;

namespace MISReports_Api.Controllers
{
    public class FinancialDashboardController : ApiController
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        private static readonly MemoryCache Cache = MemoryCache.Default;
        private const double CacheMinutes = 5;
        private const double CacheExpiryMinutes = 10;
        private static readonly object RefreshLock = new object();
        private static bool IsRefreshing;
        private static Timer WarmTimer;

        private class CachedValue<T>
        {
            public T Value { get; set; }
            public DateTimeOffset FetchedAt { get; set; }
        }

        private static void SetCache<T>(string key, T data)
        {
            Cache.Set(key, new CachedValue<T>
            {
                Value = data,
                FetchedAt = DateTimeOffset.UtcNow
            }, new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(CacheExpiryMinutes)
            });
        }

        private static T ExecuteWithTiming<T>(string label, Func<T> work)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return work();
            }
            finally
            {
                sw.Stop();
                Trace.TraceInformation($"{label} took {sw.ElapsedMilliseconds} ms");
            }
        }

        private static CachedValue<T> GetOrReturnStaleAndRefreshWithMetadata<T>(string key, Func<T> factory)
        {
            var cached = Cache.Get(key) as CachedValue<T>;
            var now = DateTimeOffset.UtcNow;
            var freshWindow = TimeSpan.FromMinutes(CacheMinutes);

            if (cached != null)
            {
                if (now - cached.FetchedAt < freshWindow)
                {
                    return cached;
                }

                _ = Task.Run(() =>
                {
                    try
                    {
                        var data = ExecuteWithTiming(key + "-refresh", factory);
                        SetCache(key, data);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"{key}-refresh failed: {ex.Message}");
                    }
                });

                return cached;
            }

            var freshData = ExecuteWithTiming(key + "-miss", factory);
            var result = new CachedValue<T>
            {
                Value = freshData,
                FetchedAt = DateTimeOffset.UtcNow
            };
            SetCache(key, freshData);
            return result;
        }

        private static void EnsureWarmTimer()
        {
            if (WarmTimer != null)
            {
                return;
            }

            lock (RefreshLock)
            {
                if (WarmTimer != null)
                {
                    return;
                }

                WarmTimer = new Timer(TimeSpan.FromMinutes(CacheMinutes).TotalMilliseconds)
                {
                    AutoReset = true,
                };
                WarmTimer.Elapsed += (s, e) => { _ = RefreshAllAsync(); };
                WarmTimer.Start();
                _ = RefreshAllAsync();
            }
        }

        private static async Task RefreshAllAsync()
        {
            if (IsRefreshing)
            {
                return;
            }

            lock (RefreshLock)
            {
                if (IsRefreshing)
                {
                    return;
                }

                IsRefreshing = true;
            }

            try
            {
                var totalTask = Task.Run(() => FetchPivTotal());
                var divTask = Task.Run(() => FetchPivDivision());
                var stockTotalTask = Task.Run(() => FetchStockTotal());
                var stockDivTask = Task.Run(() => FetchStockDivision());

                await Task.WhenAll(totalTask, divTask, stockTotalTask, stockDivTask);

                SetCache("piv-total", totalTask.Result);
                SetCache("piv-division", divTask.Result);
                SetCache("stock-total", stockTotalTask.Result);
                SetCache("stock-division", stockDivTask.Result);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"warm-refresh failed: {ex.Message}");
            }
            finally
            {
                lock (RefreshLock)
                {
                    IsRefreshing = false;
                }
            }
        }

        public FinancialDashboardController()
        {
            EnsureWarmTimer();
        }

        [HttpGet]
        [Route("api/piv/piv-total")]
        public IHttpActionResult GetPivTotal(bool refresh = false)
        {
            if (refresh)
            {
                Cache.Remove("piv-total");
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata("piv-total", FetchPivTotal);
            return Ok(meta);
        }

        [HttpGet]
        [Route("api/piv/piv-division")]
        public IHttpActionResult GetPivDivision(bool refresh = false)
        {
            if (refresh)
            {
                Cache.Remove("piv-division");
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata("piv-division", FetchPivDivision);
            return Ok(meta);
        }

        [HttpGet]
        [Route("api/piv/stock-total")]
        public IHttpActionResult GetStockTotal(bool refresh = false)
        {
            if (refresh)
            {
                Cache.Remove("stock-total");
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata("stock-total", FetchStockTotal);
            return Ok(meta);
        }

        [HttpGet]
        [Route("api/piv/stock-division")]
        public IHttpActionResult GetStockDivision(bool refresh = false)
        {
            if (refresh)
            {
                Cache.Remove("stock-division");
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata("stock-division", FetchStockDivision);
            return Ok(meta);
        }

        private static double FetchPivTotal()
        {
            return ExecuteWithTiming("piv-total", () =>
            {
                double total = 0;
                using (OracleConnection conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        select sum(c.grand_total) as PIV_collection
                        from piv_detail c
                        join gldeptm a on c.dept_id = a.dept_id
                        join glcompm b on a.comp_id = b.comp_id
                        where trunc(c.paid_date) = trunc(sysdate - 1)
                        and trim(c.status) in ('Q','P','F','FR','FA')
                        and a.status = 2
                        and b.status = 2
                        and (b.comp_id like 'DISCO%' or b.parent_id like 'DISCO%' or b.grp_comp like 'DISCO%')";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            total = Convert.ToDouble(reader.GetValue(0));
                        }
                    }
                }

                return total;
            });
        }

        private static List<object> FetchPivDivision()
        {
            return ExecuteWithTiming("piv-division", () =>
            {
                var result = new List<object>();
                using (OracleConnection conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        select distinct
                            (case
                                when b.comp_id in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(b.comp_id,0,1)||substr(b.comp_id,6,1)
                                when b.parent_id in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(b.parent_id,0,1)||substr(b.parent_id,6,1)
                                when b.grp_comp in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(b.grp_comp,0,1)||substr(b.grp_comp,6,1)
                                else ''
                            end) as Company,
                            sum(c.grand_total) as PIV_collection
                        from piv_detail c, gldeptm a, glcompm b
                        where trim(c.status) in ('Q', 'P','F','FR','FA')
                        and c.paid_date = (select TO_DATE((SYSDATE - 1),'dd/mm/yy') from dual)
                        and a.comp_id = b.comp_id
                        and c.dept_id = a.dept_id
                        and a.status = 2
                        and b.status = 2
                        and c.dept_id in (
                            select dept_id from gldeptm where status = 2 and comp_id in (
                                select comp_id from glcompm
                                where status = 2 and (comp_id like 'DISCO%' or parent_id like 'DISCO%' or grp_comp like 'DISCO%')
                            )
                        )
                        group by
                            (case
                                when b.comp_id in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(b.comp_id,0,1)||substr(b.comp_id,6,1)
                                when b.parent_id in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(b.parent_id,0,1)||substr(b.parent_id,6,1)
                                when b.grp_comp in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(b.grp_comp,0,1)||substr(b.grp_comp,6,1)
                                else ''
                            end)
                        order by
                            (case
                                when b.comp_id in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(b.comp_id,0,1)||substr(b.comp_id,6,1)
                                when b.parent_id in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(b.parent_id,0,1)||substr(b.parent_id,6,1)
                                when b.grp_comp in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(b.grp_comp,0,1)||substr(b.grp_comp,6,1)
                                else ''
                            end)";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new
                            {
                                company = reader.IsDBNull(0) ? "Other" : reader.GetString(0),
                                amount = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1))
                            });
                        }
                    }
                }

                return result;
            });
        }

        private static double FetchStockTotal()
        {
            return ExecuteWithTiming("stock-total", () =>
            {
                double total = 0;
                using (OracleConnection conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        select distinct sum(c.qty_on_hand * c.unit_price) as Stock_value
                        from inwrhmtm c
                        where c.status = 2 and c.grade_cd = 'NEW'
                        and c.dept_id in (
                            select dept_id from gldeptm where status = 2 and comp_id in (
                                select comp_id from glcompm
                                where status = 2 and (comp_id like 'DISCO%' or parent_id like 'DISCO%' or grp_comp like 'DISCO%')
                            )
                        )";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            total = Convert.ToDouble(reader.GetValue(0));
                        }
                    }
                }

                return total;
            });
        }

        private static List<object> FetchStockDivision()
        {
            return ExecuteWithTiming("stock-division", () =>
            {
                var result = new List<object>();
                using (OracleConnection conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        select
                            (case
                                when trim(b.comp_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.comp_id),1,1)||substr(trim(b.comp_id),6,1)
                                when trim(b.parent_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.parent_id),1,1)||substr(trim(b.parent_id),6,1)
                                when trim(b.grp_comp) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.grp_comp),1,1)||substr(trim(b.grp_comp),6,1)
                                else ''
                            end) as Company,
                            sum(c.qty_on_hand * c.unit_price) as Stock_value
                        from inwrhmtm c
                        join gldeptm a on c.dept_id = a.dept_id
                        join glcompm b on a.comp_id = b.comp_id
                        where c.status = 2
                        and c.grade_cd = 'NEW'
                        and a.status = 2
                        and b.status = 2
                        and (b.comp_id like 'DISCO%' or b.parent_id like 'DISCO%' or b.grp_comp like 'DISCO%')
                        group by
                            (case
                                when trim(b.comp_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.comp_id),1,1)||substr(trim(b.comp_id),6,1)
                                when trim(b.parent_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.parent_id),1,1)||substr(trim(b.parent_id),6,1)
                                when trim(b.grp_comp) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.grp_comp),1,1)||substr(trim(b.grp_comp),6,1)
                                else ''
                            end)
                        order by
                            (case
                                when trim(b.comp_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.comp_id),1,1)||substr(trim(b.comp_id),6,1)
                                when trim(b.parent_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.parent_id),1,1)||substr(trim(b.parent_id),6,1)
                                when trim(b.grp_comp) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.grp_comp),1,1)||substr(trim(b.grp_comp),6,1)
                                else ''
                            end)";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new
                            {
                                company = reader.IsDBNull(0) ? "Other" : reader.GetString(0),
                                amount = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1))
                            });
                        }
                    }
                }

                return result;
            });
        }
    }
}
