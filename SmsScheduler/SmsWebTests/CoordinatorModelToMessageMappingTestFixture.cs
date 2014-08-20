using System;
using System.Collections.Generic;
using System.Linq;
using ConfigurationModels;
using NUnit.Framework;
using Rhino.Mocks;
using SmsWeb;
using SmsWeb.Controllers;
using SmsWeb.Models;

namespace SmsWebTests
{
    [TestFixture]
    public class CoordinatorModelToMessageMappingTestFixture
    {
        [Test]
        public void MapToTrickleSpacedByTimePeriod()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    TimeSeparatorSeconds = 90,
                    Tags = "tag1, tag2",
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "confirmation",
                    UserTimeZone = "Australia/Sydney"
                };

            var mappedDateTime = DateTime.Now;
            var olsenMapping = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            olsenMapping.Expect(o => o.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone)).Return(mappedDateTime);

            var mapper = new CoordinatorModelToMessageMapping(olsenMapping);
            const string username = "username";
            var message = mapper.MapToTrickleSpacedByPeriod(model, new CountryCodeReplacement(), new List<string>(), username);

            Assert.That(message.Messages.Count, Is.EqualTo(2));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers.Split(',')[0].Trim()));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(model.Tags.Split(',').ToList().Select(t => t.Trim()).ToList()));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(mappedDateTime));
            Assert.That(message.TimeSpacing, Is.EqualTo(TimeSpan.FromSeconds(model.TimeSeparatorSeconds.Value)));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
            Assert.That(message.UserOlsenTimeZone, Is.EqualTo(model.UserTimeZone));
            Assert.That(message.Username, Is.EqualTo(username));
        }

        [Test]
        public void MapToTrickleSpacedByTimePeriodWithCountryCodeReplacement()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    TimeSeparatorSeconds = 90,
                    Tags = "tag1, tag2",
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "confirmation, anotherone; yetanother: more",
                    UserTimeZone = "Australia/Sydney"
                };

            var mappedDateTime = DateTime.Now;
            var olsenMapping = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            olsenMapping.Expect(o => o.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone)).Return(mappedDateTime);

            var mapper = new CoordinatorModelToMessageMapping(olsenMapping);
            const string username = "username";
            var message = mapper.MapToTrickleSpacedByPeriod(model, new CountryCodeReplacement { CountryCode = "+61", LeadingNumberToReplace = "0"}, new List<string>(), username);

            Assert.That(message.Messages.Count, Is.EqualTo(2));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo("+614040404040"));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(model.Tags.Split(',').ToList().Select(t => t.Trim()).ToList()));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(mappedDateTime));
            Assert.That(message.TimeSpacing, Is.EqualTo(TimeSpan.FromSeconds(model.TimeSeparatorSeconds.Value)));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
            Assert.That(message.ConfirmationEmails.Count, Is.EqualTo(4));
            Assert.That(message.ConfirmationEmails[0], Is.EqualTo("confirmation"));
            Assert.That(message.ConfirmationEmails[1], Is.EqualTo("anotherone"));
            Assert.That(message.ConfirmationEmails[2], Is.EqualTo("yetanother"));
            Assert.That(message.ConfirmationEmails[3], Is.EqualTo("more"));
            Assert.That(message.Username, Is.EqualTo(username));
        }

        [Test]
        public void MapToTrickleOverTimePeriod()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    SendAllBy = DateTime.Now.AddHours(3),
                    Tags = "tag1, tag2",
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "toby@toby.com: two",
                    UserTimeZone = "Australia/Sydney"
                };

            var mappedDateTime = DateTime.Now;
            var olsenMapping = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            olsenMapping.Expect(o => o.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone)).Return(mappedDateTime);

            var mapper = new CoordinatorModelToMessageMapping(olsenMapping);
            const string username = "username";
            var message = mapper.MapToTrickleOverPeriod(model, new CountryCodeReplacement(), new List<string>(), username);

            var coordinationDuration = model.SendAllBy.Value.Subtract(model.StartTime);
            Assert.That(coordinationDuration, Is.GreaterThan(new TimeSpan(0)));
            Assert.That(message.Messages.Count, Is.EqualTo(2));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers.Split(',')[0].Trim()));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(model.Tags.Split(',').ToList().Select(t => t.Trim().ToList())));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(mappedDateTime));
            Assert.That(message.Duration, Is.EqualTo(coordinationDuration));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
            Assert.That(message.ConfirmationEmails.Count, Is.EqualTo(2));
            Assert.That(message.ConfirmationEmails[0], Is.EqualTo("toby@toby.com"));
            Assert.That(message.ConfirmationEmails[1], Is.EqualTo("two"));
            Assert.That(message.UserOlsenTimeZone, Is.EqualTo(model.UserTimeZone));
            Assert.That(message.Username, Is.EqualTo(username));
        }

        [Test]
        public void MapToTrickleOverTimePeriodRemovingExcludedNumbers()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    SendAllBy = DateTime.Now.AddHours(3),
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "toby@toby.com",
                    UserTimeZone = "Australia/Sydney"
                };

            var mappedDateTime = DateTime.Now;
            var olsenMapping = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            olsenMapping.Expect(o => o.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone)).Return(mappedDateTime);

            var mapper = new CoordinatorModelToMessageMapping(olsenMapping);
            var excludedNumbers = new List<string> { "04040404040" };
            const string username = "username";
            var message = mapper.MapToTrickleOverPeriod(model, new CountryCodeReplacement(), excludedNumbers, username);

            var coordinationDuration = model.SendAllBy.Value.Subtract(model.StartTime);
            Assert.That(coordinationDuration, Is.GreaterThan(new TimeSpan(0)));
            Assert.That(message.Messages.Count, Is.EqualTo(1));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(null));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(mappedDateTime));
            Assert.That(message.Duration, Is.EqualTo(coordinationDuration));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
            Assert.That(message.Username, Is.EqualTo(username));
        }

        [Test]
        public void MapToTrickleOverTimePeriodWithCountryCodeReplacement()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    SendAllBy = DateTime.Now.AddHours(3),
                    Tags = "tag1, tag2",
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "toby@toby.com",
                    UserTimeZone = "Australia/Sydney"
                };

            var mappedDateTime = DateTime.Now;
            var olsenMapping = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            olsenMapping.Expect(o => o.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone)).Return(mappedDateTime);

            var mapper = new CoordinatorModelToMessageMapping(olsenMapping);
            const string username = "username";
            var message = mapper.MapToTrickleOverPeriod(model, new CountryCodeReplacement { CountryCode = "+61", LeadingNumberToReplace = "0"}, new List<string>(), username);

            var coordinationDuration = model.SendAllBy.Value.Subtract(model.StartTime);
            Assert.That(coordinationDuration, Is.GreaterThan(new TimeSpan(0)));
            Assert.That(message.Messages.Count, Is.EqualTo(2));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo("+614040404040"));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(model.Tags.Split(',').ToList().Select(t => t.Trim().ToList())));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(mappedDateTime));
            Assert.That(message.Duration, Is.EqualTo(coordinationDuration));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
            Assert.That(message.Username, Is.EqualTo(username));
        }

        [Test]
        public void MapToTrickleOverTimePeriodWithoutTags()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    SendAllBy = DateTime.Now.AddHours(3),
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "toby@toby.com",
                    UserTimeZone = "Australia/Sydney"
                };

            var mappedDateTime = DateTime.Now;
            var olsenMapping = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            olsenMapping.Expect(o => o.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone)).Return(mappedDateTime);

            var mapper = new CoordinatorModelToMessageMapping(olsenMapping);
            const string username = "username";
            var message = mapper.MapToTrickleOverPeriod(model, new CountryCodeReplacement(), new List<string>(), username);

            var coordinationDuration = model.SendAllBy.Value.Subtract(model.StartTime);
            Assert.That(coordinationDuration, Is.GreaterThan(new TimeSpan(0)));
            Assert.That(message.Messages.Count, Is.EqualTo(2));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers.Split(',')[0].Trim()));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(null));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(mappedDateTime));
            Assert.That(message.Duration, Is.EqualTo(coordinationDuration));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
            Assert.That(message.Username, Is.EqualTo(username));
        }

        [Test]
        public void MapToTrickleSetDurationBetweenMessagesRemovingExcludedNumbers()
        {
            var timeSpacing = 3;
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    TimeSeparatorSeconds = timeSpacing,
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "toby@toby.com",
                    UserTimeZone = "Australia/Sydney"
                };

            var mappedDateTime = DateTime.Now;
            var olsenMapping = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            olsenMapping.Expect(o => o.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone)).Return(mappedDateTime);

            var mapper = new CoordinatorModelToMessageMapping(olsenMapping);
            var excludedNumbers = new List<string> { "04040404040" };
            const string username = "username";
            var message = mapper.MapToTrickleSpacedByPeriod(model, new CountryCodeReplacement(), excludedNumbers, username);

            Assert.That(message.Messages.Count, Is.EqualTo(1));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(null));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(mappedDateTime));
            Assert.That(message.TimeSpacing, Is.EqualTo(new TimeSpan(0, 0, 0, timeSpacing)));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
            Assert.That(message.Username, Is.EqualTo(username));
        }

        [Test]
        public void MapToSendAllAtOnce()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    SendAllAtOnce = true,
                    Tags = "tag1, tag2",
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "confirmation, two",
                    UserTimeZone = "Australia/Sydney"
                };

            var mappedDateTime = DateTime.Now;
            var olsenMapping = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            olsenMapping.Expect(o => o.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone)).Return(mappedDateTime);

            var mapper = new CoordinatorModelToMessageMapping(olsenMapping);
            const string username = "username";
            var message = mapper.MapToSendAllAtOnce(model, new CountryCodeReplacement(), new List<string>(), username);

            Assert.That(message.Messages.Count, Is.EqualTo(2));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers.Split(',')[0].Trim()));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(model.Tags.Split(',').ToList().Select(t => t.Trim()).ToList()));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.SendTimeUtc, Is.EqualTo(mappedDateTime));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
            Assert.That(message.ConfirmationEmails.Count, Is.EqualTo(2));
            Assert.That(message.ConfirmationEmails[0], Is.EqualTo("confirmation"));
            Assert.That(message.ConfirmationEmails[1], Is.EqualTo("two"));
            Assert.That(message.UserOlsenTimeZone, Is.EqualTo(model.UserTimeZone));
            Assert.That(message.Username, Is.EqualTo(username));
        }
    }
}