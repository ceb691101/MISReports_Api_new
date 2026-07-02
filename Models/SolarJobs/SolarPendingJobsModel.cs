using System;
using Newtonsoft.Json;

namespace MISReports_Api.Models.SolarJobs
{
    public class SolarPendingJobsModel
    {
        [JsonProperty("APPLICATION_ID")]
        public string ApplicationId { get; set; }

        [JsonProperty("APPLICATION_NO")]
        public string ApplicationNo { get; set; }

        [JsonProperty("SUBMIT_DATE")]
        public DateTime? SubmitDate { get; set; }

        [JsonProperty("PROJECTNO")]
        public string ProjectNo { get; set; }

        [JsonProperty("PIV_DATE")]
        public DateTime? PivDate { get; set; }

        [JsonProperty("APPLICATION_SUB_TYPE")]
        public string ApplicationSubType { get; set; }

        [JsonProperty("PAID_DATE")]
        public DateTime? PaidDate { get; set; }

        [JsonProperty("PIV2_PAID_DATE")]
        public DateTime? Piv2PaidDate { get; set; }

        [JsonProperty("EXISTING_ACC_NO")]
        public string ExistingAccNo { get; set; }

        [JsonProperty("STATUS")]
        public string Status { get; set; }

        [JsonProperty("DEPT_ID")]
        public string DeptId { get; set; }

        [JsonProperty("CCT_NAME")]
        public string CctName { get; set; }

        [JsonProperty("PROVINCE_NAME")]
        public string ProvinceName { get; set; }
    }
}
