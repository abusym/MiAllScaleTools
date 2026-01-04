using MiAllScaleTools.Domain;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiAllScaleTools.Services
{
    public interface IScaleGoodsReader
    {
        Task<IReadOnlyList<Good>> ReadGoodsAsync(CancellationToken cancellationToken);
    }
}
