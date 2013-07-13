using System;
using System.Configuration;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Client.Indexes;
using SmsTrackingModels;

namespace SmsWeb
{
    public interface IRavenDocStore
    {
        IDocumentStore GetStore();
    }

    public class RavenDocStore : IRavenDocStore
    {
        private readonly IDocumentStore _documentStore;
        public RavenDocStore()
        {
            _documentStore = new DocumentStore { Url = ConfigurationManager.AppSettings["RAVENHQ_CONNECTION_STRING"], DefaultDatabase = "SmsTracking", ResourceManagerId = Guid.NewGuid() };
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("Configuration");
            _documentStore.DatabaseCommands.EnsureDatabaseExists("SmsTracking");
            IndexCreation.CreateIndexes(typeof(ScheduleMessagesInCoordinatorIndex).Assembly, _documentStore);
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }
    }
}