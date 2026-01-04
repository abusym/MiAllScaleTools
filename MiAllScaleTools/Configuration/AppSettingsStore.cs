using Newtonsoft.Json;
using System;
using System.IO;

namespace MiAllScaleTools.Configuration
{
    public sealed class AppSettingsStore
    {
        public const string DefaultFileName = "appsettings.json";

        public static string GetDefaultPath()
        {
            return Path.Combine(AppContext.BaseDirectory, DefaultFileName);
        }

        public static AppSettings Load()
        {
            return Load((string)null);
        }

        public static AppSettings Load(string path)
        {
            var filePath = string.IsNullOrWhiteSpace(path) ? GetDefaultPath() : path;
            if (!File.Exists(filePath))
                return new AppSettings();

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new AppSettings();

            try
            {
                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("配置文件解析失败：" + filePath + "。请检查 JSON 格式是否正确。原始错误：" + ex.Message, ex);
            }
        }

        public static void Save(AppSettings settings)
        {
            Save(settings, null);
        }

        public static void Save(AppSettings settings, string path)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var filePath = string.IsNullOrWhiteSpace(path) ? GetDefaultPath() : path;
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
