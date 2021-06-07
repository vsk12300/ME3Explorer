﻿using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using LegendaryExplorer.SharedUI;
using LegendaryExplorer.SharedUI.Bases;
using LegendaryExplorer.SharedUI.Interfaces;
using LegendaryExplorer.UserControls.ExportLoaderControls;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.TLK.ME2ME3;
using static LegendaryExplorer.Tools.TlkManagerNS.TLKManagerWPF;
using HuffmanCompression = LegendaryExplorerCore.TLK.ME2ME3.HuffmanCompression;
using System.IO;
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Diagnostics;

namespace LegendaryExplorer.Tools.TlkManagerNS
{
    /// <summary>
    /// Interaction logic for TLKManagerWPF_ExportReplaceDialog.xaml
    /// </summary>
    public partial class TLKManagerWPF_ExportReplaceDialog : TrackingNotifyPropertyChangedWindowBase, IBusyUIHost
    {
        public ICommand ReplaceSelectedTLK { get; private set; }
        public ICommand ExportSelectedTLK { get; private set; }
        public ICommand EditSelectedTLK { get; private set; }

        public ObservableCollectionExtended<LoadedTLK> TLKSources { get; set; } = new ObservableCollectionExtended<LoadedTLK>();


        #region Busy variables
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _busyText;
        public string BusyText
        {
            get => _busyText;
            set => SetProperty(ref _busyText, value);
        }
        #endregion

        public TLKManagerWPF_ExportReplaceDialog(List<LoadedTLK> loadedTLKs) : base("TLKManager Export Replace Dialog", false)
        {
            TLKSources.AddRange(loadedTLKs);
            DataContext = this;
            LoadCommands();
            InitializeComponent();
        }

        private void LoadCommands()
        {
            ReplaceSelectedTLK = new RelayCommand(ReplaceTLK, TLKSelected);
            ExportSelectedTLK = new RelayCommand(ExportTLK, TLKSelected);
            EditSelectedTLK = new RelayCommand(EditTLK, CanEditTLK);
        }

        private void EditTLK(object obj)
        {
            if (TLKList.SelectedItem is LoadedTLK { embedded: true } tlk)
            {
                //TODO: Need to find a way for the export loader to register usage of the pcc.
                IMEPackage pcc = MEPackageHandler.OpenLE1Package(tlk.tlkPath);
                var export = pcc.GetUExport(tlk.exportNumber);
                var elhw = new ExportLoaderHostedWindow(new TLKEditor(), export)
                {
                    Title = $"TLK Editor - {export.UIndex} {export.InstancedFullPath} - {export.FileRef.FilePath}"
                };
                elhw.Show();
            }
        }

        private bool CanEditTLK(object obj)
        {
            //Current code checks if it is ME1 as currently only ME1 TLK can be loaded into an export loader for ME1TLKEditor.
            return TLKList.SelectedItem is LoadedTLK {embedded: true};
        }

        private void ExportTLK(object obj)
        {
            /*
            string customExportName;
            if (TLKList.SelectedItem is LoadedTLK tlk)
            {
                customExportName = $"{ (tlk.exportName)} - {Path.GetFileName(tlk.tlkPath)}.xml";
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "XML Files (*.xml)|*.xml",
                    FileName = $"{customExportName}"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    BusyText = "Exporting TLK to XML";
                    IsBusy = true;
                    var loadingWorker = new BackgroundWorker();

                    if (tlk.exportNumber != 0)
                    {
                        //ME1
                        loadingWorker.DoWork += delegate
                        {
                            using IMEPackage pcc = MEPackageHandler.OpenLE1Package(tlk.tlkPath);
                            //ME1으로 Export 시도하더라
                            var talkfile = new ME1TalkFile(pcc, tlk.exportNumber);
                            talkfile.saveToFile(saveFileDialog.FileName);
                        };
                    }
                    else
                    {
                        //ME2,ME3
                        loadingWorker.DoWork += delegate
                        {
                            var tf = new TalkFile();
                            tf.LoadTlkData(tlk.tlkPath);
                            tf.DumpToFile(saveFileDialog.FileName);
                        };
                    }
                    loadingWorker.RunWorkerCompleted += delegate
                    {
                        IsBusy = false;
                    };
                    loadingWorker.RunWorkerAsync();
                }
            }*/
            string customExportName;
            int count = 1;
            foreach (LoadedTLK tlk in TLKList.Items)
            {
                Debug.WriteLine($"{count++}/{TLKList.Items.Count}" );
                var folderBrowserDialog = new FolderBrowserDialog();
                customExportName = $"{Path.GetDirectoryName(tlk.tlkPath)}\\{tlk.insidePath} - { (tlk.exportName)} - {Path.GetFileName(tlk.tlkPath)}.xml";
                Debug.WriteLine(customExportName);
                if (folderBrowserDialog.SelectedPath != null)
                {
                    BusyText = "Exporting TLK to XML";
                    IsBusy = true;
                    var loadingWorker = new BackgroundWorker();

                    if (tlk.exportNumber != 0)
                    {
                        //ME1
                     
                            using IMEPackage pcc = MEPackageHandler.OpenLE1Package(tlk.tlkPath);
                        //ME1으로 Export 시도하더라
                        var talkfile = new ME1TalkFile(pcc, tlk.exportNumber);
                            talkfile.saveToFile(customExportName);

                        
                    }
                    else
                    {
                        //ME2,ME3
                        loadingWorker.DoWork += delegate
                        {
                            var tf = new TalkFile();
                            tf.LoadTlkData(tlk.tlkPath);
                            tf.DumpToFile(customExportName);
                        };
                    }
                    loadingWorker.RunWorkerCompleted += delegate
                    {
                        IsBusy = false;
                    };
                    loadingWorker.RunWorkerAsync();
                }
            }
        }
 

        private void ReplaceTLK(object obj)
        {
            /*
            if (TLKList.SelectedItem is LoadedTLK tlk)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Filter = "XML Files (*.xml)|*.xml"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    BusyText = "Converting XML to TLK";
                    IsBusy = true;
                    var replacingWork = new BackgroundWorker();

                    if (tlk.exportNumber != 0)
                    {
                        //ME1
                        replacingWork.DoWork += delegate
                        {
                            LegendaryExplorerCore.TLK.ME1.HuffmanCompression compressor = new();
                            compressor.LoadInputData(openFileDialog.FileName);
                            using IMEPackage pcc = MEPackageHandler.OpenME1Package(tlk.tlkPath);
                            compressor.serializeTalkfileToExport(pcc.GetUExport(tlk.exportNumber), true);
                        };
                    }
                    else
                    {
                        //ME2,ME3

                        replacingWork.DoWork += delegate
                        {
                            var hc = new HuffmanCompression();
                            hc.LoadInputData(openFileDialog.FileName);
                            hc.SaveToFile(tlk.tlkPath);
                        };
                    }
                    replacingWork.RunWorkerCompleted += delegate
                    {
                        IsBusy = false;
                    };
                    replacingWork.RunWorkerAsync();
                }
            }*/
            if (TLKList.SelectedItem is LoadedTLK tlk)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Filter = "XML Files (*.xml)|*.xml"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    BusyText = "Converting XML to TLK";
                    IsBusy = true;
                    var replacingWork = new BackgroundWorker();

                    if (tlk.exportNumber != 0)
                    {
                        //ME1
                        replacingWork.DoWork += delegate
                        {
                            LegendaryExplorerCore.TLK.ME1.HuffmanCompression compressor = new();
                            compressor.LoadInputData(openFileDialog.FileName);
                            using IMEPackage pcc = MEPackageHandler.OpenME1Package(tlk.tlkPath);
                            compressor.serializeTalkfileToExport(pcc.GetUExport(tlk.exportNumber), true);
                        };
                    }
                    else
                    {
                        //ME2,ME3

                        replacingWork.DoWork += delegate
                        {
                            var hc = new HuffmanCompression();
                            hc.LoadInputData(openFileDialog.FileName);
                            hc.SaveToFile(tlk.tlkPath);
                        };
                    }
                    replacingWork.RunWorkerCompleted += delegate
                    {
                        IsBusy = false;
                    };
                    replacingWork.RunWorkerAsync();
                }
            }
        }

        private bool TLKSelected(object obj)
        {
            return TLKList.SelectedItem != null;
        }
    }
}
