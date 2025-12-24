using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;

namespace RR.DataverseAzureSql.Tests.Common.Mocks.Isolated
{
    internal class MockFunctionContext : FunctionContext
    {

        public MockFunctionContext() { }

        public MockFunctionContext(BindingContext bindingContext)
        {
            BindingContext = bindingContext;
        }

        public override IServiceProvider InstanceServices { get; set; }

        public override FunctionDefinition FunctionDefinition { get; }

        public override IDictionary<object, object> Items { get; set; }

        public override IInvocationFeatures Features { get; }

        public override string InvocationId { get; }

        public override string FunctionId { get; }

        public override TraceContext TraceContext { get; }

        public override BindingContext BindingContext { get; }

        public override RetryContext RetryContext => throw new NotImplementedException();



    }
}
