﻿using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ConnectionManager;
using NServiceBus;
using NServiceBus.Transport.SQLServer;
using ServiceControl.Contracts;

namespace SomeEndpoint
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var config = new EndpointConfiguration("OtherEndpoint");

            var transport = config.UseTransport<SqlServerTransport>();
            transport.ConnectionString(@"Data Source=.\SQLEXPRESS;Initial Catalog=SCAdapter_Other;Integrated Security=True");
            transport.EnableLegacyMultiInstanceMode(ConnectionFactory.GetConnection);

            transport.Routing().RegisterPublisher(typeof(CustomCheckFailed).Assembly, "ServiceControl.SqlServer");
            config.Conventions().DefiningEventsAs(IsEvent);

            config.UsePersistence<InMemoryPersistence>();
            config.SendFailedMessagesTo("ServiceControl.SqlServer.error");
            config.AuditProcessedMessagesTo("ServiceControl.SqlServer.audit");
            config.EnableInstallers();
            config.Recoverability().Immediate(i => i.NumberOfRetries(0));
            config.Recoverability().Delayed(d => d.NumberOfRetries(0));

            var endpoint = await Endpoint.Start(config);
            
            Console.WriteLine("Press <enter> to send a message the endpoint.");

            while (true)
            {
                Console.ReadLine();
                await endpoint.SendLocal(new OtherMessage());
            }
        }

        static bool IsEvent(Type t)
        {
            return t.Namespace == "ServiceControl.Contracts" ||
                (typeof(IEvent).IsAssignableFrom(t) && typeof(IEvent) != t);
        }
    }

    class HeartbeatHandler : 
        IHandleMessages<HeartbeatStopped>,
        IHandleMessages<HeartbeatRestored>
    {
        public Task Handle(HeartbeatStopped message, IMessageHandlerContext context)
        {
            Console.WriteLine($"Endpoint {message.EndpointName} stopped sending heartbeats.");
            return Task.FromResult(0);
        }

        public Task Handle(HeartbeatRestored message, IMessageHandlerContext context)
        {
            Console.WriteLine($"Endpoint {message.EndpointName} resumed sending heartbeats.");
            return Task.FromResult(0);

        }
    }

    class OtherMessageHandler : IHandleMessages<OtherMessage>
    {
        static readonly Random R = new Random();
        public Task Handle(OtherMessage message, IMessageHandlerContext context)
        {
            if (R.Next(2) == 0)
            {
                throw new Exception("Simulated");
            }
            Console.WriteLine("Processed");
            return Task.FromResult(0);
        }
    }

    class OtherMessage : IMessage
    {
    }
}
