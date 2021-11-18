using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Storage;
using Microsoft.Extensions.Options;

namespace Hyperledger.Aries.Features.IssueCredential
{
    internal class DefaultCredentialHandler : IMessageHandler
    {
        private readonly AgentOptions _agentOptions;
        private readonly ICredentialService _credentialService;

        /// <summary>Initializes a new instance of the <see cref="DefaultCredentialHandler"/> class.</summary>
        /// <param name="agentOptions">The agent options.</param>
        /// <param name="credentialService">The credential service.</param>
        public DefaultCredentialHandler(
            IOptions<AgentOptions> agentOptions,
            ICredentialService credentialService)
        {
            _agentOptions = agentOptions.Value;
            _credentialService = credentialService;
        }

        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<MessageType> SupportedMessageTypes => new MessageType[]
        {
            MessageTypes.IssueCredentialNames.OfferCredential,
            MessageTypes.IssueCredentialNames.RequestCredential,
            MessageTypes.IssueCredentialNames.IssueCredential,
            MessageTypesHttps.IssueCredentialNames.OfferCredential,
            MessageTypesHttps.IssueCredentialNames.RequestCredential,
            MessageTypesHttps.IssueCredentialNames.IssueCredential
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="messageContext">The agent message.</param>
        /// <returns></returns>
        /// <exception cref="AriesFrameworkException">Unsupported message type {messageType}</exception>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                // v1
                case MessageTypesHttps.IssueCredentialNames.OfferCredential:
                case MessageTypes.IssueCredentialNames.OfferCredential:
                    {
                        var offer = messageContext.GetMessage<CredentialOfferMessage>();
                        var recordId = await _credentialService.ProcessOfferAsync(
                            agentContext, offer, messageContext.Connection);

                        messageContext.ContextRecord = await _credentialService.GetAsync(agentContext, recordId);

                        // Auto request credential if set in the agent option
                        if (_agentOptions.AutoRespondCredentialOffer == true)
                        {
                            var (message, record) = await _credentialService.CreateRequestAsync(agentContext, recordId);
                            messageContext.ContextRecord = record;
                            return message;
                        }

                        return null;
                    }

                case MessageTypesHttps.IssueCredentialNames.RequestCredential:
                case MessageTypes.IssueCredentialNames.RequestCredential:
                    {
                        var request = messageContext.GetMessage<CredentialRequestMessage>();
                        var recordId = await _credentialService.ProcessCredentialRequestAsync(
                                agentContext: agentContext,
                                credentialRequest: request,
                                connection: messageContext.Connection);
                        if (request.ReturnRoutingRequested() && messageContext.Connection == null)
                        {
                            var (message, record) = await _credentialService.CreateCredentialAsync(agentContext, recordId);
                            messageContext.ContextRecord = record;
                            return message;
                        }
                        else
                        {
                            // Auto create credential if set in the agent option
                            if (_agentOptions.AutoRespondCredentialRequest == true)
                            {
                                var (message, record) = await _credentialService.CreateCredentialAsync(agentContext, recordId);
                                messageContext.ContextRecord = record;
                                return message;
                            }
                            messageContext.ContextRecord = await _credentialService.GetAsync(agentContext, recordId);
                            return null;
                        }
                    }

                case MessageTypesHttps.IssueCredentialNames.IssueCredential:
                case MessageTypes.IssueCredentialNames.IssueCredential:
                    {
                        var credential = messageContext.GetMessage<CredentialIssueMessage>();
                        var recordId = await _credentialService.ProcessCredentialAsync(
                            agentContext, credential, messageContext.Connection);

                        messageContext.ContextRecord = await _credentialService.GetAsync(agentContext, recordId);

                        return null;
                    }
                default:
                    throw new AriesFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }
        }
    }
}
