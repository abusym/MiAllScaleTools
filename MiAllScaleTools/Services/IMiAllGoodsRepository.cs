using MiAllScaleTools.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace MiAllScaleTools.Services
{
    public interface IMiAllGoodsRepository
    {
        Task<string> GetScaleGoodsTypeNoAsync(CancellationToken cancellationToken);
        Task<UpsertResult> UpsertGoodAsync(Good good, string goodTypeNo, bool dryRun, CancellationToken cancellationToken);
    }

    public struct UpsertResult
    {
        public bool Updated { get; private set; }
        public bool Inserted { get; private set; }

        public UpsertResult(bool updated, bool inserted)
        {
            Updated = updated;
            Inserted = inserted;
        }
    }
}
