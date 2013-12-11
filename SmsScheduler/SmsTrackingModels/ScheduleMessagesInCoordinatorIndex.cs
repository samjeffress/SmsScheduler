using System;
using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace SmsTrackingModels
{
    public class ScheduleMessagesInCoordinatorIndex : AbstractIndexCreationTask<ScheduleTrackingData>
    {
        public ScheduleMessagesInCoordinatorIndex()
        {
            Map = schedules => from schedule in schedules
                               select new
                                   {
                                       CoordinatorId = schedule.CoordinatorId.ToString(),
                                       MessageStatus = schedule.MessageStatus.ToString(),
                                       ScheduleId = schedule.ScheduleId.ToString(),
                                       ScheduleTimeUtc = schedule.ScheduleTimeUtc
                                   };

            Indexes.Add(s => s.CoordinatorId, FieldIndexing.Analyzed);
            Indexes.Add(s => s.MessageStatus, FieldIndexing.Analyzed);
            Indexes.Add(s => s.ScheduleId, FieldIndexing.Analyzed);
            Indexes.Add(s => s.ScheduleTimeUtc, FieldIndexing.Default);
        }
    }

    public class CoordinatorTrackingDataByDate : AbstractIndexCreationTask<CoordinatorTrackingData>
    {
        public CoordinatorTrackingDataByDate()
        {
            Map = coordinators => from coordinator in coordinators
                                  select new
                                      {
                                          CreationDate = coordinator.CreationDateUtc,
                                          Status = coordinator.CurrentStatus
                                      };
            Indexes.Add(c => c.CreationDateUtc, FieldIndexing.Default);
            Indexes.Add(c => c.CurrentStatus, FieldIndexing.Default);
        }
    }

    public class ReceivedSmsDataByAcknowledgement : AbstractIndexCreationTask<SmsReceivedData>
    {
        public ReceivedSmsDataByAcknowledgement()
        {
            Map = smsReceivedDatas => from receivedData in smsReceivedDatas
                                  select new
                                      {
                                          CreationDate = receivedData.SmsConfirmationData.SentAtUtc,
                                          receivedData.Acknowledge
                                      };
            Indexes.Add(c => c.SmsConfirmationData.SentAtUtc, FieldIndexing.Default);
            Indexes.Add(c => c.Acknowledge, FieldIndexing.Default);
        }
    }
}