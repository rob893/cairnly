using Azure.Communication.Email;
using Azure.Core;

namespace Cairnly.API.Utilities;

public interface IAcsEmailClientFactory
{
    EmailClient CreateClient(TokenCredential? tokenCredential = null);
}