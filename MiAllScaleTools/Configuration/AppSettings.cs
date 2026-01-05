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
        public const string DefaultDbPath = @"C:\Program Files (x86)\Vahan\My Product Name\mscale.mdb";
        public string DbPath { get; set; } = DefaultDbPath;
    }

    public sealed class MiAllSettings
    {
        public const string DefaultConnectionString =
            "Server=POS1;Database=MiDe5;User Id=sa;Password=ewilka;TrustServerCertificate=true;Connect Timeout=10";

        public string ConnectionString { get; set; } = DefaultConnectionString;
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
