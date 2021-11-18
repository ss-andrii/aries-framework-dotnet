using Newtonsoft.Json;

namespace Hyperledger.Aries.Features.IssueCredential.Models
{
    internal class CredentialAttributeValue
    {
        [JsonProperty("raw")]
        internal string Raw { get; set; }

        [JsonProperty("encoded")]
        internal string Encoded { get; set; }
    }
}

