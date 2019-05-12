﻿using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace Karen
{
    /// <summary>
    /// Simple application. Check the XAML for comments.
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            notifyIcon = (TaskbarIcon) FindResource("NotifyIcon");

            //check if server starts with app 
            if (Karen.Properties.Settings.Default.StartServerAutomatically)
                notifyIcon.ShowBalloonTip("LANraragi", "LANraragi is starting automagically...", notifyIcon.Icon);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }
    }
}
