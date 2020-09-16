using System;
using System.ComponentModel.DataAnnotations;

namespace XlProcessor.Models
{
    class RiskStatusChange
    {
        [Required, Key]
        public int Id { get; set; }

        [Required]
        public string RiskRecordVLookupName { get; set; }
        public RiskRecord RiskRecord { get; set; }

        public string OldStatus { get; set; }

        [Required]
        public string NewStatus { get; set; }

        public DateTime ChangedAt { get; set; }
    }
}
