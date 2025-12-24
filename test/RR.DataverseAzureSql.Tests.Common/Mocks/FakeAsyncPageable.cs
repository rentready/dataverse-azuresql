using Microsoft.DurableTask;


namespace RR.DataverseAzureSql.Tests.Common.Mocks
{
    public class FakeAsyncPageable<T> : AsyncPageable<T>
    {
        private readonly IEnumerable<Page<T>> _items;

        public FakeAsyncPageable(IEnumerable<Page<T>> items)
        {
            _items = items;
        }

        public override IAsyncEnumerable<Page<T>> AsPages(string continuationToken = null, int? pageSizeHint = null)
        {
            return _items.ToAsyncEnumerable();
        }
    }
}
