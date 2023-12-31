﻿using SWICD.ViewModels;
using SWICD.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SWICD.Pages
{
    /// <summary>
    /// Interaktionslogik für ProfileEditPage.xaml
    /// </summary>
    public partial class ProfileEditPage : Page
    {
        public ProfileEditPage(ControllerConfig controllerConfig = null)
        {
            InitializeComponent();
            this.DataContext = new ProfileEditPageViewModel(controllerConfig);
        }
    }
}
