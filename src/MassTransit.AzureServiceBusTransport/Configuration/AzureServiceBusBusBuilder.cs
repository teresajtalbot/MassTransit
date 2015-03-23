﻿// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.AzureServiceBusTransport.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Builders;
    using MassTransit.Pipeline;
    using MassTransit.Pipeline.Pipes;
    using PipeConfigurators;
    using Transports;


    public class AzureServiceBusBusBuilder :
        BusBuilder,
        IBusBuilder
    {
        readonly IServiceBusHost[] _hosts;
        readonly Uri _inputAddress;
        readonly Lazy<ISendEndpointProvider> _publishSendEndpointProvider;

        public AzureServiceBusBusBuilder(IEnumerable<IServiceBusHost> hosts, Uri inputAddress,
            IEnumerable<IPipeSpecification<ConsumeContext>> endpointPipeSpecifications)
            : base(endpointPipeSpecifications)
        {
            if (hosts == null)
                throw new ArgumentNullException("hosts");

            _hosts = hosts.ToArray();

            _inputAddress = inputAddress;
            _publishSendEndpointProvider = new Lazy<ISendEndpointProvider>(CreatePublishSendEndpointProvider);
        }

        protected ISendEndpointProvider PublishSendEndpointProvider
        {
            get { return _publishSendEndpointProvider.Value; }
        }

        protected override ISendEndpointProvider CreateSendEndpointProvider()
        {
            var provider = new ServiceBusSendEndpointProvider(MessageSerializer, _inputAddress, _hosts);

            return new SendEndpointCache(provider);
        }

        protected ISendEndpointProvider CreatePublishSendEndpointProvider()
        {
            var provider = new PublishSendEndpointProvider(MessageSerializer, _inputAddress, _hosts);

            return new SendEndpointCache(provider);
        }

        protected override IPublishEndpoint CreatePublishEndpoint()
        {
            return new AzureServiceBusPublishEndpoint(_hosts[0], PublishSendEndpointProvider);
        }

        public virtual IBusControl Build()
        {
            IConsumePipe consumePipe = ReceiveEndpoints.Where(x => x.InputAddress.Equals(_inputAddress))
                .Select(x => x.ConsumePipe).FirstOrDefault() ?? new ConsumePipe();

            return new MassTransitBus(_inputAddress, consumePipe, SendEndpointProvider, PublishEndpoint, ReceiveEndpoints, _hosts);
        }
    }
}