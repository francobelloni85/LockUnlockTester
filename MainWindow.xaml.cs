using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;

namespace CG.LockUnlockTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow(ManageShortcutCallback shortcutCallback)
        {
            DataContext = new MainWindowViewModel(shortcutCallback);
            InitializeComponent();
        }

    }
}
