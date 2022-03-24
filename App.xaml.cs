using LockUnlock;
using System;
using System.Windows;

namespace CG.LockUnlockTester
{
    public delegate void ManageShortcutCallback(bool value);

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Create the startup window
            MainWindow wnd = new MainWindow();
            // Do stuff here, e.g. to the window
            wnd.Title = "Test Lock&Unlock";
            // Show the window
            wnd.Show();

        }


        public void ManageShortcut(bool enable)
        {
            
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            
        }
    }
}
