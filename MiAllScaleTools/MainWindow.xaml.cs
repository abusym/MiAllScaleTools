using System.Windows;
using System.Windows.Input;
using MiAllScaleTools.Configuration;
using MiAllScaleTools.Models;
using MiAllScaleTools.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace MiAllScaleTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppSettings _settings = new AppSettings();
        private CancellationTokenSource _cts;
        private readonly ObservableCollection<SyncRow> _rows = new ObservableCollection<SyncRow>();

        public MainWindow()
        {
            InitializeComponent();
            DgResults.ItemsSource = _rows;
            LoadSettings(AppSettingsStore.Load());
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SettingsWindow(_settings) { Owner = this };
                if (dlg.ShowDialog() == true)
                    LoadSettings(dlg.SavedSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "打开设置失败");
            }
        }

        private void RunSyncCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Reuse the same behavior as clicking the "Run" button.
            BtnRun_Click(BtnRun, new RoutedEventArgs());
        }

        private async void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                return;
            }

            try
            {
                // 运行前确保已加载设置；设置窗口里会保存到文件
                if (string.IsNullOrWhiteSpace(_settings.Scale.DbPath) ||
                    string.IsNullOrWhiteSpace(_settings.MiAll.ConnectionString) ||
                    string.IsNullOrWhiteSpace(_settings.MiAll.ScaleGoodsTypeName))
                {
                    MessageBox.Show("请先在“设置”中配置 SQLite 路径和 MiAll 连接信息。", "提示");
                    return;
                }

                _cts = new CancellationTokenSource();
                SetRunningState(isRunning: true);
                TbSummary.Text = "";
                Progress.Value = 0;
                _rows.Clear();

                var barcodeTransformer = new BarcodeTransformer();
                var scaleReader = new SqliteScaleGoodsReader(_settings.Scale, barcodeTransformer);
                var miAllRepo = new SqlServerMiAllGoodsRepository(_settings.MiAll);
                var sync = new SyncService(scaleReader, miAllRepo, _settings.Sync);

                var progress = new Progress<SyncProgress>(p =>
                {
                    TbStatus.Text = p.Message;
                    if (p.Total > 0)
                        Progress.Value = Clamp(p.Current * 100.0 / p.Total, 0, 100);

                    // 每个商品完成后，实时追加一行到明细表
                    if (p.Good != null && p.Succeeded.HasValue)
                    {
                        _rows.Add(new SyncRow
                        {
                            Index = p.ItemIndex ?? _rows.Count + 1,
                            Name = p.Good.Name,
                            Barcode = p.Good.Barcode,
                            Price = p.Good.Price,
                            Succeeded = p.Succeeded.Value,
                            Result = p.Succeeded.Value ? (p.ResultText ?? "成功") : ("失败：" + (p.ResultText ?? "")),
                            Time = DateTime.Now
                        });

                        if (_rows.Count > 0)
                            DgResults.ScrollIntoView(_rows[_rows.Count - 1]);
                    }
                });

                var result = await sync.RunAsync(progress, _cts.Token);

                TbSummary.Text = $"总数：{result.Total}，更新：{result.Updated}，新增：{result.Inserted}，失败：{result.Failed}";
            }
            catch (OperationCanceledException)
            {
                TbSummary.Text = "已取消。";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "同步失败");
            }
            finally
            {
                if (_cts != null) _cts.Dispose();
                _cts = null;
                SetRunningState(isRunning: false);
                TbStatus.Text = "就绪";
            }
        }

        private void LoadSettings(AppSettings settings)
        {
            _settings = settings ?? new AppSettings();
            TbStatus.Text = string.IsNullOrWhiteSpace(_settings.Scale.DbPath) ? "请先在“设置”中配置 SQLite 路径" : "就绪";
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void SetRunningState(bool isRunning)
        {
            BtnSettings.IsEnabled = !isRunning;

            BtnRun.Content = isRunning ? "取消 (F5)" : "开始同步 (F5)";
        }
    }
}