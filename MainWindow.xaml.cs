using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace AppMover {

    public class ActionModel : INotifyPropertyChanged {
        public FixAction Action;
        private double _progress;
        private bool error;

        public event PropertyChangedEventHandler PropertyChanged;

        public double Progress
        {
            get { return _progress; }
        }

        public string Summary
        {
            get { return Action.Summary; }
        }

        public string Detail
        {
            get { return Action.Detail; }
        }

        public Brush ProgressColor
        {
            get
            {
                if (error) {
                    return Brushes.Red;
                } else {
                    return new SolidColorBrush(Color.FromRgb(0x06, 0xB0, 0x25));
                }
            }
        }

        public ActionModel(FixAction a) {
            Action = a;
        }

        public Task Execute(Action<double> progress) {
            return Action.Execute((v) => {
                if (v == -1) {
                    error = true;
                    _progress = 1;
                    OnPropertyChanged("Progress");
                    OnPropertyChanged("ProgressColor");
                    return;
                }
                _progress = v;
                progress(v);

                OnPropertyChanged("Progress");
            });
        }

        protected void OnPropertyChanged(string prop) {
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            });
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        ObservableCollection<ActionModel> action = new ObservableCollection<ActionModel>();
        bool closable = true;

        public MainWindow() {
            InitializeComponent();
        }

        private void Dispatch(Action action) {
            Dispatcher.Invoke(action);
        }

        private void SrcPick_Click(object sender, RoutedEventArgs e) {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Src.Text;
            DialogResult result = fbd.ShowDialog();
            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                Src.Text = fbd.SelectedPath;
            }
        }

        private void DestPick_Click(object sender, RoutedEventArgs e) {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Dest.Text;
            DialogResult result = fbd.ShowDialog();
            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                Dest.Text = fbd.SelectedPath;
            }
        }

        private void Expander_Loaded(object sender, RoutedEventArgs e) {
            // This is a workaround for a bug that Expander does not respect HorizontalAlignment=Stretch
            var g = sender as Grid;
            ContentPresenter cp = g.TemplatedParent as ContentPresenter;
            cp.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        }


        private void Preview_Click(object sender, RoutedEventArgs ev) {
            var srcAddr = Src.Text;
            var destAddr = Dest.Text;

            action.Clear();
            List.ItemsSource = action;

            if (!Directory.Exists(srcAddr)) {
                System.Windows.MessageBox.Show("Source address does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!System.IO.Path.IsPathRooted(destAddr)) {
                System.Windows.MessageBox.Show("Destination address is not valid", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (destAddr.Contains(srcAddr, StringComparison.OrdinalIgnoreCase)) {
                System.Windows.MessageBox.Show("Destination address cannot contain source address", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Src.IsEnabled = false;
            Dest.IsEnabled = false;
            Preview.IsEnabled = false;
            SrcPick.IsEnabled = false;
            DestPick.IsEnabled = false;

            // Be a little conservative. If we move across volume then we keep the backup
            if (Path.GetPathRoot(srcAddr) == Path.GetPathRoot(destAddr)) {
                action.Add(new ActionModel(new MoveAction(srcAddr, destAddr)));
            } else {
                action.Add(new ActionModel(new CopyAction(srcAddr, destAddr)));
            }

            var recv = new Action<FixAction>((a) => {
                Dispatch(() => {
                    action.Add(new ActionModel(a));
                });
            });
            var t = new Thread(() => {
                try {
                    var prevV = 0d;
                    var task = ShortcutScanner.Scan(srcAddr, destAddr, recv, (v) => {
                        if (v > prevV + 0.01) {
                            prevV = v;
                            Dispatch(() => {
                                Progress.Value = v * 0.2;
                            });
                        }
                    });

                    task.Wait();

                    prevV = 0d;
                    task = RegistryScanner.Scan(srcAddr, destAddr, recv, (v) => {
                        if (v > prevV + 0.01) {
                            prevV = v;
                            Dispatch(() => {
                                Progress.Value = v * 0.8 + 0.2;
                            });
                        }
                    });
                    task.Wait();

                    Dispatch(() => {
                        Apply.IsEnabled = true;
                        Cancel.IsEnabled = true;
                    });
                } catch (Exception e) {
                    MessageBox.Show(e.ToString());
                }
            });

            t.IsBackground = true;
            t.Start();
        }

        private void Apply_Click(object sender, RoutedEventArgs ev) {
            if (MessageBox.Show("You are about to apply the changes.\nThere may be changes to critical system files, which may cause malfunction of the operating system. As no warranty is provided for this program, you should continue at your own risk.\nDo you want to continue?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) {
                return;
            }

            Apply.IsEnabled = false;
            Cancel.IsEnabled = false;
            closable = false;

            Progress.Value = 0;

            var t = new Thread(() => {
                try {
                    double singleProgress = 0.1 / (action.Count - 1);
                    double overallProgress = 0;
                    foreach (var a in action) {
                        var singleProcessBound = a.Action is MoveAction || a.Action is CopyAction ? 0.9 : singleProgress;
                        var prevProgress = 0d;
                        var progressReporter = new Action<double>((progressValue) => {
                            overallProgress += (progressValue - prevProgress) * singleProcessBound;
                            prevProgress = progressValue;
                            Dispatch(() => {
                                Progress.Value = overallProgress;
                            });
                        });
                        var task = a.Execute(progressReporter);
                        task.Wait();
                    }
                    Dispatch(() => {
                        Src.IsEnabled = true;
                        SrcPick.IsEnabled = true;
                        Dest.IsEnabled = true;
                        DestPick.IsEnabled = true;
                        Preview.IsEnabled = true;
                        Cancel.IsEnabled = false;
                        closable = true;
                    });
                } catch (Exception e) {
                    closable = true;
                    MessageBox.Show(e.ToString());
                }
            });

            t.Start();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            Src.IsEnabled = true;
            SrcPick.IsEnabled = true;
            Dest.IsEnabled = true;
            DestPick.IsEnabled = true;
            Preview.IsEnabled = true;
            Cancel.IsEnabled = false;
            Apply.IsEnabled = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            if (!closable) {
                e.Cancel = true;
            }
        }
    }


}
