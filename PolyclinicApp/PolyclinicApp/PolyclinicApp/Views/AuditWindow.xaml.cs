using PolyclinicApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PolyclinicApp.Views
{
    public partial class AuditWindow : Window
    {
        private long? _selectedAuditId;

        private bool _isLoading;

        public AuditWindow()
        {
            InitializeComponent();

            _isLoading = true;
            LoadFilters();
            _isLoading = false;

            LoadAudit();
        }

        private void LoadFilters()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                List<AuditUserComboItem> users = db.Пользователи
                    .OrderBy(u => u.Логин)
                    .Select(u => new AuditUserComboItem
                    {
                        Id = u.ID_пользователя,
                        Name = u.Логин
                    })
                    .ToList();

                users.Insert(0, new AuditUserComboItem
                {
                    Id = 0,
                    Name = "Все пользователи"
                });

                UserComboBox.ItemsSource = users;
                UserComboBox.SelectedValue = 0;

                List<string> tables = db.Журнал_аудита
                    .Select(a => a.Таблица)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                tables.Insert(0, "Все таблицы");

                TableComboBox.ItemsSource = tables;
                TableComboBox.SelectedIndex = 0;
            }

            OperationComboBox.ItemsSource = new List<AuditOperationItem>
            {
                new AuditOperationItem { Code = "", Name = "Все операции" },
                new AuditOperationItem { Code = "I", Name = "Добавление" },
                new AuditOperationItem { Code = "U", Name = "Изменение" },
                new AuditOperationItem { Code = "D", Name = "Удаление" }
            };

            OperationComboBox.SelectedValue = "";
        }

        private void LoadAudit()
        {
            using (var db = new ПоликлиникаEntities1())
            {
                string search = SearchTextBox.Text.Trim();

                int selectedUserId = 0;

                if (UserComboBox.SelectedValue != null)
                {
                    selectedUserId = Convert.ToInt32(UserComboBox.SelectedValue);
                }

                string selectedTable = TableComboBox.SelectedItem as string;
                string selectedOperation = "";

                if (OperationComboBox.SelectedValue != null)
                {
                    selectedOperation = OperationComboBox.SelectedValue.ToString();
                }

                DateTime? dateFrom = DateFromPicker.SelectedDate;
                DateTime? dateTo = DateToPicker.SelectedDate;

                var query =
                    from a in db.Журнал_аудита
                    join u in db.Пользователи
                        on a.ID_пользователя equals u.ID_пользователя
                    select new
                    {
                        AuditId = a.ID_записи,
                        UserId = u.ID_пользователя,
                        UserLogin = u.Логин,
                        TableName = a.Таблица,
                        Operation = a.Операция,
                        RowId = a.ID_записи_в_таблице,
                        OldValue = a.Старое_значение,
                        NewValue = a.Новое_значение,
                        IpAddress = a.IP_адрес,
                        DateTime = a.Дата_время
                    };

                if (selectedUserId != 0)
                {
                    query = query.Where(x => x.UserId == selectedUserId);
                }

                if (!string.IsNullOrWhiteSpace(selectedTable) && selectedTable != "Все таблицы")
                {
                    query = query.Where(x => x.TableName == selectedTable);
                }

                if (!string.IsNullOrWhiteSpace(selectedOperation))
                {
                    query = query.Where(x => x.Operation == selectedOperation);
                }

                if (dateFrom != null)
                {
                    DateTime from = dateFrom.Value.Date;
                    query = query.Where(x => x.DateTime >= from);
                }

                if (dateTo != null)
                {
                    DateTime to = dateTo.Value.Date.AddDays(1);
                    query = query.Where(x => x.DateTime < to);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.UserLogin.Contains(search) ||
                        x.TableName.Contains(search) ||
                        x.Operation.Contains(search) ||
                        x.RowId.Contains(search) ||
                        x.IpAddress.Contains(search) ||
                        x.OldValue.Contains(search) ||
                        x.NewValue.Contains(search));
                }

                AuditListView.ItemsSource = query
                    .OrderByDescending(x => x.DateTime)
                    .Take(1000)
                    .ToList()
                    .Select(x => new AuditListItem
                    {
                        AuditId = x.AuditId,
                        UserId = x.UserId,
                        UserLogin = x.UserLogin,
                        TableName = x.TableName,
                        Operation = x.Operation,
                        OperationText = GetOperationText(x.Operation),
                        RowId = x.RowId,
                        OldValue = x.OldValue,
                        NewValue = x.NewValue,
                        OldValueShort = CutText(x.OldValue, 70),
                        NewValueShort = CutText(x.NewValue, 70),
                        IpAddress = x.IpAddress,
                        DateTime = x.DateTime,
                        DateTimeText = x.DateTime.ToString("dd.MM.yyyy HH:mm:ss")
                    })
                    .ToList();
            }
        }

        private void AuditListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AuditListItem selected = AuditListView.SelectedItem as AuditListItem;

            if (selected == null)
                return;

            _selectedAuditId = selected.AuditId;

            OldValueTextBox.Text = selected.OldValue;
            NewValueTextBox.Text = selected.NewValue;
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            LoadAudit();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            LoadAudit();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading)
                return;

            LoadAudit();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _isLoading = true;
            LoadFilters();
            _isLoading = false;

            LoadAudit();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            _isLoading = true;

            UserComboBox.SelectedValue = 0;
            TableComboBox.SelectedIndex = 0;
            OperationComboBox.SelectedValue = "";

            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;

            SearchTextBox.Clear();

            _isLoading = false;

            LoadAudit();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAuditId == null)
            {
                MessageBox.Show("Выберите запись журнала для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранную запись журнала аудита?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new ПоликлиникаEntities1())
                {
                    Журнал_аудита audit = db.Журнал_аудита
                        .FirstOrDefault(a => a.ID_записи == _selectedAuditId.Value);

                    if (audit == null)
                    {
                        MessageBox.Show("Запись журнала не найдена.");
                        return;
                    }

                    db.Журнал_аудита.Remove(audit);
                    db.SaveChanges();
                }

                _selectedAuditId = null;
                OldValueTextBox.Clear();
                NewValueTextBox.Clear();

                LoadAudit();

                MessageBox.Show("Запись журнала удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении записи журнала: " + ex.Message);
            }
        }

        private static string GetOperationText(string operation)
        {
            switch (operation)
            {
                case "I":
                    return "Добавление";
                case "U":
                    return "Изменение";
                case "D":
                    return "Удаление";
                default:
                    return "Неизвестно";
            }
        }

        private static string CutText(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }
    }

    public class AuditListItem
    {
        public long AuditId { get; set; }

        public int UserId { get; set; }
        public string UserLogin { get; set; }

        public string TableName { get; set; }

        public string Operation { get; set; }
        public string OperationText { get; set; }

        public string RowId { get; set; }

        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public string OldValueShort { get; set; }
        public string NewValueShort { get; set; }

        public string IpAddress { get; set; }

        public DateTime DateTime { get; set; }
        public string DateTimeText { get; set; }
    }

    public class AuditUserComboItem
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class AuditOperationItem
    {
        public string Code { get; set; }

        public string Name { get; set; }
    }
}