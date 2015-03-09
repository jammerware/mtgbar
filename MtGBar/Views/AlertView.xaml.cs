﻿using System.Windows;
using FirstFloor.ModernUI.Windows.Controls;
using MtGBar.ViewModels;

namespace MtGBar.Views
{
    public partial class AlertView : ModernWindow
    {
        public AlertView()
        {
            InitializeComponent();
        }

        private void this_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AlertViewModel vm = (DataContext as AlertViewModel);

            TheLinkGroup.DisplayName = vm.WindowTitle;
            TheLink.DisplayName = vm.WindowSubTitle;
            vm.CloseRequested += (hey, theyWantToCloseIt) => { this.Close(); };
        }
    }
}