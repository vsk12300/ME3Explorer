﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.Unreal;

namespace LegendaryExplorerCore.Packages
{
    public abstract partial class UnrealPackageFile : INotifyPropertyChanged
    {
        public const uint packageTagLittleEndian = 0x9E2A83C1; //Default, PC
        public const uint packageTagBigEndian = 0xC1832A9E;
        public string FilePath { get; }
        public bool IsModified { get; protected set; }
        public int FullHeaderSize { get; protected set; }
        public UnrealFlags.EPackageFlags Flags { get; protected set; }
        public int NameCount { get; protected set; }
        public int NameOffset { get; protected set; }
        public int ExportCount { get; protected set; }
        public int ExportOffset { get; protected set; }
        public int ImportCount { get; protected set; }
        public int ImportOffset { get; protected set; }
        public int NumCompressedChunksAtLoad { get; protected set; }
        public int DependencyTableOffset { get; protected set; }
        public Guid PackageGuid { get; set; }

        public bool IsCompressed => Flags.HasFlag(UnrealFlags.EPackageFlags.Compressed);

        /// <summary>
        /// A lookup table that maps the full instanced path of an entry to that entry, which makes looking up entries by name quick.
        /// ONLY WORKS properly if there are NO duplicate indexes (besides trash) in the package.
        /// Is not used if the table is not populated, methods will perform a full search.
        /// </summary>
        internal CaseInsensitiveDictionary<IEntry> EntryLookupTable;

        public enum CompressionType
        {
            None = 0,
            Zlib = 0x1, // PC ME3
            LZO = 0x2, //ME1 and ME2 PC
            LZX = 0x4, //Xbox
            LZMA = 0x8, //WiiU, PS3 
            OodleLeviathan = 0x400 // LE1?
        }

        public List<ME1TalkFile> LocalTalkFiles { get; } = new();

        public static ushort UnrealVersion(MEGame game) => game switch
        {
            MEGame.ME1 => MEPackage.ME1UnrealVersion,
            MEGame.ME2 => MEPackage.ME2UnrealVersion,
            MEGame.ME3 => MEPackage.ME3UnrealVersion,
            MEGame.LE1 => MEPackage.LE1UnrealVersion,
            MEGame.LE2 => MEPackage.LE2UnrealVersion,
            MEGame.LE3 => MEPackage.LE3UnrealVersion,
            MEGame.UDK => UDKPackage.UDKUnrealVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(game), game, null)
        };

        public static ushort LicenseeVersion(MEGame game) => game switch
        {
            MEGame.ME1 => MEPackage.ME1LicenseeVersion,
            MEGame.ME2 => MEPackage.ME2LicenseeVersion,
            MEGame.ME3 => MEPackage.ME3LicenseeVersion,
            MEGame.LE1 => MEPackage.LE1LicenseeVersion,
            MEGame.LE2 => MEPackage.LE2LicenseeVersion,
            MEGame.LE3 => MEPackage.LE3LicenseeVersion,
            MEGame.UDK => UDKPackage.UDKLicenseeVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(game), game, null)
        };

        #region Names
        protected uint namesAdded;


        // Used to make name lookups quick when doing a contains operation as this method is called
        // quite often
        protected CaseInsensitiveDictionary<int> nameLookupTable = new CaseInsensitiveDictionary<int>();

        protected List<string> names;
        public IReadOnlyList<string> Names => names;

        public bool IsName(int index) => index >= 0 && index < names.Count;

        public string GetNameEntry(int index) => IsName(index) ? names[index] : "";


        public int FindNameOrAdd(string name)
        {
            if (nameLookupTable.TryGetValue(name, out var index))
            {
                return index;
            }

            addName(name, true); //Don't bother doing a lookup as we just did one. 
            // If this was an issue it'd be a multithreading issue that still could occur and is an
            // issue in the user code
            return names.Count - 1;
        }

        protected void addName(string name, bool skipLookup = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new Exception("Cannot add a null/empty name to the list of names for a package file.\nThis is a bug in LegendaryExplorerCore.");
            }

            if (skipLookup || !nameLookupTable.TryGetValue(name, out var index))
            {
                names.Add(name);
                namesAdded++;
                nameLookupTable[name] = names.Count - 1;
                NameCount = names.Count;

                updateTools(PackageChange.NameAdd, NameCount - 1);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameCount)));
                IsModified = true;
            }
        }

        public void replaceName(int idx, string newName)
        {
            if (newName == null)
            {
                // Cannot add a null name!
                throw new ArgumentException(nameof(newName), new Exception("Cannot replace a name with a null value!"));
            }
            if (IsName(idx) && !names[idx].Equals(newName, StringComparison.InvariantCultureIgnoreCase))
            {
                nameLookupTable.Remove(names[idx]);
                names[idx] = newName;
                nameLookupTable[newName] = idx;
                IsModified = true; // Package has become modified
                updateTools(PackageChange.NameEdit, idx);
            }
        }

        /// <summary>
        /// Checks whether a name exists in the PCC and returns its index
        /// If it doesn't exist returns -1
        /// </summary>
        /// <param name="nameToFind">The name of the string to find</param>
        /// <returns></returns>
        public int findName(string nameToFind)
        {
            if (nameLookupTable.TryGetValue(nameToFind, out var index))
            {
                return index;
            }
            return -1;
        }

        public void restoreNames(List<string> list)
        {
            names = list;
            mapNames();
            NameCount = names.Count;
        }

        private void mapNames()
        {
            nameLookupTable.Clear();
            int i = 0;
            foreach (var name in names)
            {
                nameLookupTable[name] = i;
                i++;
            }
        }

        /// <summary>
        /// Gets the next available index for a given name - this counts all names, not just the instanced full name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetNextIndexForName(string name)
        {
            int index = 0;
            foreach (ExportEntry ent in exports)
            {
                if (name == ent.ObjectName && ent.ObjectName.Number > index)
                {
                    index = ent.ObjectName.Number;
                }
            }
            return index + 1;
        }

        public NameReference GetNextIndexedName(string name)
        {
            name = name.Trim().Replace(' ', '_'); //no spaces 
            return new NameReference(name, GetNextIndexForName(name));
        }

        #endregion

        #region Exports
        protected List<ExportEntry> exports;
        public IReadOnlyList<ExportEntry> Exports => exports;

        public bool IsUExport(int uindex) => uindex > 0 && uindex <= exports.Count;

        public void AddExport(ExportEntry exportEntry)
        {
            if (exportEntry.FileRef != this)
                throw new Exception("Cannot add an export entry from another package file");

            exportEntry.DataChanged = true;
            exportEntry.HeaderOffset = 1; //This will make it so when setting idxLink it knows the export has been attached to the tree, even though this doesn't do anything. Find by offset may be confused by this. Updates on save
            exportEntry.Index = exports.Count;
            exportEntry.PropertyChanged += exportChanged;
            exports.Add(exportEntry);
            // For debugging
            //if (EntryLookupTable.ContainsKey(exportEntry.InstancedFullPath))
            //{
            //    Debugger.Break();
            //}

            // We need a way to handle a clone that doesn't have a unique name! Or this system will not work
            EntryLookupTable[exportEntry.InstancedFullPath] = exportEntry; // ADD TO LOOKUP CACHE

            ExportCount = exports.Count;

            //Debug.WriteLine($@" >> Added export {exportEntry.InstancedFullPath}");


            updateTools(PackageChange.ExportAdd, exportEntry.UIndex);
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs((nameof(ExportCount));
        }

        public IEntry FindEntry(string instancedname)
        {
            if (EntryLookupTable != null && EntryLookupTable.Any())
            {
                EntryLookupTable.TryGetValue(instancedname, out var matchingEntry);
                return matchingEntry;
            }
            else
            {
                // Look at imports first
                var entry = Imports.FirstOrDefault(x => x.InstancedFullPath == instancedname) as IEntry;
                if (entry == null)
                {
                    // Look at exports
                    entry = Exports.FirstOrDefault(x => x.InstancedFullPath == instancedname) as IEntry;
                }
                return entry;
            }
        }
        public ImportEntry FindImport(string instancedname)
        {
            if (EntryLookupTable != null && EntryLookupTable.Any())
            {
                EntryLookupTable.TryGetValue(instancedname, out var matchingEntry);
                return matchingEntry as ImportEntry;
            }
            else
            {
                return Imports.FirstOrDefault(x => x.InstancedFullPath == instancedname);
            }
        }

        public ExportEntry FindExport(string instancedname)
        {
            if (EntryLookupTable != null && EntryLookupTable.Any())
            {
                EntryLookupTable.TryGetValue(instancedname, out var matchingEntry);
                return matchingEntry as ExportEntry;
            }
            else
            {
                return Exports.FirstOrDefault(x => x.InstancedFullPath == instancedname);
            }
        }

        public ExportEntry GetUExport(int uindex) => exports[uindex - 1];

        public bool TryGetUExport(int uIndex, out ExportEntry export)
        {
            if (IsUExport(uIndex))
            {
                export = GetUExport(uIndex);
                return true;
            }

            export = null;
            return false;
        }
        #endregion

        #region Imports
        protected List<ImportEntry> imports;
        public IReadOnlyList<ImportEntry> Imports => imports;

        /// <summary>
        /// Determines if this is an Import based on it's UIndex
        /// </summary>
        /// <param name="uindex"></param>
        /// <returns></returns>
        public bool IsImport(int uindex) => (uindex < 0 && uindex > int.MinValue && Math.Abs(uindex) <= ImportCount);

        /// <summary>
        /// Adds an import to the tree. This method is used to add new imports.
        /// </summary>
        /// <param name="importEntry"></param>
        public void AddImport(ImportEntry importEntry)
        {
            if (importEntry.FileRef != this)
                throw new Exception("you cannot add a new import entry from another package file, it has invalid references!");

            importEntry.Index = imports.Count;
            importEntry.PropertyChanged += importChanged;
            importEntry.HeaderOffset = 1; //This will make it so when setting idxLink it knows the import has been attached to the tree, even though this doesn't do anything. Find by offset may be confused by this. Updates on save
            imports.Add(importEntry);
            EntryLookupTable[importEntry.InstancedFullPath] = importEntry; // ADD TO LOOKUP CACHE

            importEntry.EntryHasPendingChanges = true;
            ImportCount = imports.Count;

            updateTools(PackageChange.ImportAdd, importEntry.UIndex);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImportCount)));
        }

        /// <summary>
        /// Rebuilds the lookup table for this package. Call when there are name changes or the name of an entry is changed. May
        /// need to be optimized in a way so this is not called during things like porting so the list is not constantly rebuilt.
        /// </summary>
        public void RebuildLookupTable()
        {
            if (EntryLookupTable == null)
            {
                EntryLookupTable = new CaseInsensitiveDictionary<IEntry>(ImportCount + ExportCount);
            }
            else
            {
                EntryLookupTable.Clear();
            }
            foreach (var exportEntry in exports)
            {
                EntryLookupTable[exportEntry.InstancedFullPath] = exportEntry; // ADD TO LOOKUP CACHE
            }
            foreach (var importEntry in imports)
            {
                EntryLookupTable[importEntry.InstancedFullPath] = importEntry; // ADD TO LOOKUP CACHE
            }
        }

        public ImportEntry GetImport(int uIndex) => imports[Math.Abs(uIndex) - 1];
        public bool TryGetImport(int uIndex, out ImportEntry import)
        {
            if (IsImport(uIndex))
            {
                import = GetImport(uIndex);
                return true;
            }

            import = null;
            return false;
        }

        #endregion

        #region IEntry
        /// <summary>
        ///     gets Export or Import name
        /// </summary>
        /// <param name="uIndex">unreal index</param>
        public string getObjectName(int uIndex)
        {
            if (IsEntry(uIndex))
                return GetEntry(uIndex).ObjectName;
            if (uIndex == 0)
                return "Class";
            return "";
        }

        /// <summary>
        ///     gets Export or Import entry
        /// </summary>
        /// <param name="uindex">unreal index</param>
        public IEntry GetEntry(int uindex)
        {
            if (IsUExport(uindex))
                return exports[uindex - 1];
            if (IsImport(uindex))
                return imports[-uindex - 1];
            return null;
        }
        public bool IsEntry(int uindex) => IsUExport(uindex) || IsImport(uindex);
        public bool TryGetEntry(int uIndex, out IEntry entry)
        {
            if (IsEntry(uIndex))
            {
                entry = GetEntry(uIndex);
                return true;
            }

            entry = null;
            return false;
        }

        public void RemoveTrailingTrash()
        {
            ExportEntry trashPackage = FindExport(TrashPackageName);
            if (trashPackage == null)
            {
                return;
            }
            int trashPackageUIndex = trashPackage.UIndex;
            //make sure the first trashed export is the trashpackage
            foreach (ExportEntry exp in exports)
            {
                if (exp == trashPackage)
                {
                    //trashpackage is the first trashed export, so we're good
                    break;
                }
                if (exp.idxLink == trashPackageUIndex)
                {
                    //turn this into trashpackage, turn old trashpackage into regular Trash, and point all trash entries to the new trashpackage
                    exp.ObjectName = TrashPackageName;
                    exp.idxLink = 0;
                    exp.PackageGUID = TrashPackageGuid;

                    trashPackage.ObjectName = "Trash";
                    trashPackage.idxLink = exp.UIndex;
                    trashPackage.PackageGUID = Guid.Empty;

                    foreach (IEntry entry in trashPackage.GetChildren())
                    {
                        entry.idxLink = exp.UIndex;
                    }

                    trashPackage = exp;
                    trashPackageUIndex = trashPackage.UIndex;
                    EntryLookupTable[TrashPackageName] = trashPackage;
                    break;
                }
            }


            //remove imports
            for (int i = ImportCount - 1; i >= 0; i--)
            {
                ImportEntry lastImport = imports[i];
                if (lastImport.idxLink != trashPackageUIndex)
                {
                    //non-trash import, so stop removing
                    break;
                }

                lastImport.PropertyChanged -= importChanged;
                imports.RemoveAt(i);
                updateTools(PackageChange.ImportRemove, lastImport.UIndex);
                IsModified = true;
            }
            if (ImportCount != imports.Count)
            {
                ImportCount = imports.Count;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImportCount)));
            }

            //remove exports
            for (int i = ExportCount - 1; i >= 0; i--)
            {
                ExportEntry lastExport = exports[i];
                if (lastExport.idxLink != trashPackageUIndex)
                {
                    //non-trash export, so stop removing
                    break;
                }

                lastExport.PropertyChanged -= importChanged;
                exports.RemoveAt(i);
                updateTools(PackageChange.ExportRemove, lastExport.UIndex);
                IsModified = true;
            }
            if (ExportCount != exports.Count)
            {
                ExportCount = exports.Count;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExportCount)));
            }
            //if there are no more trashed imports or exports, and if the TrashPackage is the last export, remove it
            List<IEntry> trashChildren = null;
            if (exports.LastOrDefault() is ExportEntry finalExport && finalExport == trashPackage)
            {
                trashChildren = trashPackage.GetChildren();
                if (trashChildren.IsEmpty())
                {
                    trashPackage.PropertyChanged -= importChanged;
                    exports.Remove(trashPackage);
                    updateTools(PackageChange.ExportRemove, trashPackage.UIndex);
                    IsModified = true;
                    EntryLookupTable.Remove(TrashPackageName); // Remove the lookup for the trash package
                }
            }

            if (ExportCount != exports.Count)
            {
                // Remove subtrash object if none in lookup table. Otherwise update the pointer.
                if (trashChildren != null && trashChildren.Any())
                {
                    EntryLookupTable[$"{TrashPackageName}.Trash"] = trashChildren[0];
                }
                else
                {
                    EntryLookupTable.Remove($"{TrashPackageName}.Trash");
                }

                ExportCount = exports.Count;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExportCount)));
            }
        }

        #endregion

        private DateTime? lastSaved;
        public DateTime LastSaved
        {
            get
            {
                if (lastSaved.HasValue)
                {
                    return lastSaved.Value;
                }

                if (File.Exists(FilePath))
                {
                    return (new FileInfo(FilePath)).LastWriteTime;
                }

                return DateTime.MinValue;
            }
        }

        public long FileSize => File.Exists(FilePath) ? (new FileInfo(FilePath)).Length : 0;

        protected virtual void AfterSave()
        {
            //We do if checks here to prevent firing tons of extra events as we can't prevent firing change notifications if 
            //it's not really a change due to the side effects of suppressing that.
            foreach (var export in exports)
            {
                if (export.DataChanged)
                {
                    export.DataChanged = false;
                }
                if (export.HeaderChanged)
                {
                    export.HeaderChanged = false;
                }
                if (export.EntryHasPendingChanges)
                {
                    export.EntryHasPendingChanges = false;
                }
            }
            foreach (var import in imports)
            {
                if (import.HeaderChanged)
                {
                    import.HeaderChanged = false;
                }
                if (import.EntryHasPendingChanges)
                {
                    import.EntryHasPendingChanges = false;
                }
            }
            namesAdded = 0;

            lastSaved = DateTime.Now;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastSaved)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileSize)));
            IsModified = false;
        }

        #region packageHandler stuff
        public ObservableCollection<IPackageUser> Users { get; } = new ObservableCollection<IPackageUser>();
        public List<IPackageUser> WeakUsers { get; } = new List<IPackageUser>();

        public void RegisterTool(IPackageUser user)
        {
            // DEBUGGING MEMORY LEAK CODE
            //Debug.WriteLine($"{FilePath} RefCount incrementing from {RefCount} to {RefCount + 1} due to RegisterTool()");
            RefCount++;
            Users.Add(user);
            user.RegisterClosed(() =>
            {
                ReleaseUser(user);
                Dispose();
            });
        }

        public void Release(IPackageUser user = null)
        {
            if (user != null)
            {
                user = Users.FirstOrDefault(x => x == user);
                if (user != null)
                {
                    ReleaseUser(user);
                }
                else
                {
                    Debug.WriteLine("Releasing package that isn't in use by any user");
                }
            }
            Dispose();
        }

        private void ReleaseUser(IPackageUser user)
        {
            Users.Remove(user);
            if (Users.Count == 0)
            {
                noLongerOpenInTools?.Invoke(this);
            }
            user.ReleaseUse();
        }

        public delegate void MEPackageEventHandler(UnrealPackageFile sender);
        public event MEPackageEventHandler noLongerOpenInTools;

        protected void exportChanged(object sender, PropertyChangedEventArgs e)
        {
            // If we are never using the global cache there is no point
            // to notifying other things because nothing will share the 
            // package file
            if (sender is ExportEntry exp)
            {
                switch (e.PropertyName)
                {
                    case nameof(ExportEntry.DataChanged):
                        updateTools(PackageChange.ExportData, exp.UIndex);
                        break;
                    case nameof(ExportEntry.HeaderChanged):
                        updateTools(PackageChange.ExportHeader, exp.UIndex);
                        break;
                }
            }
        }

        protected void importChanged(object sender, PropertyChangedEventArgs e)
        {
            if (MEPackageHandler.GlobalSharedCacheEnabled && sender is ImportEntry imp && e.PropertyName == nameof(ImportEntry.HeaderChanged))
            {
                updateTools(PackageChange.ImportHeader, imp.UIndex);
            }
        }

        private readonly object _updatelock = new object();
        readonly HashSet<PackageUpdate> pendingUpdates = new HashSet<PackageUpdate>();
        readonly List<Task> tasks = new List<Task>();
        readonly Dictionary<int, bool> taskCompletion = new Dictionary<int, bool>();
        const int queuingDelay = 50;
        protected void updateTools(PackageChange change, int index)
        {
            if (Users.Count == 0 && WeakUsers.Count == 0)
            {
                return;
            }
            PackageUpdate update = new PackageUpdate(change, index);
            bool isNewUpdate;
            lock (_updatelock)
            {
                isNewUpdate = !pendingUpdates.Contains(update);
            }
            if (isNewUpdate)
            {
                lock (_updatelock)
                {
                    pendingUpdates.Add(update);
                }
                Task task = Task.Delay(queuingDelay);
                taskCompletion[task.Id] = false;
                tasks.Add(task);

                task.ContinueWithOnUIThread(x =>
                {
                    taskCompletion[x.Id] = true;
                    if (tasks.TrueForAll(t => taskCompletion[t.Id]))
                    {
                        tasks.Clear();
                        taskCompletion.Clear();
                        List<PackageUpdate> updates;
                        lock (_updatelock)
                        {
                            updates = pendingUpdates.ToList();
                            pendingUpdates.Clear();
                        }
                        var removedImports = updates.Where(u => u.Change == PackageChange.ImportRemove).Select(u => u.Index).ToList();
                        var removedExports = updates.Where(u => u.Change == PackageChange.ExportRemove).Select(u => u.Index).ToList();
                        var pendingUpdatesList = new List<PackageUpdate>();
                        //remove add/change updates for entries that have been removed
                        foreach (PackageUpdate upd in updates)
                        {
                            switch (upd.Change)
                            {
                                case PackageChange.ExportAdd:
                                case PackageChange.ExportData:
                                case PackageChange.ExportHeader:
                                    {
                                        if (!removedExports.Contains(upd.Index))
                                        {
                                            pendingUpdatesList.Add(upd);
                                        }
                                        break;
                                    }
                                case PackageChange.ImportAdd:
                                case PackageChange.ImportHeader:
                                    {
                                        if (!removedImports.Contains(upd.Index))
                                        {
                                            pendingUpdatesList.Add(upd);
                                        }
                                        break;
                                    }
                                default:
                                    pendingUpdatesList.Add(upd);
                                    break;
                            }
                        }
                        foreach (var item in Users.Concat(WeakUsers))
                        {
                            item.handleUpdate(pendingUpdatesList);
                        }
                    }
                });
            }
        }

        public event MEPackageEventHandler noLongerUsed;
        /// <summary>
        /// Amount of known tracked references to this object that were acquired through OpenMEPackage(). Manual references are not tracked
        /// </summary>
        public int RefCount { get; private set; }

        public void RegisterUse()
        {
            // DEBUGGING MEMORY LEAK CODE
            //Debug.WriteLine($"{FilePath} RefCount incrementing from {RefCount} to {RefCount + 1}");
            RefCount++;
        }

        /// <summary>
        /// Doesn't neccesarily dispose the object.
        /// Will only do so once this has been called by every place that uses it.
        /// HIGHLY Recommend using the using block instead of calling this directly.
        /// </summary>
        public void Dispose()
        {
            // DEBUGGING MEMORY LEAK CODE
            //Debug.WriteLine($"{FilePath} RefCount decrementing from {RefCount} to {RefCount - 1}");

            RefCount--;

            if (RefCount == 0)
            {
                noLongerUsed?.Invoke(this);
            }
        }
        #endregion

        public const string TrashPackageName = "ME3ExplorerTrashPackage";
        public static Guid TrashPackageGuid = "ME3ExpTrashPackage".ToGuid(); //DO NOT EDIT!!

        protected UnrealPackageFile(string filePath)
        {
            FilePath = filePath;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
