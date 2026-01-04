using MiAllScaleTools.Configuration;
using MiAllScaleTools.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiAllScaleTools.Services
{
    public sealed class SyncService
    {
        private readonly IScaleGoodsReader _scaleReader;
        private readonly IMiAllGoodsRepository _miAllRepo;
        private readonly SyncSettings _syncSettings;

        public SyncService(IScaleGoodsReader scaleReader, IMiAllGoodsRepository miAllRepo, SyncSettings syncSettings)
        {
            _scaleReader = scaleReader ?? throw new ArgumentNullException(nameof(scaleReader));
            _miAllRepo = miAllRepo ?? throw new ArgumentNullException(nameof(miAllRepo));
            _syncSettings = syncSettings ?? throw new ArgumentNullException(nameof(syncSettings));
        }

        public async Task<SyncResult> RunAsync(IProgress<SyncProgress> progress, CancellationToken cancellationToken)
        {
            if (progress != null) progress.Report(new SyncProgress("读取电子秤数据...", 0, 0));
            var goods = await _scaleReader.ReadGoodsAsync(cancellationToken);

            if (goods.Count == 0 && _syncSettings.ScaleEmptyTreatAsError)
                throw new InvalidOperationException("电子秤读取到 0 条商品，已按配置中止（防止误删/误同步）。");

            if (progress != null) progress.Report(new SyncProgress("连接 MiAll 数据库...", 0, goods.Count));
            var typeNo = await _miAllRepo.GetScaleGoodsTypeNoAsync(cancellationToken);

            var result = new SyncResult(total: goods.Count);

            for (var i = 0; i < goods.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var good = goods[i];
                if (progress != null) progress.Report(new SyncProgress($"同步：{good.Name}", i, goods.Count, good, itemIndex: i + 1, succeeded: null, resultText: null));

                try
                {
                    var upsert = await _miAllRepo.UpsertGoodAsync(good, typeNo, _syncSettings.DryRun, cancellationToken);
                    if (upsert.Updated) result.Updated++;
                    if (upsert.Inserted) result.Inserted++;

                    var text = upsert.Inserted ? "新增" : (upsert.Updated ? "更新" : "完成");
                    if (progress != null) progress.Report(new SyncProgress($"完成：{good.Name}", i + 1, goods.Count, good, itemIndex: i + 1, succeeded: true, resultText: text));
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Failures.Add(new SyncFailure(good, ex));
                    if (progress != null) progress.Report(new SyncProgress($"失败：{good.Name}", i + 1, goods.Count, good, itemIndex: i + 1, succeeded: false, resultText: ex.Message));
                }
            }

            if (progress != null) progress.Report(new SyncProgress("同步完成", goods.Count, goods.Count));
            return result;
        }
    }

    public sealed class SyncResult
    {
        public int Total { get; private set; }
        public int Updated { get; set; }
        public int Inserted { get; set; }
        public int Failed { get; set; }
        public List<SyncFailure> Failures { get; private set; } = new List<SyncFailure>();

        public SyncResult(int total)
        {
            Total = total;
        }
    }

    public sealed class SyncFailure
    {
        public Good Good { get; private set; }
        public Exception Exception { get; private set; }

        public SyncFailure(Good good, Exception exception)
        {
            Good = good;
            Exception = exception;
        }
    }

    public sealed class SyncProgress
    {
        public string Message { get; private set; }
        public int Current { get; private set; }
        public int Total { get; private set; }
        public Good Good { get; private set; }
        public int? ItemIndex { get; private set; }
        public bool? Succeeded { get; private set; }
        public string ResultText { get; private set; }

        public SyncProgress(
            string message,
            int current,
            int total,
            Good good = null,
            int? itemIndex = null,
            bool? succeeded = null,
            string resultText = null)
        {
            Message = message;
            Current = current;
            Total = total;
            Good = good;
            ItemIndex = itemIndex;
            Succeeded = succeeded;
            ResultText = resultText;
        }
    }
}
