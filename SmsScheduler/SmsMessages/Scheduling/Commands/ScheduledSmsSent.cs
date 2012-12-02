using System;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling.Commands
{
    public class ScheduledSmsSent
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduledSmsId { get; set; }

        public SmsConfirmationData ConfirmationData { get; set; }

        public string Number { get; set; }
    }
}