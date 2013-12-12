﻿using System;
using System.Linq;
using System.Web.Mvc;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsTrackingModels;
using SmsTrackingModels.RavenIndexs;

namespace SmsWeb.Controllers
{
    public class ReceivedMessageController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public IBus Bus { get; set; }

        public PartialViewResult Count()
        {
            using (var session = DocumentStore.GetStore().OpenSession())
            {
                var unacknowledgedSms = session.Query<SmsReceivedData, ReceivedSmsDataByAcknowledgement>().Count(r => r.Acknowledge == false);
                return PartialView("_ReceivedSmsCount", unacknowledgedSms);
            }
        }

        public PartialViewResult Index()
        {
            using (var session = DocumentStore.GetStore().OpenSession())
            {
                var unacknowledgedSms = session.Query<SmsReceivedData, ReceivedSmsDataByAcknowledgement>()
                    .Where(r => r.Acknowledge == false)
                    .ToList();
                return PartialView("_ReceivedSmsIndex", unacknowledgedSms);
            }
        }

        public ActionResult Respond(string incomingSmsId)
        {
            using (var session = DocumentStore.GetStore().OpenSession())
            {
                var incomingSms = session.Load<SmsReceivedData>(incomingSmsId);
                return View("Respond", incomingSms);
            }
        }

        [HttpPost]
        public ActionResult Respond(RespondToSmsIncoming response)
        {
            using (var session = DocumentStore.GetStore().OpenSession())
            {
                var incomingSms = session.Load<SmsReceivedData>(response.IncomingSmsId);
                Bus.Send(new SendOneMessageNow
                {
                    CorrelationId = response.IncomingSmsId,
                    SmsData = new SmsData(incomingSms.SmsData.Mobile, response.Message)
                });
                incomingSms.Acknowledge = true;
                session.SaveChanges();
                return View("Respond", incomingSms);
            }
        }

    }

    public class RespondToSmsIncoming
    {
        public Guid IncomingSmsId { get; set; }
        public string Message { get; set; }
    }
}