﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Xml;
using ME3Explorer.Packages;
using ME3Explorer.SharedUI;
using ME1Explorer.Unreal.Classes;
using static ME1Explorer.Unreal.Classes.TalkFile;
using Microsoft.Win32;
using System.Threading;

namespace ME3Explorer.ME1TlkEditor
{
    /// <summary>
    /// Interaction logic for ME1TlkEditorWPF.xaml
    /// </summary>
    public partial class ME1TlkEditorWPF : ExportLoaderControl
    {
        public List<TLKStringRef> LoadedStrings; //Loaded TLK
        public ObservableCollectionExtended<TLKStringRef> CleanedStrings { get; } = new ObservableCollectionExtended<TLKStringRef>(); // Displayed
        private bool itemSelected;
        private int lastSearchIndex;
        private bool xmlUp;

        public ME1TlkEditorWPF()
        {
            DataContext = this;
            InitializeComponent();
            itemSelected = false;
        }

        //SirC "efficiency is next to godliness" way of Checking export is ME1/TLK
        public override bool CanParse(IExportEntry exportEntry) => exportEntry.FileRef.Game == MEGame.ME1 && exportEntry.ClassName == "BioTlkFile";

        public override void Dispose()
        {

        }

        public override void LoadExport(IExportEntry exportEntry)
        {
            var tlkFile = new ME1Explorer.Unreal.Classes.TalkFile(exportEntry); // Setup object as TalkFile
            LoadedStrings = tlkFile.StringRefs.ToList(); //This is not binded to so reassigning is fine
            CleanedStrings.ClearEx(); //clear strings Ex does this in bulk (faster)
            CleanedStrings.AddRange(LoadedStrings.Where(x => x.StringID > 0).ToList()); //nest it remove 0 strings.
            CurrentLoadedExport = exportEntry;
            itemSelected = false;
            lastSearchIndex = 0;
            EnableSave(false);
            EnableCommit(false);
            editBox.Text = "No strings loaded"; //Reset ability to save, reset edit box if export changed.
        }

        public override void UnloadExport()
        {
            EnableCommit(false);
        }


        private void Evt_Commit(object sender, RoutedEventArgs e)
        {
            ME1Explorer.HuffmanCompression huff = new ME1Explorer.HuffmanCompression();
            huff.LoadInputData(LoadedStrings);
            huff.serializeTLKStrListToExport(CurrentLoadedExport);
            EnableCommit(false);
        }

        private void DisplayedString_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = DisplayedString_ListBox.SelectedItem as TLKStringRef;

            if (selectedItem != null)
            {
                editBox.Text = selectedItem.Data;
                itemSelected = true;
                EnableSave(false);
            }
        }

        private void Evt_TextEdited(object sender, TextChangedEventArgs e)
        {
            if (itemSelected)
            {
                var selectedItem = DisplayedString_ListBox.SelectedItem as TLKStringRef;
                if (editBox.Text != selectedItem.Data)
                {
                    EnableSave(true);
                }
            }
        }

        private void SaveButton_Clicked(object sender, RoutedEventArgs e)
        {
            var selectedItem = DisplayedString_ListBox.SelectedItem as TLKStringRef;

            if (selectedItem != null)
            {
                selectedItem.Data = editBox.Text;
                EnableSave(false);
                EnableCommit(true);
            }
        }

        private void Evt_SetID(object sender, RoutedEventArgs e)
        {
            SetNewID();
        }

        public int DlgStringID(int curID) //Dialog tlkstring id
        {
            var newID = 0;
            bool isValid = false;
            while (!isValid)
            {
                PromptDialog inst = new PromptDialog("Set new string ID", "TLK Editor", curID.ToString(), false, PromptDialog.InputType.Text);
                inst.ShowDialog();

                if (int.TryParse(inst.ResponseText, out int newIDInt))
                {
                    //test result is an acceptable input
                    if (newIDInt > 0)
                    {
                        isValid = true;
                        newID = newIDInt;
                        break;
                    }
                    MessageBox.Show("String ID must be a positive whole number");
                }
                else
                {
                    MessageBox.Show("String ID must be a positive whole number");
                }
            }

            if (isValid)
            {
                return newID;
            }
            return curID;
        }

        private void Evt_AddString(object sender, RoutedEventArgs e)
        {
            var blankstringref = new TLKStringRef(100, 1, "New Blank Line");
            LoadedStrings.Add(blankstringref);
            CleanedStrings.Add(blankstringref);
            int cntStrings = CleanedStrings.Count(); // Find number of strings.
            DisplayedString_ListBox.SelectedIndex = cntStrings - 1; //Set focus to new line (which is the last one)
            DisplayedString_ListBox.ScrollIntoView(DisplayedString_ListBox.SelectedItem); //Scroll to last item
            SetNewID();
            EnableCommit(true);
        }

        private void Evt_DeleteString(object sender, RoutedEventArgs e)
        {
            var selectedItem = DisplayedString_ListBox.SelectedItem as TLKStringRef;
            CleanedStrings.Remove(selectedItem);
            LoadedStrings.Remove(selectedItem);
            itemSelected = false;
            EnableCommit(true);
        }

        private void Evt_ExportXML(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml",
                FileName = CurrentLoadedExport.ObjectName + ".xml"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                ME1Explorer.Unreal.Classes.TalkFile talkfile = new ME1Explorer.Unreal.Classes.TalkFile(CurrentLoadedExport);
                talkfile.saveToFile(saveFileDialog.FileName);
            }

        }

        private void Evt_ImportXML(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "XML Files (*.xml)|*.xml"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ME1Explorer.HuffmanCompression compressor = new ME1Explorer.HuffmanCompression();
                compressor.LoadInputData(openFileDialog.FileName);
                compressor.serializeTLKStrListToExport(CurrentLoadedExport);
            }
        }

        private void Evt_ViewXML(object sender, RoutedEventArgs e)
        {
            if (!xmlUp)
                {
                StringBuilder xmlTLK = new StringBuilder();
                using (StringWriter stringWriter = new StringWriter(xmlTLK))
                {
                    using (XmlTextWriter writer = new XmlTextWriter(stringWriter))
                    {
                        writer.Formatting = Formatting.Indented;
                        writer.Indentation = 4;

                        writer.WriteStartDocument();
                        writer.WriteStartElement("tlkFile");
                        writer.WriteAttributeString("Name", Name);

                        for (int i = 0; i < LoadedStrings.Count; i++)
                        {
                            writer.WriteStartElement("string");
                            writer.WriteStartElement("id");
                            writer.WriteValue(LoadedStrings[i].StringID);
                            writer.WriteEndElement(); // </id>
                            writer.WriteStartElement("flags");
                            writer.WriteValue(LoadedStrings[i].Flags);
                            writer.WriteEndElement(); // </flags>
                            if (LoadedStrings[i].Flags != 1)
                                writer.WriteElementString("data", "-1");
                            else
                                writer.WriteElementString("data", LoadedStrings[i].Data);
                            writer.WriteEndElement(); // </string>
                        }
                        writer.WriteEndElement(); // </tlkFile>
                    }
                }
                popoutXmlBox.Text = xmlTLK.ToString();
                popupDlg.Height = LowerDock.ActualHeight + DisplayedString_ListBox.ActualHeight;
                popupDlg.Width = DisplayedString_ListBox.ActualWidth;
                btnViewXML.ToolTip = "Close XML View.";
                popupDlg.IsOpen = true;
                xmlUp = true;
            }
        }

        private async void Evt_CloseXML(object sender, EventArgs e)
        {
            await System.Threading.Tasks.Task.Delay(100);  //Catch double clicks of XML button 
            xmlUp = false;
            btnViewXML.ToolTip = "View as XML.";

        }

        private void EnableSave(bool enableSave)
        {
            btnSaveEdit.IsEnabled = enableSave;
            if (enableSave)
            {
                btnSaveEdit.FontWeight = FontWeights.Bold; //Enable save button
            }
            else
            {
                btnSaveEdit.FontWeight = FontWeights.Normal; //Reset save button
            }

        }

        private void EnableCommit(bool enableCmt)
        {
            btnCommit.IsEnabled = true;
            if (enableCmt)
            {
                btnCommit.Foreground = Brushes.Red;
                btnCommit.FontWeight = FontWeights.Bold; //Enabled
            }
            else
            {
                btnCommit.Foreground = Brushes.DarkGray;
                btnCommit.FontWeight = FontWeights.Normal; //Reset
            }
        }

        private void SetNewID()
        { 
            var selectedItem = DisplayedString_ListBox.SelectedItem as TLKStringRef;
            if (selectedItem != null)
            {

                var stringRefNewID = DlgStringID(selectedItem.StringID); //Run popout box to set tlkstring id
                selectedItem.StringID = stringRefNewID;
                EnableCommit(true);
            }
        }

        private void Evt_Search(object sender, RoutedEventArgs e)
        {
            //if (DisplayedString_ListBox.SelectedIndex >= 0)
            //{
            //    lastSearchIndex = DisplayedString_ListBox.SelectedIndex + 1;
            //}
            //else
            //{
            //    lastSearchIndex = 0;
            //}

            string searchTerm = boxSearch.Text.ToLower();
            int foundIndex = -1;
            for (int i = lastSearchIndex; i < CleanedStrings.Count; i++)
            {
                if (CleanedStrings[i].StringID.ToString().Contains(searchTerm))             //ID Search
                {
                    foundIndex = i;
                    break;
                }
                else if (CleanedStrings[i].Data != null)
                {
                    if (CleanedStrings[i].Data.ToLower().Contains(searchTerm))             //Data Search
                    {
                        foundIndex = i;
                        break;
                    }
                }
            }

            if (foundIndex <= -1)
            {
                MessageBox.Show("Not Found");
                lastSearchIndex = 0;
            }
            else
            {
                DisplayedString_ListBox.SelectedIndex = foundIndex;
                DisplayedString_ListBox.ScrollIntoView(DisplayedString_ListBox.SelectedItem);
                lastSearchIndex = foundIndex + 1;
            }
        }

    }
}