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

            // Defensive: tolerate partially-null settings coming from JSON
            if (_settings.Scale == null) _settings.Scale = new ScaleSettings();
            if (_settings.MiAll == null) _settings.MiAll = new MiAllSettings();
            if (_settings.Sync == null) _settings.Sync = new SyncSettings();

            if (string.IsNullOrWhiteSpace(_settings.Scale.DbPath))
                _settings.Scale.DbPath = ScaleSettings.DefaultDbPath;

            if (string.IsNullOrWhiteSpace(_settings.MiAll.ConnectionString))
                _settings.MiAll.ConnectionString = MiAllSettings.DefaultConnectionString;

            TbScaleDbPath.Text = _settings.Scale.DbPath;
            TbSqlConn.Text = _settings.MiAll.ConnectionString;
            TbGoodsTypeName.Text = _settings.MiAll.ScaleGoodsTypeName;
        }

        private void BtnBrowseDb_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Access Database (*.mdb;*.accdb)|*.mdb;*.accdb|All files (*.*)|*.*",
                Title = "选择电子秤 Access 数据库文件（mscale.mdb）"
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
                    throw new InvalidOperationException("请先选择 Access 数据库文件路径（.mdb/.accdb）。");

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