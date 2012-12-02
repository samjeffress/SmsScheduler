using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsMessages.Coordinator.Commands
{
    public class TrickleSmsWithDefinedTimeBetweenEachMessage
    {
        public Guid CoordinatorId { get; set; }

        public List<SmsData> Messages { get; set; }

        public DateTime StartTimeUTC { get; set; }

        public TimeSpan TimeSpacing { get; set; }

        public SmsMetaData MetaData { get; set; }
    }
}