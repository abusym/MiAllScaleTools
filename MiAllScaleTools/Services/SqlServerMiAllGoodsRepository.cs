using MiAllScaleTools.Configuration;
using MiAllScaleTools.Domain;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace MiAllScaleTools.Services
{
    public sealed class SqlServerMiAllGoodsRepository : IMiAllGoodsRepository
    {
        private readonly MiAllSettings _miAll;

        public SqlServerMiAllGoodsRepository(MiAllSettings miAll)
        {
            _miAll = miAll ?? throw new ArgumentNullException(nameof(miAll));
        }

        public async Task<string> GetScaleGoodsTypeNoAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_miAll.ConnectionString))
                throw new InvalidOperationException("未配置 MiAll.ConnectionString（SQL Server 连接字符串）");

            using (var conn = new SqlConnection(_miAll.ConnectionString))
            {
                await conn.OpenAsync(cancellationToken);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandTimeout = _miAll.CommandTimeoutSeconds;
                    cmd.CommandText = "select goodstypeno from jbgoodstype where goodstypename=@name";
                    cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 100) { Value = _miAll.ScaleGoodsTypeName });

                    var obj = await cmd.ExecuteScalarAsync(cancellationToken);
                    var typeNo = obj != null ? obj.ToString() : null;
                    if (string.IsNullOrWhiteSpace(typeNo))
                        throw new InvalidOperationException($"请在 MiAll 后台添加商品类目“{_miAll.ScaleGoodsTypeName}”");

                    return typeNo;
                }
            }
        }

        public async Task<UpsertResult> UpsertGoodAsync(Good good, string goodTypeNo, bool dryRun, CancellationToken cancellationToken)
        {
            if (good == null) throw new ArgumentNullException(nameof(good));
            if (string.IsNullOrWhiteSpace(goodTypeNo)) throw new ArgumentException("goodTypeNo 不能为空", nameof(goodTypeNo));

            if (dryRun)
            {
                // DryRun：不落库
                return new UpsertResult(updated: false, inserted: false);
            }

            using (var conn = new SqlConnection(_miAll.ConnectionString))
            {
                await conn.OpenAsync(cancellationToken);

                // 事务：保护 max+1 生成 goodsno 的并发
                using (var tx = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        // 1) update
                        using (var update = conn.CreateCommand())
                        {
                            update.Transaction = tx;
                            update.CommandTimeout = _miAll.CommandTimeoutSeconds;
                            update.CommandText = @"
update jbgoods
set srefprice=@price, goodsname=@name
where barcode=@barcode and goodstypeno=@goodstypeno";

                            update.Parameters.Add(new SqlParameter("@price", SqlDbType.Decimal) { Value = good.Price });
                            update.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = (object)good.Name ?? DBNull.Value });
                            update.Parameters.Add(new SqlParameter("@barcode", SqlDbType.VarChar, 50) { Value = good.Barcode });
                            update.Parameters.Add(new SqlParameter("@goodstypeno", SqlDbType.VarChar, 50) { Value = goodTypeNo });

                            var affected = await update.ExecuteNonQueryAsync(cancellationToken);
                            if (affected > 0)
                            {
                                tx.Commit();
                                return new UpsertResult(updated: true, inserted: false);
                            }
                        }

                        // 2) insert（goodsno = max+1）
                        string nextGoodsNo;
                        using (var maxCmd = conn.CreateCommand())
                        {
                            maxCmd.Transaction = tx;
                            maxCmd.CommandTimeout = _miAll.CommandTimeoutSeconds;
                            maxCmd.CommandText = "select isnull(max(cast(goodsno as int)),0)+1 from jbgoods with (updlock, holdlock)";

                            var obj = await maxCmd.ExecuteScalarAsync(cancellationToken);
                            nextGoodsNo = obj != null ? obj.ToString() : null;
                            if (string.IsNullOrWhiteSpace(nextGoodsNo))
                                throw new InvalidOperationException("无法生成 goodsno（max+1 返回空）");
                        }

                        using (var insert = conn.CreateCommand())
                        {
                            insert.Transaction = tx;
                            insert.CommandTimeout = _miAll.CommandTimeoutSeconds;
                            insert.CommandText = @"
insert into jbgoods(goodsno, goodscode, goodsname, goodstypeno, barcode, srefprice, salestype)
values(@goodsno, @goodscode, @goodsname, @goodstypeno, @barcode, @price, 0)";

                            insert.Parameters.Add(new SqlParameter("@goodsno", SqlDbType.VarChar, 50) { Value = nextGoodsNo });
                            insert.Parameters.Add(new SqlParameter("@goodscode", SqlDbType.VarChar, 50) { Value = nextGoodsNo });
                            insert.Parameters.Add(new SqlParameter("@goodsname", SqlDbType.NVarChar, 200) { Value = (object)good.Name ?? DBNull.Value });
                            insert.Parameters.Add(new SqlParameter("@goodstypeno", SqlDbType.VarChar, 50) { Value = goodTypeNo });
                            insert.Parameters.Add(new SqlParameter("@barcode", SqlDbType.VarChar, 50) { Value = good.Barcode });
                            insert.Parameters.Add(new SqlParameter("@price", SqlDbType.Decimal) { Value = good.Price });

                            await insert.ExecuteNonQueryAsync(cancellationToken);
                        }

                        tx.Commit();
                        return new UpsertResult(updated: false, inserted: true);
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { /* ignore */ }
                        throw;
                    }
                }
            }
        }
    }
}