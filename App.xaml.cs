using System;
using System.Windows;

namespace AppMover {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        [STAThread]
        public static void Main(string[] args) {
            try {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
            }
        }

    }
}
