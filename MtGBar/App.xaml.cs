﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Bazam.KeyAdept.Infrastructure;
using Hardcodet.Wpf.TaskbarNotification;
using MtGBar.Infrastructure;
using MtGBar.Infrastructure.Utilities;
using MtGBar.Infrastructure.Utilities.Updates;

namespace MtGBar
{
    public partial class App : Application
    {
        private bool _IWelcomedThem = false;
        private TaskbarIcon _TheTaskBarIcon = null;

        private TaskbarIcon TheTaskBarIcon
        {
            get
            {
                if (_TheTaskBarIcon == null) {
                    _TheTaskBarIcon = (TaskbarIcon)FindResource("TheTaskBarIcon");
                }
                return _TheTaskBarIcon;
            }
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppState.Instance.LoggingNinja.LogMessage("UNCAUGHT: " + e.Exception.Message + " STACK TRACE: " + e.Exception.StackTrace);
            e.Handled = true;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // find our taskbar icon so we can do horrible things to it
            if (TheTaskBarIcon != null) {
                TheTaskBarIcon.Dispose();
            }

            try {
                AppState.Instance.HotkeyRegistrar.UnregisterAllHotkeys();
            }
            catch (Exception) {
                // LIKE I GIVE A FUCK
            }
        }

        private void MelekDataStore_Loaded(object sender, EventArgs e)
        {
            if (!_IWelcomedThem) {
                // welcome, noob
                string welcome = AppConstants.APPNAME + " is online and ready to help.\n\n";
                if (AppState.Instance.Settings.Hotkey != null) {
                    welcome += "Hit " + AppState.Instance.Settings.Hotkey.ToString() + " to get started!";
                }
                else {
                    welcome += "Looks like you don't have a hotkey set up right now. Right-click here and choose Settings to pick one, or just choose Browse to party without a hotkey. But that's way less fun.";
                }

                TalkAtcha.TalkAtEm("Ready!", welcome);
                Dispatcher.Invoke(() => {
                    (FindResource("TheTaskBarIcon") as TaskbarIcon).ToolTipText = AppConstants.APPNAME;
                }, DispatcherPriority.Normal);
                _IWelcomedThem = true;
            }

            // start the updater clock so it polls for updates
            Updater.Instance.UpdateFound += () => {
                TalkAtcha.TalkAtEm("Update!", "There's an update available for " + AppConstants.APPNAME + "! You can download it as quick as instant-speed removal by closing and reopening the app.");
            };
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // first of all, omg, make sure there's only one instance running. it gets weird fast otherwise.
            Process thisProc = Process.GetCurrentProcess();
            if (Process.GetProcessesByName(thisProc.ProcessName).Length > 1) {
                MessageBox.Show("Oops. " + AppConstants.APPNAME + " is already running. Check your System Tray and you should should see it there. If you're having trouble, close all instances of MtGBar, start it up again, and use the \"Settings\" menu item to contact the developer. Sorry!", AppConstants.APPNAME);
                Application.Current.Shutdown();
                return;
            }

            // make sure our user app data stuff is there
            FileSystemManager.Init();

            // as soon as a melekdatastore instance is referenced, it'll start loading, so know that shit and register a listener that will do stuff when it's done
            AppState.Instance.MelekDataStore.DataLoaded += MelekDataStore_Loaded;
            // also say my name say my name when the thing has new packages
            AppState.Instance.MelekDataStore.PackagesUpdated += (newPackages) => {
                // set the lastimagecheck to a low value so that next time the clock comes around, new image data is downloaded (for the new packages)
                AppState.Instance.Settings.LastImageCheck = DateTime.MinValue;
                AppState.Instance.Settings.Save();

                if (newPackages.Length == 1) {
                    TalkAtcha.TalkAtEm("New data: " + newPackages[0].Name, AppConstants.APPNAME + " has downloaded new data about " + newPackages[0].Name + ". Search for your favorite spoiler now!");
                }
                else if (newPackages.Length > 1) {
                    TalkAtcha.TalkAtEm("New data!", AppConstants.APPNAME + " just downloaded a bunch of new data. Search for your favorite new card now!");
                }
            };

            // pretty sure here's where we should hook up the hotkey
            if (AppState.Instance.Settings.Hotkey != null) {
                AppState.Instance.RegisterHotkey(AppState.Instance.Settings.Hotkey);
            }

            // set up the tooltip
            (FindResource("TheTaskBarIcon") as TaskbarIcon).ToolTip = AppConstants.APPNAME;

            // HEY! LISTEN!
            // ... to the hotkey registrar so we can tell the user if they try to do something that will fuck stuff up
            AppState.Instance.HotkeyRegistrar.UnavailableHotkeyRegistered += (HotkeyEventArgs args) => {
                TalkAtcha.TalkAtEm("Oops.", AppConstants.APPNAME + " tried to register its hotkey but couldn't. This is usually because it's trying to use a hotkey that some other software is using. Try visiting settings and changing the hotkey. Sorry :(");
            };

            // let them know that stuff is loading and will be done asap
            TalkAtcha.TalkAtEm("Welcome to " + AppConstants.APPNAME + "!", "Loading up the sweet dataz...");
        }
    }
}