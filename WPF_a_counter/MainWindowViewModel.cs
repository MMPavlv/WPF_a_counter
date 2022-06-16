using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPF_a_counter
{
    public enum ItemStatus
    {
        Pending,
        Analyzed,
        Error,
        Cancelled
    }

    public enum GlobalStatus
    {
        Idling,
        Loading,
        Finished, 
        Canseled
    }

    public class Item : INotifyPropertyChanged
    {
        private string _Name;
        private int _Matches;
        private ItemStatus _Status = ItemStatus.Pending;
        private bool _IsBest = false;
        public Item(string name, int matches)
        {
            Name = name;
            Matches = matches;
        }

        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    OnPropertyChange("Name");
                }
            }
        }
        public int Matches
        {
            get { return _Matches; }
            set
            {
                if (_Matches != value)
                {
                    _Matches = value;
                    OnPropertyChange("Matches");
                }
            }
        }

        public ItemStatus Status
        {
            get { return _Status; }
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    OnPropertyChange("Status");
                }
            }
        }

        public bool IsBest
        {
            get { return _IsBest; }
            set
            {
                if (_IsBest != value)
                {
                    _IsBest = value;
                    OnPropertyChange("IsBest");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChange(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    class MainWindowViewModel : INotifyPropertyChanged
    {
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private readonly ObservableCollection<Item> _coll = new ObservableCollection<Item>();
        public ObservableCollection<Item> Items
        {
            get { return _coll; }
        }

        private GlobalStatus _GlobalStatus = GlobalStatus.Idling;
        public GlobalStatus Status
        {
            get { return _GlobalStatus; }
            set
            {
                if (_GlobalStatus != value)
                {
                    _GlobalStatus = value;
                    OnPropertyChange("Status");
                }
            }
        }

        private int _Progress_min = 0;
        public int Progress_min
        {
            get { return _Progress_min; }
            set
            {
                if (_Progress_min != value)
                {
                    _Progress_min = value;
                    OnPropertyChange("Progress_min");
                }
            }
        }

        private int _Progress_max = 1;
        public int Progress_max
        {
            get { return _Progress_max; }
            set
            {
                if (_Progress_max != value)
                {
                    _Progress_max = value;
                    OnPropertyChange("Progress_max");
                }
            }
        }

        private int _Progress_val = 0;
        public int Progress_val
        {
            get { return _Progress_val; }
            set
            {
                if (_Progress_val != value)
                {
                    _Progress_val = value;
                    OnPropertyChange("Progress_val");
                }
            }
        }

        public MainWindowViewModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ICommand _clickCommand;
        public ICommand ClickCommand
        {
            get
            {
                return _clickCommand ?? (_clickCommand = new CommandHandler(() => AnalyzeAction(Items, cancellationToken), () => CanExecute));
            }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                return _cancelCommand ?? (_cancelCommand = new CommandHandler(() => CancelAction(cancellationToken), () => CanExecute));
            }
        }

        private ICommand _loadCommand;
        public ICommand LoadCommand
        {
            get
            {
                return _loadCommand ?? (_loadCommand = new CommandHandler(() => LoadAction(Items), () => CanExecute));
            }
        }
        public bool CanExecute
        {
            get
            {
                return true;
            }
        }

        public async void AnalyzeAction(ObservableCollection<Item> items, CancellationTokenSource token)
        {
            Progress_val = 0;
            Status = GlobalStatus.Loading;

            for (int i = 0; i < items.Count; i++)
            {
                items[i].Status = ItemStatus.Pending;
                items[i].Matches = 0;
                items[i].IsBest = false;
            }

            if (token.IsCancellationRequested)
                token = new CancellationTokenSource();

            int min_value = 0;
            for (int i = 0; i < items.Count; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false); //optional
                
                if (!token.IsCancellationRequested)
                {
                    items[i].Matches = Service.CountAInUrl(items[i].Name).GetAwaiter().GetResult();

                    if (items[i].Matches == -1)
                    {
                        items[i].Status = ItemStatus.Error;
                    }
                    else
                    {
                        items[i].Status = ItemStatus.Analyzed;
                    }

                    if (items[i].Matches > min_value)
                    {
                        min_value = items[i].Matches;
                        items[i].IsBest = true;

                        for (int j = 0; j < i; j++)
                        {
                            items[j].IsBest = false;
                        }
                    }
                    Progress_val += 1;
                }
                else
                {
                    items[i].Matches = 0;
                    items[i].Status = ItemStatus.Cancelled;
                }
            }

            Status = GlobalStatus.Finished;
        }


        public void CancelAction(CancellationTokenSource token)
        {
            if (token != null || !token.IsCancellationRequested)
            {
                Status = GlobalStatus.Canseled;
                token.Cancel();
            }
        }

        private void LoadAction(ObservableCollection<Item> items)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";

            bool? result = dialog.ShowDialog();

            int counter = 0;
            if (result == true)
            {
                items.Clear();
                foreach (string line in System.IO.File.ReadLines(dialog.FileName))
                {
                    items.Add(new Item(line, 0));
                    counter++;
                }
                Progress_max = counter;
                Progress_val = 0;
                Status = GlobalStatus.Idling;
            }
        }
    }

    public class CommandHandler : ICommand
    {
        private Action _action;
        private Func<bool> _canExecute;

        /// <summary>
        /// Creates instance of the command handler
        /// </summary>
        /// <param name="action">Action to be executed by the command</param>
        /// <param name="canExecute">A bolean property to containing current permissions to execute the command</param>
        public CommandHandler(Action action, Func<bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Wires CanExecuteChanged event 
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Forcess checking if execute is allowed
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute.Invoke();
        }

        public void Execute(object parameter)
        {
            _action();
        }
    }
}
