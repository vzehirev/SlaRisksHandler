using LinqToExcel.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace XlProcessor.Models
{
    class RiskRecord
    {
        [Required, Key]
        [ExcelColumn("Vlookup Name")]
        public string VLookupName { get; set; }

        [ExcelColumn("DXC Status")]
        public string DxcStatus { get; set; }

        [ExcelColumn("Last Status Change")]
        public DateTime? LastStatusChange { get; set; }

        public TimeSpan? TotalHoldTime { get; set; }

        public ICollection<RiskStatusChange> StatusChanges { get; set; } = new HashSet<RiskStatusChange>();
    }
}