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
        KeyboardListener KListener = new KeyboardListener();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ManageShortcutCallback shortcutCallback = new ManageShortcutCallback(ManageShortcut);

            // Create the startup window
            MainWindow wnd = new MainWindow(shortcutCallback);
            // Do stuff here, e.g. to the window
            wnd.Title = "Test Lock&Unlock";
            // Show the window
            wnd.Show();
            KListener.KeyDown += new RawKeyEventHandler(KListener_KeyDown);
        }

        void KListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            Console.WriteLine(args.Key.ToString());
            Console.WriteLine(args.ToString()); // Prints the text of pressed button, takes in account big and small letters. E.g. "Shift+a" => "A"
        }


        public void ManageShortcut(bool enable)
        {
            if (enable)
            {
                KListener.EnableShortcut();
            }
            else {
                KListener.DisableShortcut();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            KListener.Dispose();
        }
    }
}
