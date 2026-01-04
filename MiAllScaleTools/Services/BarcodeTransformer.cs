using System;

namespace MiAllScaleTools.Services
{
    public sealed class BarcodeTransformer
    {
        // 规则保持与旧实现一致：条码必须为 7 位，取第 3-7 位（Substring(2,5)）
        private const int ExpectedLength = 7;
        private const int ExtractStart = 2;
        private const int ExtractLength = 5;

        public string Transform(string rawBarcode, string goodNameForError = null)
        {
            var name = string.IsNullOrWhiteSpace(goodNameForError) ? "" : $"{goodNameForError}：";

            if (string.IsNullOrWhiteSpace(rawBarcode))
                throw new InvalidOperationException($"{name}条码为空");

            rawBarcode = rawBarcode.Trim();
            if (rawBarcode.Length != ExpectedLength)
                throw new InvalidOperationException($"{name}的条码位数必须等于{ExpectedLength}位");

            if (ExtractStart < 0 || ExtractStart + ExtractLength > rawBarcode.Length)
                throw new InvalidOperationException($"{name}条码截取规则无效");

            return rawBarcode.Substring(ExtractStart, ExtractLength);
        }
    }
}