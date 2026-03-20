using System;

namespace MISReports_Api.Models.Dashboard
{
    /// <summary>
    /// Represents a single bill cycle row returned from the receive_position table.
    /// </summary>
    public class SalesAndCollectionModel
    {
        public int BillCycle { get; set; }
        public decimal Collection { get; set; }
        public decimal Sales { get; set; }

        // Populated by the API — not from DB
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Wraps both Ordinary (bill_type='O') and Bulk (bill_type='B') result sets
    /// together with the max bill cycle that was used to derive the range.
    /// </summary>
    public class SalesAndCollectionRangeResult
    {
        /// <summary>
        /// The maximum bill_cycle found in receive_position (Ordinary DB).
        /// The query range is [MaxBillCycle - 7 .. MaxBillCycle].
        /// </summary>
        public int MaxBillCycle { get; set; }

        /// <summary>
        /// Ordinary customer results (bill_type = 'O'), ordered by bill_cycle ASC.
        /// </summary>
        public System.Collections.Generic.List<SalesAndCollectionModel> OrdinaryData { get; set; }
            = new System.Collections.Generic.List<SalesAndCollectionModel>();

        /// <summary>
        /// Bulk customer results (bill_type = 'B'), ordered by bill_cycle ASC.
        /// </summary>
        public System.Collections.Generic.List<SalesAndCollectionModel> BulkData { get; set; }
            = new System.Collections.Generic.List<SalesAndCollectionModel>();
    }
}