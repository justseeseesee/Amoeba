﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using Omnius.Utilities;
using Omnius.Security;
using Prism.Events;
using Prism.Interactivity.InteractionRequest;
using System.Runtime.Serialization;
using System.Globalization;

namespace Amoeba.Interface
{
    class CreationTimeSearchConditionsControlViewModel : ManagerBase
    {
        private Settings _settings;

        public ICollectionView ContentsView => CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<SearchCondition<SearchRange<DateTime>>> _contents = new ObservableCollection<SearchCondition<SearchRange<DateTime>>>();
        public ReactiveProperty<SearchCondition<SearchRange<DateTime>>> SelectedItem { get; private set; }
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveProperty<bool> Contains { get; private set; }
        public ReactiveProperty<DateTime> MinInput { get; private set; }
        public ReactiveProperty<DateTime> MaxInput { get; private set; }

        public ReactiveCommand AddCommand { get; private set; }
        public ReactiveCommand EditCommand { get; private set; }
        public ReactiveCommand DeleteCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private readonly object _lockObject = new object();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public CreationTimeSearchConditionsControlViewModel(IEnumerable<SearchCondition<SearchRange<DateTime>>> contents)
        {
            _contents.AddRange(contents);

            this.Init();
        }

        public IEnumerable<SearchCondition<SearchRange<DateTime>>> GetContents()
        {
            return _contents.ToArray();
        }

        public void Init()
        {
            {
                this.SelectedItem = new ReactiveProperty<SearchCondition<SearchRange<DateTime>>>().AddTo(_disposable);
                this.SelectedItem.Where(n => n != null).Subscribe(n =>
                {
                    this.Contains.Value = n.IsContains;
                    this.MinInput.Value = n.Value.Min;
                    this.MaxInput.Value = n.Value.Max;
                });

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.Contains = new ReactiveProperty<bool>(true).AddTo(_disposable);
                this.MinInput = new ReactiveProperty<DateTime>(DateTime.MinValue).AddTo(_disposable);
                this.MaxInput = new ReactiveProperty<DateTime>(DateTime.Now).AddTo(_disposable);

                this.AddCommand = new ReactiveCommand().AddTo(_disposable);
                this.AddCommand.Subscribe(() => this.Add()).AddTo(_disposable);

                this.EditCommand = this.SelectedItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.EditCommand.Subscribe(() => this.Edit()).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.Delete()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(SearchInfoEditWindow), nameof(CreationTimeSearchConditionsControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.Sort(null);
            }
        }

        private void Sort(string propertyName)
        {
            if (propertyName == null)
            {
                this.ContentsView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(_sortInfo.PropertyName))
                {
                    this.Sort(_sortInfo.PropertyName, _sortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_sortInfo.PropertyName == propertyName)
                {
                    if (_sortInfo.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                this.ContentsView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.Sort(propertyName, direction);
                }

                _sortInfo.Direction = direction;
                _sortInfo.PropertyName = propertyName;
            }
        }

        private void Sort(string propertyName, ListSortDirection direction)
        {
            switch (propertyName)
            {
                case "Contains":
                    this.ContentsView.SortDescriptions.Add(new SortDescription("Contains", direction));
                    break;
                case "Min":
                    this.ContentsView.SortDescriptions.Add(new SortDescription("Value.Min", direction));
                    break;
                case "Max":
                    this.ContentsView.SortDescriptions.Add(new SortDescription("Value.Max", direction));
                    break;
            }
        }

        private void Add()
        {
            var condition = new SearchCondition<SearchRange<DateTime>>(this.Contains.Value, new SearchRange<DateTime>(this.MinInput.Value, this.MaxInput.Value));
            if (_contents.Contains(condition)) return;

            _contents.Add(condition);
        }

        private void Edit()
        {
            var selectedItem = this.SelectedItem.Value;
            if (selectedItem == null) return;

            var condition = new SearchCondition<SearchRange<DateTime>>(this.Contains.Value, new SearchRange<DateTime>(this.MinInput.Value, this.MaxInput.Value));
            if (_contents.Contains(condition)) return;

            var index = _contents.IndexOf(selectedItem);
            _contents[index] = condition;
        }

        private void Delete()
        {
            var selectedItem = this.SelectedItem.Value;
            if (selectedItem == null) return;

            _contents.Remove(selectedItem);
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("SortInfo", _sortInfo);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
