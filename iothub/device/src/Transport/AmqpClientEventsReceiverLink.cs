// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Specialization of AmqpClientLink for sending methods response
    /// </summary>
    internal class AmqpClientEventsReceiverLink : AmqpClientLink
    {
        const string linkNamePostFix = "_EventsReceiverLink";

        internal AmqpClientEventsReceiverLink(
            AmqpClientLinkType amqpClientLinkType, 
            AmqpSession amqpSession, 
            DeviceClientEndpointIdentity deviceClientEndpointIdentity, 
            TimeSpan timeout,
            string correlationId,
            bool useTokenRefresher,
            AmqpClientSession amqpAuthenticationSession
            )
            : base(amqpClientLinkType, amqpSession, deviceClientEndpointIdentity, correlationId, useTokenRefresher, amqpAuthenticationSession)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientEventsReceiverLink)}");

            linkPath = this.BuildPath(CommonConstants.DeviceEventPathTemplate, CommonConstants.ModuleEventPathTemplate);
            Uri uri = deviceClientEndpointIdentity.iotHubConnectionString.BuildLinkAddress(linkPath);
            uint prefetchCount = deviceClientEndpointIdentity.amqpTransportSettings.PrefetchCount;

            amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = CommonResources.GetNewStringGuid(linkNamePostFix),
                Role = true,
                TotalLinkCredit = prefetchCount,
                AutoSendFlow = prefetchCount > 0,
                Source = new Source() { Address = uri.AbsoluteUri }
            };
            amqpLinkSettings.SndSettleMode = null; // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
            amqpLinkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;

            var timeoutHelper = new TimeoutHelper(timeout);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeoutHelper.RemainingTime().TotalMilliseconds);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, deviceClientEndpointIdentity.productInfo.ToString());

            amqpLink = new ReceivingAmqpLink(amqpLinkSettings);
            amqpLink.AttachTo(amqpSession);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientEventsReceiverLink)}");
        }
    }
}