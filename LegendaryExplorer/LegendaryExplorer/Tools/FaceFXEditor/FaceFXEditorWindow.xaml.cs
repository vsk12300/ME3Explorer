﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LegendaryExplorer.Misc;
using LegendaryExplorer.SharedUI;
using LegendaryExplorer.SharedUI.Bases;
using LegendaryExplorer.SharedUI.Interfaces;
using LegendaryExplorer.UserControls.SharedToolControls;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace LegendaryExplorer.Tools.FaceFXEditor
{
    /// <summary>
    /// Interaction logic for FaceFXEditor.xaml
    /// </summary>
    public partial class FaceFXEditorWindow : WPFBase, IRecents
    {
        public ObservableCollectionExtended<ExportEntry> AnimSets { get; } = new();

        public string CurrentFile => Pcc != null ? Path.GetFileName(Pcc.FilePath) : "Select a file to load";

        private ExportEntry _selectedExport;

        public ExportEntry SelectedExport
        {
            get => _selectedExport;
            set => SetProperty(ref _selectedExport, value);
        }

        private string FileQueuedForLoad;
        private ExportEntry ExportQueuedForFocusing;
        private string LineQueuedForFocusing;

        public FaceFXEditorWindow() : base("FaceFX Editor")
        {
            InitializeComponent();
            LoadCommands();
            DataContext = this;

            RecentsController.InitRecentControl(Toolname, Recents_MenuItem, LoadFile);
        }

        public FaceFXEditorWindow(ExportEntry export, string lineName = null) : this()
        {
            FileQueuedForLoad = export.FileRef.FilePath;
            ExportQueuedForFocusing = export;
            LineQueuedForFocusing = lineName;
        }

        public ICommand OpenCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand SaveAsCommand { get; set; }
        public ICommand LoadAnimsetCommand { get; set; }

        private void LoadCommands()
        {
            OpenCommand = new GenericCommand(OpenPackage);
            SaveCommand = new GenericCommand(SavePackage, PackageIsLoaded);
            SaveAsCommand = new GenericCommand(SavePackageAs, PackageIsLoaded);
            LoadAnimsetCommand = new GenericCommand(LoadAnimset, CanLoadAnimset);
        }

        private bool CanLoadAnimset()
        {
            return SelectedExport != null;
        }

        private void LoadAnimset()
        {
            editorControl.SaveChanges();
            editorControl.LoadExport(SelectedExport);
        }

        private void SavePackageAs()
        {
            string extension = Path.GetExtension(Pcc.FilePath);
            SaveFileDialog d = new SaveFileDialog { Filter = $"*{extension}|*{extension}" };
            if (d.ShowDialog() == true)
            {
                editorControl.SaveChanges();
                Pcc.Save(d.FileName);
                MessageBox.Show("Done.");
            }
        }

        private void SavePackage()
        {
            editorControl.SaveChanges();
            Pcc.Save();
        }

        private void OpenPackage()
        {
            OpenFileDialog d = new OpenFileDialog { Filter = GameFileFilters.OpenFileFilter };
            if (d.ShowDialog() == true)
            {
                try
                {
                    LoadFile(d.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to open file:\n" + ex.Message);
                }
            }
        }

        private bool PackageIsLoaded()
        {
            System.Diagnostics.Debug.WriteLine("Package Is Loaded.");
            return Pcc != null;
        }

        public void LoadFile(string fileName)
        {
            try
            {
                LoadMEPackage(fileName);
                editorControl.UnloadExport();
                RefreshComboBox();

                Title = $"FaceFX Editor - {Pcc.FilePath}";
                RecentsController.AddRecent(fileName, false, Pcc?.Game);
                RecentsController.SaveRecentList(true);
                OnPropertyChanged(nameof(CurrentFile));
            }
            catch (Exception ex)
            {
                UnLoadMEPackage();
                MessageBox.Show($"Error:\n{ex.Message}");
            }
        }

        private void RefreshComboBox()
        {
            ExportEntry item = SelectedExport;
            AnimSets.ClearEx();
            AnimSets.AddRange(Pcc.Exports.Where(exp => exp.ClassName == "FaceFXAnimSet"));
            if (AnimSets.Contains(item))
            {
                SelectedExport = item;
            }
            else
            {
                SelectedExport = AnimSets.FirstOrDefault();
            }
        }

        public override void handleUpdate(List<PackageUpdate> updates)
        {
            IEnumerable<PackageUpdate> relevantUpdates = updates.Where(x => x.Change.HasFlag(PackageChange.Export));
            List<int> updatedExports = relevantUpdates.Select(x => x.Index).ToList();
            if (SelectedExport != null && updatedExports.Contains(SelectedExport.UIndex))
            {
                int index = SelectedExport.UIndex;
                //loaded FaceFXAnimset is no longer a FaceFXAnimset
                if (SelectedExport.ClassName != "FaceFXAnimSet")
                {
                    editorControl.UnloadExport();
                }
                else if (!this.IsForegroundWindow())
                {
                    editorControl.LoadExport(SelectedExport);
                }
                updatedExports.Remove(index);
            }
            if (updatedExports.Intersect(AnimSets.Select(exp => exp.UIndex)).Any())
            {
                RefreshComboBox();
            }
            else
            {
                if (updatedExports.Any(uIdx => Pcc.GetEntry(uIdx)?.ClassName == "FaceFXAnimSet"))
                {
                    RefreshComboBox();
                }
            }
        }

        private void WPFBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FileQueuedForLoad))
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    //Wait for all children to finish loading
                    LoadFile(FileQueuedForLoad);
                    FileQueuedForLoad = null;

                    if (AnimSets.Contains(ExportQueuedForFocusing))
                    {
                        SelectedExport = ExportQueuedForFocusing;
                        RefreshComboBox();
                        editorControl.LoadExport(SelectedExport);
                        if (LineQueuedForFocusing != null)
                        {
                            editorControl.SelectLineByName(LineQueuedForFocusing);
                        }
                    }
                    ExportQueuedForFocusing = null;
                    LineQueuedForFocusing = null;

                    Activate();
                }));
            }
        }

        public void PropogateRecentsChange(IEnumerable<RecentsControl.RecentItem> newRecents)
        {
            RecentsController.PropogateRecentsChange(false, newRecents);
        }

        public string Toolname => "FaceFXEditor";
    }
}
