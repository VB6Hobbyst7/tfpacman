﻿using System.Windows;

namespace TFlex.PackageManager.UI
{
    /// <summary>
    /// Interaction logic for PropertiesUI.xaml
    /// </summary>
    public partial class PropertiesUI : Window
    {
        public PropertiesUI(object obj)
        {
            InitializeComponent();
            propertyGrid.SelectedObject = obj;
        }
    }
}