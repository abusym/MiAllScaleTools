namespace MiAllScaleTools.Configuration
{
    public sealed class AppSettings
    {
        public ScaleSettings Scale { get; set; } = new ScaleSettings();
        public MiAllSettings MiAll { get; set; } = new MiAllSettings();
        public SyncSettings Sync { get; set; } = new SyncSettings();
    }

    public sealed class ScaleSettings
    {
        public string DbPath { get; set; } = "";
    }

    public sealed class MiAllSettings
    {
        public string ConnectionString { get; set; } = "";
        public string ScaleGoodsTypeName { get; set; } = "099 生鲜（电子秤）";
        public int CommandTimeoutSeconds { get; set; } = 30;
    }

    public sealed class SyncSettings
    {
        public bool DryRun { get; set; } = false;
        public bool EnableDeleteMissing { get; set; } = false;
        public bool ScaleEmptyTreatAsError { get; set; } = true;
    }
}
