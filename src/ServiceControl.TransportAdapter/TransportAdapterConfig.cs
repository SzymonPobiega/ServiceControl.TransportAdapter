﻿namespace ServiceControl.TransportAdapter
{
    using System;
    using NServiceBus;
    using NServiceBus.Transport;

    /// <summary>
    /// Configures the ServiceControl transport adapter.
    /// </summary>
    /// <typeparam name="TFront">Endpoints' transport.</typeparam>
    /// <typeparam name="TBack">ServiceControl transport.</typeparam>
    public class TransportAdapterConfig<TFront, TBack>
        where TFront : TransportDefinition, new()
        where TBack : TransportDefinition, new()
    {
        internal string Name { get; }
        internal IIntegrationEventPublishingStrategy IntegrationEventPublishingStrategy { get; private set; } = new NullIntegrationEventPublishingStrategy();
        internal IIntegrationEventSubscribingStrategy IntegrationEventSubscribingStrategy { get; private set; } = new NullIntegrationEventSubscribingStrategy();

        internal Action<TransportExtensions<TFront>> FrontendTransportCustomization { get; private set; } = e => { };
        internal Action<TransportExtensions<TBack>> BackendTransportCustomization { get; private set; }= e => { };

        /// <summary>
        /// Gets or sets the endpoint-side error queue -- the error queue configured in the endpoints. Defaults to error.
        /// </summary>
        public string EndpointSideErrorQueue { get; set; } = "error";

        /// <summary>
        /// Gets or sets the ServiceControl-side error queue -- the error queue configured in ServiceControl. Defaults to error.
        /// </summary>
        public string ServiceControlSideErrorQueue { get; set; } = "error";

        /// <summary>
        /// Gets or sets the endpoint-side audit queue -- the audit queue configured in the endpoints. Defaults to audit.
        /// </summary>
        public string EndpointSideAuditQueue { get; set; } = "audit";

        /// <summary>
        /// Gets or sets the ServiceControl-side audit queue -- the error audit configured in ServiceControl. Defaults to audit.
        /// </summary>
        public string ServiceControlSideAuditQueue { get; set; } = "audit";

        /// <summary>
        /// Gets or sets the poison message queue. Messages that can't be forwarded are moved to this queue.
        /// </summary>
        public string PoisonMessageQueue { get; set; } = "poison";

        /// <summary>
        /// Gets or sets the endpoint-side control queue -- the control queue configured in the endpoints. Defaults to Particular.ServiceControl.
        /// </summary>
        public string EndpointSideControlQueue { get; set; } = "Particular.ServiceControl";

        /// <summary>
        /// Gets or sets the ServiceControl-side control queue -- the ServiceControl input queue. Defaults to Particular.ServiceControl.
        /// </summary>
        public string ServiceControlSideControlQueue { get; set; } = "Particular.ServiceControl";

        /// <summary>
        /// Gets or sets the number of immediate retries to be used when forwarding control messages.
        /// </summary>
        public int ControlForwardingImmediateRetries { get; set; } = 5;

        /// <summary>
        /// Gets or sets the number of immediate retries to be used when forwarding ServiceControl integration messages.
        /// </summary>
        public int IntegrationForwardingImmediateRetries { get; set; } = 5;

        /// <summary>
        /// Gets or sets the number of immediate retries to be used when forwarding retry messages.
        /// </summary>
        public int RetryForwardingImmediateRetries { get; set; } = 5;

        /// <summary>
        /// Creates a new configuration object.
        /// </summary>
        /// <param name="name">Name of the adapter. Used as a prefix for adapter's own queues.</param>
        public TransportAdapterConfig(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Use provied callback to customize the endpoint-side transport.
        /// </summary>
        /// <param name="customization">Customization function.</param>
        public void CustomizeEndpointTransport(Action<TransportExtensions<TFront>> customization)
        {
            if (customization == null)
            {
                throw new ArgumentNullException(nameof(customization));
            }
            FrontendTransportCustomization = customization;
        }

        /// <summary>
        /// Use provided callback to customize the ServiceControl-side transport.
        /// </summary>
        /// <param name="customization">Customization function.</param>
        public void CustomizeServiceControlTransport(Action<TransportExtensions<TBack>> customization)
        {
            if (customization == null)
            {
                throw new ArgumentNullException(nameof(customization));
            }
            BackendTransportCustomization = customization;
        }

        /// <summary>
        /// Enables ServiceControl integration event forwarding using provided pub/sub strategies.
        /// </summary>
        /// <param name="integrationEventPublishingStrategy">Publishing strategy.</param>
        /// <param name="integrationEventSubscribingStrategy">Subscribing strategy.</param>
        public void ConfigureIntegrationEventForwarding(IIntegrationEventPublishingStrategy integrationEventPublishingStrategy, IIntegrationEventSubscribingStrategy integrationEventSubscribingStrategy)
        {
            if (integrationEventPublishingStrategy == null)
            {
                throw new ArgumentNullException(nameof(integrationEventPublishingStrategy));
            }
            if (integrationEventSubscribingStrategy == null)
            {
                throw new ArgumentNullException(nameof(integrationEventSubscribingStrategy));
            }
            this.IntegrationEventPublishingStrategy = integrationEventPublishingStrategy;
            this.IntegrationEventSubscribingStrategy = integrationEventSubscribingStrategy;
        }
    }
}