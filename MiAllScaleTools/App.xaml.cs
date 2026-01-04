using System.Configuration;
using System.Data;
using System.Windows;
using SQLitePCL;

namespace MiAllScaleTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Microsoft.Data.Sqlite 在 .NET Framework 下通常需要初始化 SQLitePCL（加载 e_sqlite3）
            try { Batteries_V2.Init(); } catch { /* ignore */ }

            base.OnStartup(e);
        }
    }

}
