using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Core;
using RR.DataverseAzureSql.Common.Interfaces.Services.Storage;

namespace RR.DataverseAzureSql.Tests.Common.Mocks.Services.Storage
{
    public class MockTableService : ITableService
    {
        private readonly Dictionary<string, string> _tableStorage = new Dictionary<string, string>();

        public string GetDeltalink(string partitionKey)
        {
            if (_tableStorage.TryGetValue(partitionKey, out string value))
            {
                return value;
            }
            return "";
        }

        public Response SetDeltalink(string partitionKey, string deltalink)
        {
            _tableStorage[partitionKey] = deltalink;
            return new MockResponse();
        }
    }

    internal class MockResponse : Response
    {
        public override int Status => throw new NotImplementedException();

        public override string ReasonPhrase => throw new NotImplementedException();

        public override Stream ContentStream { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string ClientRequestId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        protected override bool ContainsHeader(string name)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string value)
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string> values)
        {
            throw new NotImplementedException();
        }
    }
}
