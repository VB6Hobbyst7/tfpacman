﻿using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TFlex.PackageManager.Common;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using UndoRedoFramework;

#pragma warning disable CA1721

namespace TFlex.PackageManager.Controls
{
    /// <summary>
    /// Interaction logic for InputScaleControl.xaml
    /// </summary>
    public partial class InputScaleControl : UserControl, ITypeEditor
    {
        int index = -1;
        readonly Dictionary<string, decimal> scale;
        readonly string other;
        UndoRedo<decimal?> value;

        public InputScaleControl()
        {
            InitializeComponent();

            scale = new Dictionary<string, decimal>
            {
                { Resource.GetString(Resource.TRANSLATOR_0, "dn1_3_1", 0), 99999 },
                { "1:1",    1 },
                { "1:2",    0.5m },
                { "1:4",    0.25m },
                { "1:5",    0.2m },
                { "1:10",   0.1m },
                { "1:15",   0.0666666666666666666666666667m },
                { "1:20",   0.05m },
                { "1:25",   0.04m },
                { "1:40",   0.025m },
                { "1:50",   0.02m },
                { "1:75",   0.0133333333333333333333333333m },
                { "1:100",  0.01m },
                { "1:200",  0.005m },
                { "1:250",  0.004m },
                { "1:500",  0.002m },
                { "1:1000", 0.001m },
                { "2:1",    2 },
                { "2.5:1",  2.5m },
                { "4:1",    4 },
                { "5:1",    5 },
                { "10:1",   10 },
                { "20:1",   20 },
                { "50:1",   50 },
                { "100:1",  100 }
            };

            foreach (var i in scale) comboBox.Items.Add(i.Key);
            other = Resource.GetString(Resource.TRANSLATOR_0, "dn1_3_2", 0);

            UndoRedoManager.CommandDone += UndoRedoManager_CommandDone;
        }

        private void UndoRedoManager_CommandDone(object sender, CommandDoneEventArgs e)
        {
            if (!(DataContext is PropertyItem p))
                return;

            switch (e.CommandDoneType)
            {
                case CommandDoneType.Undo:
                    if (UndoRedoManager.RedoCommands.Count() > 0 &&
                        UndoRedoManager.RedoCommands.Last() == p.PropertyName && Value != value.Value)
                    {
                        decimalUpDown.Value = value.Value;
                    }
                    break;
                case CommandDoneType.Redo:
                    if (UndoRedoManager.UndoCommands.Count() > 0 &&
                        UndoRedoManager.UndoCommands.Last() == p.PropertyName && Value != value.Value)
                    {
                        decimalUpDown.Value = value.Value;
                    }
                    break;
            }

            //Debug.WriteLine(string.Format("Action: [name: {0}, value: {1}, type: {2}]",
            //    p.PropertyName, p.Value, e.CommandDoneType));
        }

        public decimal? Value
        {
            get => (decimal?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(decimal?), typeof(InputScaleControl),
                new FrameworkPropertyMetadata(null, 
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            Binding binding = new Binding("Value")
            {
                Source = propertyItem,
                ValidatesOnExceptions = true,
                ValidatesOnDataErrors = true,
                Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
            };
            BindingOperations.SetBinding(this, ValueProperty, binding);

            decimalUpDown.Value = Value;
            decimalUpDown.ValueChanged += DecimalUpDown_ValueChanged;
            if (scale.ContainsValue(Value.Value))
            {
                for (int i = 0; i < scale.Count; i++)
                {
                    if (scale.ElementAt(i).Value == Value)
                    {
                        comboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
                comboBox.SelectedIndex = 0;

            comboBox.SelectionChanged += ComboBox_SelectionChanged;
            value = new UndoRedo<decimal?>(Value);

            return this;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string[] _scale;

            if (comboBox.SelectedIndex == 0 && decimalUpDown.Value == 99999)
            {
                decimalUpDown.IsEnabled = false;
            }
            else if (comboBox.SelectedIndex == 0 && decimalUpDown.Value != 99999)
            {
                decimalUpDown.Value = scale.ElementAt(comboBox.SelectedIndex).Value;
                decimalUpDown.IsEnabled = false;
            }
            else if (comboBox.SelectedIndex != index && (_scale = ((string)comboBox.SelectedValue).Split(':')).Length == 2)
            {
                decimal.TryParse(_scale[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal num1);
                decimal.TryParse(_scale[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal num2);

                decimalUpDown.Value = (num1 / num2);
                decimalUpDown.IsEnabled = true;
            }
        }

        private void DecimalUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(DataContext is PropertyItem p))
                return;

            if (value.Value != decimalUpDown.Value)
            {
                using (UndoRedoManager.Start(p.PropertyName))
                {
                    value.Value = decimalUpDown.Value;
                    UndoRedoManager.Commit();
                }
            }

            if (scale.ContainsValue(decimalUpDown.Value.Value))
            {
                for (int i = 0; i < scale.Count; i++)
                {
                    if (scale.ElementAt(i).Value == decimalUpDown.Value.Value)
                    {
                        comboBox.SelectedIndex = (index = i);
                        break;
                    }
                }

                if (comboBox.Items.Count > 25)
                    comboBox.Items.RemoveAt(25);
            }
            else if (comboBox.Items.Count == 25)
            {
                comboBox.Items.Add(other);
                comboBox.SelectedIndex = 25;
            }

            //Debug.WriteLine(string.Format("PropertyItem: [name: {0}, value: {1}, can undo: {2}, can redo: {3}]",
            //    p.PropertyName, p.Value, UndoRedoManager.CanUndo, UndoRedoManager.CanRedo));
        }
    }
}
