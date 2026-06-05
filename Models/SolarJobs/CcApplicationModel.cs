using System;
using Newtonsoft.Json;

namespace MISReports_Api.Models.SolarJobs
{
    public class CcApplicationModel
    {
        [JsonProperty("APPLICATION_ID")]
        public string ApplicationId { get; set; }

        [JsonProperty("APPLICATION_NO")]
        public string ApplicationNo { get; set; }

        [JsonProperty("PIV_RECEIPT_NO")]
        public string PivReceiptNo { get; set; }

        [JsonProperty("PIV_NO")]
        public string PivNo { get; set; }

        [JsonProperty("PIV_AMOUNT")]
        public decimal? PivAmount { get; set; }

        [JsonProperty("NAME")]
        public string Name { get; set; }

        [JsonProperty("APPLICATION_SUB_TYPE")]
        public string ApplicationSubType { get; set; }

        [JsonProperty("PIV1_DATE")]
        public DateTime? Piv1Date { get; set; }

        [JsonProperty("PIV1_NO")]
        public string Piv1No { get; set; }

        [JsonProperty("PIV1_RECEIPT_NO")]
        public string Piv1ReceiptNo { get; set; }

        [JsonProperty("STREET_ADDRESS")]
        public string StreetAddress { get; set; }

        [JsonProperty("SUBURB")]
        public string Suburb { get; set; }

        [JsonProperty("CITY")]
        public string City { get; set; }

        [JsonProperty("PAID_DATE")]
        public DateTime? PaidDate { get; set; }

        [JsonProperty("TARIFF_CAT_CODE")]
        public string TariffCatCode { get; set; }

        [JsonProperty("PHASE")]
        public string Phase { get; set; }

        [JsonProperty("CONNECTION_TYPE")]
        public string ConnectionType { get; set; }

        [JsonProperty("PROJECTNO")]
        public string ProjectNo { get; set; }

        [JsonProperty("ACC_NO")]
        public string AccNo { get; set; }

        [JsonProperty("CCT_NAME")]
        public string CctName { get; set; }
    }
}