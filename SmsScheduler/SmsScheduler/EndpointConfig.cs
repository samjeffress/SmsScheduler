﻿using NServiceBus;

namespace SmsScheduler
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization, AsA_Publisher
    {
        public void Init()
        {
            var configure = Configure.With()
            .DefaultBuilder()
                .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                .DefiningMessagesAs(t => t.Namespace != null && (t.Namespace.EndsWith("Messages") || t.Namespace.EndsWith("Responses")))
                .DefiningMessagesAs(t => t.Namespace == "SmsMessages")
                .DefiningMessagesAs(t => t.Namespace == "SmsTrackingMessages.Messages")
            .RunTimeoutManager()
            .Log4Net()
            .XmlSerializer()
            .MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
            .RavenPersistence()
            .Sagas()
                .RavenSagaPersister()
            .UnicastBus()
                .ImpersonateSender(false)
                .LoadMessageHandlers();
            //                .RavenSubscriptionStorage();

            Configure.Instance.Configurer.ConfigureComponent<RavenDocStore>(DependencyLifecycle.SingleInstance);

            configure.CreateBus().Start();
            //.Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());
        }
    }
}
