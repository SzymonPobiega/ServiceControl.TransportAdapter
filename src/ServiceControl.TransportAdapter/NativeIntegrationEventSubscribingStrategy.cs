﻿namespace ServiceControl.TransportAdapter
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Raw;
    using NServiceBus.Transport;

    public class NativeIntegrationEventSubscribingStrategy : IIntegrationEventSubscribingStrategy
    {
        static Type[] messageTypes =
        {
            typeof(Contracts.MessageFailed),
            typeof(Contracts.CustomCheckFailed),
            typeof(Contracts.CustomCheckSucceeded),
            typeof(Contracts.HeartbeatRestored),
            typeof(Contracts.HeartbeatStopped),
        };

        public async Task EnsureSubscribed(IRawEndpoint integrationEventSubscriber, string serviceControlInputQueue)
        {
            var transportInfrastructure = integrationEventSubscriber.Settings.Get<TransportInfrastructure>();
            var subscriptionInfra = transportInfrastructure.ConfigureSubscriptionInfrastructure();

            var factoryProperty = typeof(TransportSubscriptionInfrastructure).GetProperty("SubscriptionManagerFactory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var factoryInstance = (Func<IManageSubscriptions>) factoryProperty.GetValue(subscriptionInfra, new object[0]);
            var subscriptionManager = factoryInstance();

            foreach (var messageType in messageTypes)
            {
                await subscriptionManager.Subscribe(messageType, new ContextBag()).ConfigureAwait(false);
            }
        }
    }
}