﻿using System;
using ConfigurationModels;
using NUnit.Framework;
using Raven.Client;
using Rhino.Mocks;
using SmsActioner;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using Twilio;

namespace SmsActionerTests
{
    [TestFixture]
    public class SmsServiceTestFixture
    {
        private IRavenDocStore _ravenDocStore;
        private IDocumentSession _docSession;
        private readonly SmsProviderConfiguration _twilioProvider = new SmsProviderConfiguration {SmsProvider = SmsProvider.Twilio};

        [SetUp]
        public void Setup()
        {
            _ravenDocStore = MockRepository.GenerateStub<IRavenDocStore>();
            var mockRavenStore = MockRepository.GenerateStub<IDocumentStore>();
            _docSession = MockRepository.GenerateStub<IDocumentSession>();
            _ravenDocStore.Expect(r => r.GetStore()).Return(mockRavenStore);
            _ravenDocStore.Expect(r => r.ConfigurationDatabaseName()).Return("something");
            mockRavenStore.Expect(m => m.OpenSession(Arg<string>.Is.Anything)).Return(_docSession);
        }

        [Test]
        public void SmsServiceSuccess()
        {
            _docSession.Expect(d => d.Load<SmsProviderConfiguration>("SmsProviderConfiguration")).Return(_twilioProvider);

            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message")};
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper, RavenDocStore = _ravenDocStore };

            var smsMessage = new SMSMessage { Status = "sent", Sid = "sidReceipt", DateSent = DateTime.Now, Price = 3 };
            twilioWrapper
                .Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessage);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof(SmsSent)));
            Assert.That(response.Sid, Is.EqualTo(smsMessage.Sid));
            var smsSent = response as SmsSent;
            Assert.That(smsSent.SmsConfirmationData.Receipt, Is.EqualTo(smsMessage.Sid));
            Assert.That(smsSent.SmsConfirmationData.SentAtUtc, Is.EqualTo(smsMessage.DateSent));
            Assert.That(smsSent.SmsConfirmationData.Price, Is.EqualTo(smsMessage.Price));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceSending()
        {
            _docSession.Expect(d => d.Load<SmsProviderConfiguration>("SmsProviderConfiguration")).Return(_twilioProvider);

            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper, RavenDocStore = _ravenDocStore };

            var smsMessageSending = new SMSMessage { Status = "sending", Sid = "sidReceipt" };
            twilioWrapper
                .Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof (SmsSending)));
            Assert.That(response.Sid, Is.EqualTo(smsMessageSending.Sid));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceSendingFails()
        {
            _docSession.Expect(d => d.Load<SmsProviderConfiguration>("SmsProviderConfiguration")).Return(_twilioProvider);

            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper, RavenDocStore = _ravenDocStore };

            var smsMessageSending = new SMSMessage { Status = "failed", Sid = "sidReceipt", RestException = new RestException {Code = "code", Message = "message", MoreInfo = "moreInfo", Status = "status"}};
            twilioWrapper
                .Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof(SmsFailed)));
            Assert.That(response.Sid, Is.EqualTo(smsMessageSending.Sid));
            var smsFailed = response as SmsFailed;
            Assert.That(smsFailed.Status, Is.EqualTo(smsMessageSending.RestException.Status));
            Assert.That(smsFailed.Code, Is.EqualTo(smsMessageSending.RestException.Code));
            Assert.That(smsFailed.Message, Is.EqualTo(smsMessageSending.RestException.Message));
            Assert.That(smsFailed.MoreInfo, Is.EqualTo(smsMessageSending.RestException.MoreInfo));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceRestException()
        {
            _docSession.Expect(d => d.Load<SmsProviderConfiguration>("SmsProviderConfiguration")).Return(_twilioProvider);

            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper, RavenDocStore = _ravenDocStore };

            var smsMessageSending = new SMSMessage { RestException = new RestException {Code = "code", Message = "message", MoreInfo = "moreInfo", Status = "status"}};
            twilioWrapper
                .Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof(SmsFailed)));
            Assert.That(response.Sid, Is.EqualTo(smsMessageSending.Sid));
            var smsFailed = response as SmsFailed;
            Assert.That(smsFailed.Status, Is.EqualTo(smsMessageSending.RestException.Status));
            Assert.That(smsFailed.Code, Is.EqualTo(smsMessageSending.RestException.Code));
            Assert.That(smsFailed.Message, Is.EqualTo(smsMessageSending.RestException.Message));
            Assert.That(smsFailed.MoreInfo, Is.EqualTo(smsMessageSending.RestException.MoreInfo));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceMessageQueued()
        {
            _docSession.Expect(d => d.Load<SmsProviderConfiguration>("SmsProviderConfiguration")).Return(_twilioProvider);

            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper, RavenDocStore = _ravenDocStore };

            var smsMessageSending = new SMSMessage { Status = "queued", Sid = "sidReceipt" };
            twilioWrapper
                .Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof(SmsQueued)));
            Assert.That(response.Sid, Is.EqualTo(smsMessageSending.Sid));
            twilioWrapper.VerifyAllExpectations();
        }
    }
}