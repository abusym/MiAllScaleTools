using MiAllScaleTools.Configuration;
using MiAllScaleTools.Domain;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MiAllScaleTools.Services
{
    public sealed class AccessScaleGoodsReader : IScaleGoodsReader
    {
        private readonly ScaleSettings _scale;
        private readonly BarcodeTransformer _barcodeTransformer;

        // Old tool query (Access mscale.mdb)
        private const string DefaultGoodsQuery =
            "select mname1 as name, mprice as price, mcode as barcode from t_base_merchandise";

        public AccessScaleGoodsReader(ScaleSettings scale, BarcodeTransformer barcodeTransformer)
        {
            _scale = scale ?? throw new ArgumentNullException(nameof(scale));
            _barcodeTransformer = barcodeTransformer ?? throw new ArgumentNullException(nameof(barcodeTransformer));
        }

        public async Task<IReadOnlyList<Good>> ReadGoodsAsync(CancellationToken cancellationToken)
        {
            // OleDb APIs are synchronous; keep async signature for interface compatibility.
            return await Task.Run(() => ReadGoods(cancellationToken), cancellationToken);
        }

        private IReadOnlyList<Good> ReadGoods(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_scale.DbPath))
                throw new InvalidOperationException("未配置 Scale.DbPath（Access 数据库文件路径）");

            if (!File.Exists(_scale.DbPath))
                throw new FileNotFoundException("Access 数据库文件未找到，请检查配置 Scale.DbPath", _scale.DbPath);

            var connStr = BuildConnectionString(_scale.DbPath, out var providerUsed);

            var goods = new List<Good>();

            try
            {
                using (var conn = new OleDbConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(DefaultGoodsQuery, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader == null) return goods;

                        while (reader.Read())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var name = reader["name"] != null ? (reader["name"].ToString() ?? "") : "";
                            var rawBarcode = reader["barcode"] != null ? (reader["barcode"].ToString() ?? "") : "";

                            decimal price = 0m;
                            var priceObj = reader["price"];
                            if (priceObj != null && priceObj != DBNull.Value)
                            {
                                if (priceObj is decimal) price = (decimal)priceObj;
                                else if (priceObj is double) price = Convert.ToDecimal((double)priceObj, CultureInfo.InvariantCulture);
                                else if (priceObj is float) price = Convert.ToDecimal((float)priceObj, CultureInfo.InvariantCulture);
                                else if (priceObj is int) price = (int)priceObj;
                                else if (priceObj is long) price = (long)priceObj;
                                else
                                {
                                    decimal parsed;
                                    if (decimal.TryParse(priceObj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)) price = parsed;
                                    else price = Convert.ToDecimal(priceObj, CultureInfo.InvariantCulture);
                                }
                            }

                            var barcode = _barcodeTransformer.Transform(rawBarcode, name);

                            goods.Add(new Good
                            {
                                Name = name,
                                Price = price,
                                Barcode = barcode
                            });
                        }
                    }
                }

                return goods;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "读取电子秤 Access 失败：请确认这是电子秤软件的数据库文件（mscale.mdb/mscale.accdb），且包含表 t_base_merchandise（字段 mname1/mprice/mcode）。" +
                    "如果提示找不到 Provider，请安装 Microsoft Access Database Engine（ACE OLEDB）并确保程序位数与驱动一致。" +
                    $"（当前使用 Provider: {providerUsed}）原始错误：{ex.Message}",
                    ex);
            }
        }

        private static string BuildConnectionString(string dbPath, out string providerUsed)
        {
            var ext = Path.GetExtension(dbPath)?.ToLowerInvariant();

            // Provider selection rules:
            // - .accdb: requires ACE (Jet cannot open it)
            // - .mdb: prefer Jet (works without installing ACE, but 32-bit only), fallback to ACE if present
            var candidates = ext == ".accdb"
                ? new[] { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0" }
                : new[] { "Microsoft.Jet.OLEDB.4.0", "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0" };

            Exception last = null;
            foreach (var provider in candidates)
            {
                var cs = $"Provider={provider};Data Source={dbPath};Persist Security Info=False;";
                try
                {
                    using (var conn = new OleDbConnection(cs))
                    {
                        conn.Open();
                        providerUsed = provider;
                        return cs;
                    }
                }
                catch (Exception ex)
                {
                    // Only fall back when the provider isn't installed / registered.
                    if (!IsProviderMissing(ex))
                        throw;

                    last = ex;
                }
            }

            providerUsed = "(none)";
            throw new InvalidOperationException(
                "无法打开 Access 数据库：未找到可用的 OLEDB Provider（ACE/Jet）。" +
                "常见原因：程序以 64 位运行导致 Jet 4.0 不可用，或未安装 Microsoft Access Database Engine（ACE）。" +
                "解决方式：若数据库是 .mdb，优先使用 x86 版本运行；若是 .accdb，请安装与程序位数一致的 ACE 驱动（2010/2016）。" +
                $"（尝试过的 Provider: {string.Join(", ", candidates)}）",
                last);
        }

        private static bool IsProviderMissing(Exception ex)
        {
            // Typical messages:
            // - "The 'Microsoft.ACE.OLEDB.12.0' provider is not registered on the local machine."
            // - "未在本地计算机上注册 'Microsoft.Jet.OLEDB.4.0' 提供程序。"
            if (ex is OleDbException) return false;

            var msg = ex.Message ?? "";
            if (msg.IndexOf("provider is not registered", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (msg.IndexOf("未在本地计算机上注册", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (msg.IndexOf("找不到", StringComparison.OrdinalIgnoreCase) >= 0 &&
                msg.IndexOf("provider", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            // Some environments surface this as COMException with REGDB_E_CLASSNOTREG (0x80040154).
            if (ex is COMException comEx && comEx.ErrorCode == unchecked((int)0x80040154)) return true;

            // Walk inner exceptions too.
            return ex.InnerException != null && IsProviderMissing(ex.InnerException);
        }
    }
}


