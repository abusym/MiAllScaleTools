using Microsoft.Win32;
using MiAllScaleTools.Configuration;
using System;
using System.Windows;

namespace MiAllScaleTools
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;

        public AppSettings SavedSettings
        {
            get { return _settings; }
        }

        public SettingsWindow(AppSettings current)
        {
            InitializeComponent();
            _settings = current ?? new AppSettings();

            TbScaleDbPath.Text = _settings.Scale.DbPath;
            TbSqlConn.Text = _settings.MiAll.ConnectionString;
            TbGoodsTypeName.Text = _settings.MiAll.ScaleGoodsTypeName;
        }

        private void BtnBrowseDb_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "SQLite (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3|All files (*.*)|*.*",
                Title = "选择电子秤 SQLite 数据库文件"
            };

            if (dlg.ShowDialog(this) == true)
                TbScaleDbPath.Text = dlg.FileName;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var next = new AppSettings();
                next.Scale.DbPath = (TbScaleDbPath.Text ?? "").Trim();
                next.MiAll.ConnectionString = (TbSqlConn.Text ?? "").Trim();
                var typeName = (TbGoodsTypeName.Text ?? "").Trim();
                next.MiAll.ScaleGoodsTypeName = string.IsNullOrWhiteSpace(typeName) ? "099 生鲜（电子秤）" : typeName;
                next.MiAll.CommandTimeoutSeconds = _settings.MiAll.CommandTimeoutSeconds;
                next.Sync = _settings.Sync;

                if (string.IsNullOrWhiteSpace(next.Scale.DbPath))
                    throw new InvalidOperationException("请先选择 SQLite 数据库文件路径。");

                _settings = next;
                AppSettingsStore.Save(_settings);

                TbHint.Text = "已保存。";
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "保存失败");
            }
        }
    }
}