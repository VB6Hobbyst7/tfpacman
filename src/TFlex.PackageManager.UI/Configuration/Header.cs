﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.IO;
using System.Xml.Linq;
using TFlex.PackageManager.Attributes;
using TFlex.PackageManager.Common;
using TFlex.PackageManager.Editors;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace TFlex.PackageManager.Configuration
{
    /// <summary>
    /// The Configuration Header class.
    /// </summary>
    public class Header : INotifyPropertyChanged
    {
        #region private fields
        Translator_0 translator_0;
        Translator_1 translator_1;
        Translator_2 translator_2;
        Translator_3 translator_3;
        Translator_6 translator_6;
        Translator_7 translator_7;
        Translator_9 translator_9;
        Translator_10 translator_10;

        string configurationName;
        string initialCatalog;
        string targetDirectory;
        string inputExtension;
        object modules;
        object translator;
        XElement tr_data;
        XAttribute data_1_1;
        XAttribute data_1_2;
        XAttribute data_1_3;
        XAttribute data_1_4;
        XAttribute data_1_5;

        int m_index;
        int processing;
        bool isChanged;
        readonly Dictionary<int, object> translators;
        #endregion

        public Header()
        {
            configurationName = string.Empty;
            initialCatalog    = string.Empty;
            targetDirectory   = string.Empty;
            inputExtension    = "*.grb";
            translators       = new Dictionary<int, object>();

            TModules = new List<object>
            {
                new M0(),  // Document
                new M1(),  // Acad
                new M2(),  // Acis
                new M1(),  // Bitmap
                new M2(),  // Iges
                new M2(),  // Jt
                new M1(),  // Pdf
                new M2()   // Step
            };

            foreach (var m in TModules)
            {
                (m as Modules).PropertyChanged += Modules_PropertyChanged;
            }
        }

        #region event handlers
        private void DataContext_Changed(object sender, XObjectChangeEventArgs e)
        {
            IsChanged = true;

            //Debug.WriteLine(string.Format("DataContext_Changed: [{0}]", 
            //    e.ObjectChange));
        }

        private void Translator_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            IsInvalid = (sender as Category_3).HasErrors;
        }
        
        private void Modules_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var m = sender as Modules;
            string[] values = data_1_5.Value.Split(' ');

            switch (e.PropertyName)
            {
                case "Pages":
                    values[0] = m.Pages       ? "1" : "0";
                    break;
                case "Projections":
                    values[1] = m.Projections ? "1" : "0";
                    break;
                case "Variables":
                    values[2] = m.Variables   ? "1" : "0";
                    break;
                case "Links":
                    values[3] = m.Links       ? "1" : "0";
                    break;
            }

            data_1_5.Value = values.ToString(" ");
        }
        #endregion

        #region internal properties
        /// <summary>
        /// XML data context.
        /// </summary>
        internal XDocument DataContext { get; private set; }

        /// <summary>
        /// The configuration is changed.
        /// </summary>
        internal bool IsChanged
        {
            get => isChanged;
            private set
            {
                if (isChanged != value)
                {
                    isChanged = value;
                    OnPropertyChanged("IsChanged");
                }
            }
        }

        /// <summary>
        /// Validating this configuration properties.
        /// </summary>
        internal bool IsInvalid { get; private set; }

        /// <summary>
        /// Configurations directory.
        /// </summary>
        internal string UserDirectory { get; set; }

        /// <summary>
        /// Modules container for switch between translators.
        /// </summary>
        internal List<object> TModules { get; }
        #endregion

        #region public properties
        /// <summary>
        /// The configuration name definition.
        /// </summary>
        [PropertyOrder(1)]
        [CustomDisplayName(Resource.HEADER_UI, "dn1_1")]
        [CustomDescription(Resource.HEADER_UI, "dn1_1")]
        [ReadOnly(true)]
        public string ConfigurationName
        {
            get => configurationName;
            set
            {
                if (configurationName != value)
                {
                    configurationName = value;
                }
            }
        }

        /// <summary>
        /// The initial catalog definition.
        /// </summary>
        [PropertyOrder(2)]
        [CustomDisplayName(Resource.HEADER_UI, "dn1_2")]
        [CustomDescription(Resource.HEADER_UI, "dn1_2")]
        [Editor(typeof(PathEditor), typeof(UITypeEditor))]
        public string InitialCatalog
        {
            get { return initialCatalog; }
            set
            {
                if (initialCatalog != value)
                {
                    initialCatalog = value;
                    data_1_2.Value = value;

                    OnPropertyChanged("InitialCatalog");
                }
            }
        }

        /// <summary>
        /// The target directory definition.
        /// </summary>
        [PropertyOrder(3)]
        [CustomDisplayName(Resource.HEADER_UI, "dn1_3")]
        [CustomDescription(Resource.HEADER_UI, "dn1_3")]
        [Editor(typeof(PathEditor), typeof(UITypeEditor))]
        public string TargetDirectory
        {
            get => targetDirectory;
            set
            {
                if (targetDirectory != value)
                {
                    targetDirectory = value;
                    data_1_3.Value = value;

                    OnPropertyChanged("TargetDirectory");
                }
            }
        }

        /// <summary>
        /// The input file extension.
        /// </summary>
        [PropertyOrder(4)]
        [CustomDisplayName(Resource.HEADER_UI, "dn1_4")]
        [CustomDescription(Resource.HEADER_UI, "dn1_4")]
        [ItemsSource(typeof(InputExtensionItems))]
        public string InputExtension
        {
            get { return inputExtension; }
            set
            {
                if (inputExtension != value)
                {
                    inputExtension = value;
                }
            }
        }

        /// <summary>
        /// The translator modules for current selector.
        /// </summary>
        [PropertyOrder(5)]
        [CustomDisplayName(Resource.HEADER_UI, "dn1_5")]
        [CustomDescription(Resource.HEADER_UI, "dn1_5")]
        [ExpandableObject]
        [Editor(typeof(ModulesEditor), typeof(UITypeEditor))]
        public object Modules
        {
            get => modules;
            set
            {
                int index = TModules.IndexOf(value);
                if (index != m_index)
                {
                    modules = value;
                    m_index = index;
                    data_1_5.Value = (value as Modules).ToString();
                    InitTranslator();
                    DataContext.Element("configuration").Element("translator")
                        .ReplaceWith(tr_data);
                    OnPropertyChanged("Modules");
                }
            }
        }

        /// <summary>
        /// Reference on current translator.
        /// </summary>
        [Browsable(false)]
        public object Translator
        {
            get => translator;
            private set
            {
                if (translator != value)
                {
                    translator = value;
                    OnPropertyChanged("Translator");
                }
            }
        }

        /// <summary>
        /// Current processing mode.
        /// </summary>
        [Browsable(false)]
        public int Processing
        {
            get => processing;
            set 
            {
                if (processing != value)
                {
                    processing = value;
                    DataContext.Element("configuration").Element("translator")
                        .Attribute("mode").Value = processing.ToString();
                    OnPropertyChanged("Processing");
                }
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// Initialize the translator.
        /// </summary>
        private void InitTranslator()
        {
            switch (m_index)
            {
                case 0: // Document
                    if (translator_0 == null)
                    {
                        translator_0 = new Translator_0();
                        translator_0.ErrorsChanged += Translator_ErrorsChanged;
                        translator_0.NewTranslator();
                        translators.Add(m_index, translator_0);

                        tr_data = translator_0.Data;
                    }
                    break;
                case 1: // Acad
                    if (translator_1 == null)
                    {
                        translator_1 = new Translator_1();
                        translator_1.ErrorsChanged += Translator_ErrorsChanged;
                        translator_1.NewTranslator();
                        translators.Add(m_index, translator_1);

                        tr_data = translator_1.Data;
                    }
                    break;
                case 2: // ACIS
                    if (translator_2 == null)
                    {
                        translator_2 = new Translator_2();
                        translator_2.ErrorsChanged += Translator_ErrorsChanged;
                        translator_2.NewTranslator();
                        translators.Add(m_index, translator_2);

                        tr_data = translator_2.Data;
                    }
                    break;
                case 3: // Bitmap
                    if (translator_3 == null)
                    {
                        translator_3 = new Translator_3();
                        translator_3.ErrorsChanged += Translator_ErrorsChanged;
                        translator_3.NewTranslator();
                        translators.Add(m_index, translator_3);

                        tr_data = translator_3.Data;
                    }
                    break;
                case 4: // IGES
                    if (translator_6 == null)
                    {
                        translator_6 = new Translator_6();
                        translator_6.ErrorsChanged += Translator_ErrorsChanged;
                        translator_6.NewTranslator();
                        translators.Add(m_index, translator_6);

                        tr_data = translator_6.Data;
                    }
                    break;
                case 5: // JT
                    if (translator_7 == null)
                    {
                        translator_7 = new Translator_7();
                        translator_7.ErrorsChanged += Translator_ErrorsChanged;
                        translator_7.NewTranslator();
                        translators.Add(m_index, translator_7);

                        tr_data = translator_7.Data;
                    }
                    break;
                case 6: // PDF
                    if (translator_9 == null)
                    {
                        translator_9 = new Translator_9();
                        translator_9.ErrorsChanged += Translator_ErrorsChanged;
                        translator_9.NewTranslator();
                        translators.Add(m_index, translator_9);

                        tr_data = translator_9.Data;
                    }
                    break;
                case 7: // STEP
                    if (translator_10 == null)
                    {
                        translator_10 = new Translator_10();
                        translator_10.ErrorsChanged += Translator_ErrorsChanged;
                        translator_10.NewTranslator();
                        translators.Add(m_index, translator_10);

                        tr_data = translator_10.Data;
                    }
                    break;
            }

            Translator = translators[m_index];

            //Debug.WriteLine(string.Format("InitTranslator [index: {0}]", m_index));
        }

        /// <summary>
        /// Load translator data.
        /// </summary>
        internal void LoadTranslator()
        {
            switch (m_index)
            {
                case 0: // Document
                    translator_0.LoadTranslator(tr_data);
                    break;
                case 1: // Acad
                    translator_1.LoadTranslator(tr_data);
                    break;
                case 2: // ACIS
                    translator_2.LoadTranslator(tr_data);
                    break;
                case 3: // Bitmap
                    translator_3.LoadTranslator(tr_data);
                    break;
                case 4: // IGES
                    translator_6.LoadTranslator(tr_data);
                    break;
                case 5: // JT
                    translator_7.LoadTranslator(tr_data);
                    break;
                case 6: // PDF
                    translator_9.LoadTranslator(tr_data);
                    break;
                case 7: // STEP
                    translator_10.LoadTranslator(tr_data);
                    break;
            }

            //Debug.WriteLine(string.Format("LoadTranslator [index: {0}]", m_index));
        }

        /// <summary>
        /// Create new configuration.
        /// </summary>
        /// <returns>The XML-data for new configuration.</returns>
        internal XDocument NewConfiguration()
        {
            var m = (TModules[0] as Modules);

            data_1_1 = new XAttribute("value", ConfigurationName);
            data_1_2 = new XAttribute("value", InitialCatalog);
            data_1_3 = new XAttribute("value", TargetDirectory);
            data_1_4 = new XAttribute("value", InputExtension);
            data_1_5 = new XAttribute("value", m.ToString());

            XDocument document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("configuration", 
                    new XElement("header",
                        new XElement("parameter",
                            new XAttribute("name", "ConfigurationName"),
                            data_1_1),
                        new XElement("parameter",
                            new XAttribute("name", "InitialCatalog"),
                            data_1_2),
                        new XElement("parameter",
                            new XAttribute("name", "TargetDirectory"),
                            data_1_3),
                        new XElement("parameter",
                            new XAttribute("name", "InputExtension"),
                            data_1_4), 
                        new XElement("parameter", 
                            new XAttribute("name", "Modules"), 
                            data_1_5)), 
                    tr_data));

            return document;
        }

        /// <summary>
        /// The configuration task method.
        /// </summary>
        /// <param name="flag">
        /// Flags definition: (0) - read, (1) - write, (2) - delete
        /// </param>
        internal void ConfigurationTask(int flag)
        {
            var path = UserDirectory + "\\" + ConfigurationName + ".config";

            if (flag == 0 && File.Exists(path))
            {
                DataContext = XDocument.Load(path);
                
                var header = DataContext
                    .Element("configuration")
                    .Element("header");

                tr_data = DataContext
                    .Element("configuration")
                    .Element("translator");

                m_index    = int.Parse(tr_data.Attribute("type").Value);
                processing = int.Parse(tr_data.Attribute("mode").Value);

                foreach (var p in header.Elements())
                {
                    LoadHeader(p);
                }

                InitTranslator();
                LoadTranslator();
                DataContext.Changed += DataContext_Changed;
                modules = TModules[m_index];
                IsChanged = false;
            }

            if (flag != 2)
            {
                if (DataContext == null)
                {
                    m_index = 0;
                    processing = 0;
                    modules = TModules[0];
                    InitTranslator();
                    DataContext = NewConfiguration();
                    DataContext.Changed += DataContext_Changed;
                    IsChanged = false;
                }

                if (flag == 1)
                {
                    DataContext.Save(path);
                    IsChanged = false;
                }
            }
            else
            {
                File.Delete(path);
                m_index    = 0;
                processing = 0;
            }

            //Debug.WriteLine(string.Format("ConfigurationTask [flag: {0}, path: {1}]",
            //    flag, path));
        }

        /// <summary>
        /// Extension method for processing header parameters.
        /// </summary>
        /// <param name="element">Source header element.</param>
        private void LoadHeader(XElement element)
        {
            var a = element.Attribute("value");
            switch (element.Attribute("name").Value)
            {
                case "ConfigurationName":
                    configurationName = a.Value;
                    data_1_1 = a;
                    break;
                case "InitialCatalog":
                    initialCatalog = a.Value;
                    data_1_2 = a;
                    break;
                case "TargetDirectory":
                    targetDirectory = a.Value;
                    data_1_3 = a;
                    break;
                case "InputExtension":
                    inputExtension = a.Value;
                    data_1_4 = a;
                    break;
                case "Modules":
                    (TModules[m_index] as Modules).SetValue(a.Value);
                    data_1_5 = a;
                    break;
            }
        }
        #endregion

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The OpPropertyChanged event handler.
        /// </summary>
        /// <param name="name">Property name.</param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }

#pragma warning disable CA1812
    internal class InputExtensionItems : IItemsSource
    {
        public ItemCollection GetValues()
        {
            return new ItemCollection
            {
                { "*.grb", "GRB" }
            };
        }
    }
}