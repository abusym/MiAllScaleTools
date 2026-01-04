using Microsoft.Data.Sqlite;
using MiAllScaleTools.Configuration;
using MiAllScaleTools.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MiAllScaleTools.Services
{
    public sealed class SqliteScaleGoodsReader : IScaleGoodsReader
    {
        private readonly ScaleSettings _scale;
        private readonly BarcodeTransformer _barcodeTransformer;
        private const string DefaultGoodsQuery =
            "select mname1 as name, mprice as price, mcode as barcode from t_base_merchandise";

        public SqliteScaleGoodsReader(ScaleSettings scale, BarcodeTransformer barcodeTransformer)
        {
            _scale = scale ?? throw new ArgumentNullException(nameof(scale));
            _barcodeTransformer = barcodeTransformer ?? throw new ArgumentNullException(nameof(barcodeTransformer));
        }

        public async Task<IReadOnlyList<Good>> ReadGoodsAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_scale.DbPath))
                throw new InvalidOperationException("未配置 Scale.DbPath（SQLite 文件路径）");

            if (!File.Exists(_scale.DbPath))
                throw new FileNotFoundException("SQLite 文件未找到，请检查配置 Scale.DbPath", _scale.DbPath);

            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = _scale.DbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared
            };

            var goods = new List<Good>();

            using (var conn = new SqliteConnection(csb.ToString()))
            {
                await conn.OpenAsync(cancellationToken);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = DefaultGoodsQuery;

                    try
                    {
                        using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            while (await reader.ReadAsync(cancellationToken))
                            {
                                var name = reader["name"] != null ? (reader["name"].ToString() ?? "") : "";
                                var rawBarcode = reader["barcode"] != null ? (reader["barcode"].ToString() ?? "") : "";

                                decimal price = 0m;
                                var priceObj = reader["price"];
                                if (priceObj != null && priceObj != DBNull.Value)
                                {
                                    // SQLite 可能返回 long/double/string，统一按 decimal 处理
                                    if (priceObj is decimal) price = (decimal)priceObj;
                                    else if (priceObj is long) price = (long)priceObj;
                                    else if (priceObj is double) price = Convert.ToDecimal((double)priceObj, CultureInfo.InvariantCulture);
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
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            "读取电子秤 SQLite 失败：请确认这是电子秤软件的数据库文件，且包含表 t_base_merchandise（字段 mname1/mprice/mcode）。如版本不一致请联系技术人员处理。原始错误：" +
                            ex.Message, ex);
                    }
                }
            }

            return goods;
        }
    }
}