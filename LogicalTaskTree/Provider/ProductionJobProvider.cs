using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NetEti.ApplicationControl;
using NetEti.CustomControls;
using NetEti.FileTools;
using NetEti.FileTools.Zip;
using NetEti.ObjectSerializer;
using Vishnu.Interchange;

namespace LogicalTaskTree.Provider
{
    /// <summary>
    ///   Sucht, lädt und liefert Jobs für JobList-Knoten im LogicalTaskTree.
    /// </summary>
    /// <remarks>
    ///   File: ProductionJobProvider.cs
    ///   Autor: Erik Nagel
    ///   12.10.2013 Erik Nagel: erstellt
    ///   27.10.2017 Erik Nagel: Bessere Auflösung gezippter Jobs mit SubJobs.
    ///   01.02.2024 Erik Nagel: AppSettings.JobDirPathes um "Documentation" und "Tests" erweitert.
    ///   22.02.2024 Erik Nagel: GetJobElementResolvedPath, isPathServerReachable und canPing
    ///              hier rausgeschmissen und in überarbeiteter Form in NetworkMappingsRefresher realisiert.
    ///   20.04.2025 Erik Nagel: Komplett überarbeitet; Verarbeitung von Json-JobDescriptions hinzugefügt.
    /// </remarks>
    public class ProductionJobProvider : JobProviderBase
    {
        #region protected members

        /// <summary>
        /// Fügt dem Dictionary LoadedJobPackages das JobPackage
        /// mit dem logischen Pfad logicalJobName hinzu.
        /// Im Fehlerfall wird einfach nichts hinzugefügt.
        /// </summary>
        /// <param name="physicalJobPath">Der physikalische Pfad zur JobDescription oder zum Job-Verzeichnis.</param>
        /// <param name="logicalJobName">Der logische Name des Jobs oder null beim Root-Job.</param>
        /// <returns>Neu gesetzter, angepasster oder übergebener logicalJobName.</returns>
        protected override string TryLoadJobPackage(string physicalJobPath, string? logicalJobName = null)
        {
            try
            {
                int depth = 0;
                logicalJobName = LoadJob(null, physicalJobPath, logicalJobName,
                  null, false, null, null, true, false, out depth);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(String.Format($"Ladefehler auf {physicalJobPath} ({Directory.GetCurrentDirectory()}" +
                $" {(System.Reflection.Assembly.GetExecutingAssembly().Location)})\r\n{ex.Message}"));
            }
            return logicalJobName;
        }

        #endregion protected members

        #region private members

        #region Job

        //===================================================================================================
        // Lädt einen Job oder SubJob; SubJobs können intern oder extern (eigenes JobDescription-File)
        // angelegt sein. SubJobs werdenrekursiv wie Jobs geladen. Unterstützte Formate sind XML und JSON.
        //===================================================================================================
        private string LoadJob(Job? mother, string physicalJobPath, string? logicalJobName,
            JobContainer? subJobContainer, bool startCollapsed, TriggerShell? snapshotTrigger,
            string? snapshotTriggerName, bool isRootJob, bool isSnapshot, out int subJobDepth)
        {
            //===============================================================================================
            ShowJobLoadingInfo(physicalJobPath, logicalJobName);
            //===============================================================================================

            bool isDefaultSnapshot = false;
            string? loadedLogicalJobName = logicalJobName; // Vorbelegung
            bool popDirs = false;
            JobContainer? jobContainer = null;
            if (!String.IsNullOrEmpty(physicalJobPath)) // Bei MainJob oder extern definiertem
            {                                           // SubJob XML oder JSON laden.
                //===========================================================================================
                loadedLogicalJobName = FindAndLoadJobDescription(physicalJobPath, isRootJob,
                    isSnapshot, ref jobContainer, ref isDefaultSnapshot);
                //===========================================================================================
                if (string.IsNullOrEmpty(logicalJobName))
                {
                    logicalJobName = loadedLogicalJobName;
                    ConfigurationManager.LoadLocalConfiguration(logicalJobName);
                }
                if (!isSnapshot)
                {
                    popDirs = true;
                }
            }
            else
            {
                // inline definierter SubJob
                jobContainer = subJobContainer;
                if (jobContainer != null && mother != null)
                {
                    jobContainer.Format = mother.Format;
                }
            }
            if (String.IsNullOrEmpty(logicalJobName))
            {
                throw new ArgumentException("'LogicalJobName' ist nicht gesetzt(Job).");
            }
            if (jobContainer == null)
            {
                throw new ApplicationException("jobContainer is null.");
            }

            JobPackage jobPackage = new JobPackage(physicalJobPath, logicalJobName);
            jobPackage.Job.IsDefaultSnapshot = isDefaultSnapshot;
            string logicalExpression = jobContainer.LogicalExpression ?? "";
            jobPackage.Job.LogicalExpression = logicalExpression;

            //===============================================================================================
            LoadUserControlPathes(mother, jobContainer, jobPackage);
            //===============================================================================================
            LoadJobProperties(physicalJobPath, logicalJobName, jobContainer, jobPackage);
            //===============================================================================================
            LoadJobJriggers(logicalJobName, snapshotTrigger, isRootJob, loadedLogicalJobName,
                jobContainer, jobPackage);
            //===============================================================================================
            LoadJobLogger(logicalJobName, loadedLogicalJobName, jobContainer, jobPackage);
            //===============================================================================================
            LoadCheckers(physicalJobPath, logicalJobName, loadedLogicalJobName, jobContainer,
                jobPackage);
            //===============================================================================================
            LoadValueModifiers(physicalJobPath, logicalJobName, jobContainer, jobPackage);
            //===============================================================================================
            LoadTriggers(logicalJobName, loadedLogicalJobName, jobContainer, jobPackage);
            //===============================================================================================
            LoadLoggers(logicalJobName, loadedLogicalJobName, jobContainer, jobPackage);
            //===============================================================================================
            LoadWorkers(logicalJobName, loadedLogicalJobName, jobContainer, jobPackage);
            //===============================================================================================
            subJobDepth = LoadSubJobs(jobContainer, jobPackage);
            //===============================================================================================
            LoadSnapshots(physicalJobPath, logicalJobName, loadedLogicalJobName,
                jobContainer, jobPackage);
            //===============================================================================================

            try
            {
                LoadedJobPackages.Add(jobPackage.JobName, jobPackage);
            }
            catch
            {
                throw new ApplicationException(
                    String.Format("Job {0}: Ein Job kann nur einmal hinzugefügt werden."
                    + "\nHinweis: eine mehrfache Referenzierung in einem logischen Ausdruck ist jedoch möglich.",
                    jobPackage.JobName));
            }
            if (popDirs)
            {
                _appSettings.JobDirPathes.Pop();
            }
            return logicalJobName;
        }

        #region ShowJobLoadingInfo

        //===================================================================================================
        // published den greade zu ladenden Job für den SplashScreen.
        //===================================================================================================
        private static void ShowJobLoadingInfo(string physicalJobPath, string? logicalJobName)
        {
            string? splashMessageJobName = logicalJobName;
            if (String.IsNullOrEmpty(splashMessageJobName))
            {
                if (Directory.Exists(physicalJobPath))
                {
                    splashMessageJobName = Path.GetFileNameWithoutExtension(physicalJobPath);
                }
                else
                {
                    if (Path.GetExtension(physicalJobPath).ToLower() == ".zip")
                    {
                        splashMessageJobName = Path.GetFileNameWithoutExtension(physicalJobPath);
                    }
                    else
                    {
                        splashMessageJobName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(physicalJobPath));
                    }
                }
            }
            InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("lade " + splashMessageJobName ?? "---"));
        }

        #endregion ShowJobLoadingInfo

        #region FindAndLoadJobDescription

        //===================================================================================================
        // Sucht und lädt einen externen Job/SubJob aus einem XML oder Json oder einem gepackten
        // XML oder Json (ZIP) und vermerkt dabei die relevanten Pfadinformationen in globalen Properties.
        //===================================================================================================
        private string FindAndLoadJobDescription(string physicalJobPath, bool isRootJob, bool isSnapshot,
                                      ref JobContainer? jobContainer, ref bool isDefaultSnapshot)
        {
            if (!isSnapshot)
            {
                string resolvedJobDir;
                physicalJobPath = this._appSettings.PrepareAndLocatePath(physicalJobPath);
                resolvedJobDir = Path.GetDirectoryName(physicalJobPath) ?? "";
                if (isRootJob)
                {
                    this._appSettings.AppEnvAccessor.RegisterKeyValue(
                        "JobDirectory", Path.GetFullPath(resolvedJobDir));
                }
                _appSettings.JobDirPathes.Push(resolvedJobDir);
                if (!_appSettings.AssemblyDirectories.Contains(Path.Combine(resolvedJobDir, "Plugin")))
                {
                    _appSettings.AssemblyDirectories.Insert(0, Path.Combine(resolvedJobDir, "Plugin"));
                }
                if (!_appSettings.AssemblyDirectories.Contains(resolvedJobDir))
                {
                    _appSettings.AssemblyDirectories.Insert(1, resolvedJobDir);
                }
            }
            else
            {
                string tmpPath = this._appSettings.PrepareAndLocatePath(
                    physicalJobPath, new string[] { }, false);
                string tmpDir = Path.GetDirectoryName(tmpPath) ?? "";
                if (!File.Exists(tmpPath) && !Directory.Exists(tmpDir))
                {
                    if (Directory.Exists(Path.GetDirectoryName(tmpDir)))
                    {
                        Directory.CreateDirectory(tmpDir);
                    }
                    else
                    {
                        throw new FileNotFoundException(String.Format(
                            "Weder der SnapShot, noch sein Verzeichnis wurden gefunden: {0}.",
                            physicalJobPath));
                    }
                }
                else
                {
                    if (File.Exists(tmpPath))
                    {
                        // Im .info File steht der Pfad zum Job
                        physicalJobPath = File.ReadAllText(tmpPath);
                    }
                }
            }
            this._appSettings.SaveJobPathes();
            //===================================================================================================
            jobContainer = this.LoadJobDescriptionFile(physicalJobPath, isSnapshot);
            //===================================================================================================
            if (jobContainer == null)
            {
                if (!isSnapshot)
                {
                    throw new FileLoadException(physicalJobPath);
                }
                else
                {
                    isDefaultSnapshot = true;
                    jobContainer =
                        this.LoadJobDescriptionFile(this._appSettings.PrepareAndLocatePath(
                            @"LoadExceptionJob.xml"), isSnapshot);
                }
            }
            string logicalName = jobContainer?.LogicalName ?? "_unknown_";
            return logicalName;
        }

        //===================================================================================================
        // Lädt einen externen Job/SubJob aus einem XML oder Json und vermerkt dabei die
        // relevanten Pfadinformationen in globalen Properties.
        //===================================================================================================
        private JobContainer? LoadJobDescriptionFile(
            string physicalJobPath, bool isSnapshot = false)
        {
            JobContainer? jobContainer = null;
            string? jobDirectory = null;
            if (File.Exists(physicalJobPath))
            {
                jobDirectory = Path.GetDirectoryName(physicalJobPath) ?? "";
            }
            else
            {
                if (Directory.Exists(physicalJobPath))
                {
                    jobDirectory = physicalJobPath;
                }
            }
            if (Directory.Exists(jobDirectory))
            {
                IVishnuJobProvider? userJobProvider = null;
                // Suche nach einem User-JobProvider, bekannte dlls ausschließen.
                foreach (string dll in Directory.GetFiles(jobDirectory, "*.dll"))
                {
                    if (!(new List<string>() { "7z64.dll", "7z32.dll" }).Contains(Path.GetFileName(dll)))
                    {
                        userJobProvider = null;
                        try
                        {
                            userJobProvider = (IVishnuJobProvider?)
                                                VishnuAssemblyLoader.GetAssemblyLoader().DynamicLoadObjectOfTypeFromAssembly(
                                                    dll, typeof(IVishnuJobProvider));
                            if (userJobProvider != null)
                            {
                                return userJobProvider.GetVishnuJobDescription(physicalJobPath);
                            }
                        }
                        catch { }
                    }
                }
            }
            if (File.Exists(physicalJobPath))
            {
                if (physicalJobPath.ToLower().EndsWith(".xml"))
                {
                    if (!isSnapshot)
                    {
                        jobContainer =
                            SerializationUtility.LoadFromXmlFile<JobContainer>(physicalJobPath);
                    }
                    else
                    {
                        SnapshotJobContainer? snapshotJobContainer
                            = SerializationUtility.LoadFromXmlFile<SnapshotJobContainer>(physicalJobPath);
                        jobContainer = snapshotJobContainer?.JobDescription;
                        if (jobContainer != null)
                        {
                            jobContainer.Path = snapshotJobContainer?.Path;
                            jobContainer.TimeStamp = snapshotJobContainer?.TimeStamp;
                        }
                    }
                    if (jobContainer != null)
                    {
                        jobContainer.Format = JobDescriptionFormat.Xml;
                    }
                }
                else
                {
                    if (physicalJobPath.ToLower().EndsWith(".json"))
                    {
                        string jsonString = File.ReadAllText(physicalJobPath);
                        // Parse das JSON-Dokument
                        using JsonDocument document = JsonDocument.Parse(jsonString);
                        if (!isSnapshot)
                        {
                            // Greife auf das "JobDescription"-Element zu
                            JsonElement rootElement = document.RootElement.GetProperty("JobDescription");

                            // Deserialisiere das "JobDescription"-Element in die JobContainer-Klasse
                            jobContainer =
                                JsonSerializer.Deserialize<JobContainer>(rootElement.GetRawText());
                        }
                        else
                        {
                            // Greife auf das "Snapshot"-Element zu
                            JsonElement rootElement = document.RootElement.GetProperty("Snapshot");

                            // Deserialisiere das "Snapshot"-Element in die SnapshotJobContainer-Klasse
                            SnapshotJobContainer? snapshotJobContainer =
                                JsonSerializer.Deserialize<SnapshotJobContainer>(rootElement.GetRawText());

                            jobContainer = snapshotJobContainer?.JobDescription;
                            if (jobContainer != null)
                            {
                                jobContainer.Path = snapshotJobContainer?.Path;
                                jobContainer.TimeStamp = snapshotJobContainer?.TimeStamp;
                            }
                        }
                        if (jobContainer != null)
                        {
                            jobContainer.Format = JobDescriptionFormat.Json;
                        }
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Unbekanntes JobDescription-Format: {physicalJobPath}");
                    }
                }
            }
            return jobContainer;
        }

        #endregion FindAndLoadJobDescription

        #region LoadUserControlPathes

        //===================================================================================================
        // UserControlPathes: lädt optional im Job definierte Pfade zu User-spezifischen Controls zur
        // Darstellung im Vishnu-Tree. Berücksichtigt dabei Hierarchien von bei Jobs, SubJobs, Checkern
        // und verschiedene Typen von User-Controls.
        //===================================================================================================
        private void LoadUserControlPathes(Job? mother, JobContainer jobContainer, JobPackage jobPackage)
        {
            // Die UserControl-Pfade sind alle mit Default ungleich null vor-initialisiert
            // (JobPackage.Job-Konstruktor), deshalb kann hier keine vereinfachende
            // null-propagation kodiert werden, da sonst die Vorbelegungen ggf. mit null
            // überschrieben würden.
            // Die Variante: "jobPackage.Job.JobListUserControlPath
            //                = tmpVar ??= jobPackage.Job.JobListUserControlPath;"
            // würde zwar funktionieren, hätte aber im Fall "tmpVar == null" eine unnötige
            // Zuweisung "jobPackage.Job.JobListUserControlPath = jobPackage.Job.JobListUserControlPath;"
            // zur Folge.

            string? tmpString = jobContainer.JobListUserControlPath ?? mother?.JobListUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.JobListUserControlPath = tmpString;
            }
            tmpString = jobContainer.JobListUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.JobListUserControlPath = tmpString;
            }
            tmpString = jobContainer.JobListUserControlPath ?? mother?.JobConnectorUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.JobConnectorUserControlPath = tmpString;
            }
            tmpString = jobContainer.JobListUserControlPath ?? mother?.NodeListUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.NodeListUserControlPath = tmpString;
            }
            tmpString = jobContainer.JobListUserControlPath ?? mother?.SingleNodeUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.SingleNodeUserControlPath = tmpString;
            }
            tmpString = jobContainer.JobListUserControlPath ?? mother?.ConstantNodeUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.ConstantNodeUserControlPath = tmpString;
            }
            tmpString = jobContainer.JobListUserControlPath ?? mother?.SnapshotUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.SnapshotUserControlPath = tmpString;
            }
        }

        #endregion LoadUserControlPathes

        #region LoadJobProperties

        //===================================================================================================
        // Job-Properties: lädt alle einfachen Properties des Jobs.
        //===================================================================================================
        private static void LoadJobProperties(string physicalJobPath, string logicalJobName,
            JobContainer jobContainer, JobPackage jobPackage)
        {
            try
            {
                jobPackage.Job.Format = jobContainer.Format;
                jobPackage.Job.BreakWithResult
                    = (jobContainer.JobListUserControlPath) != null
                        && Convert.ToBoolean(jobContainer.BreakWithResult);

                jobPackage.Job.ThreadLocked = false;
                JobContainerThreadLock? threadLock = jobContainer.StructuredThreadLock;
                if (threadLock != null)
                {
                    jobPackage.Job.ThreadLocked = threadLock.ThreadLocked;
                    jobPackage.Job.LockName = threadLock.LockName;
                }
                jobPackage.Job.StartCollapsed = jobContainer.StartCollapsed;
                jobPackage.Job.InitNodes = jobContainer.InitNodes;
                jobPackage.Job.TriggeredRunDelay = jobContainer.TriggeredRunDelay;
                jobPackage.Job.IsVolatile = jobContainer.IsVolatile;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(String.Format("Fehler in {0}({1})\n{2}",
                    logicalJobName, physicalJobPath, ex.Message), ex);
            }
        }

        #endregion LoadJobProperties

        #region LoadJobJriggers

        //===================================================================================================
        // JobSnapshotTrigger, JobTrigger: lädt einen optionalen Trigger zur Erzeugung von Snapshots
        //                                 und einen optionalen Trigger zum Neustart für den Job.
        //===================================================================================================
        private void LoadJobJriggers(string logicalJobName, TriggerShell? snapshotTrigger,
            bool isRootJob, string? loadedLogicalJobName, JobContainer jobContainer, JobPackage jobPackage)
        {
            //===============================================================================================
            LoadJobSnapshotTrigger(logicalJobName, isRootJob, loadedLogicalJobName, jobContainer, jobPackage);
            //===============================================================================================
            LoadJobTrigger(logicalJobName, snapshotTrigger, loadedLogicalJobName, jobContainer, jobPackage);
            //===============================================================================================
        }

        //===================================================================================================
        // JobTrigger: lädt einen optionalen Trigger zum Neustart für den Job.
        //===================================================================================================
        private void LoadJobTrigger(string logicalJobName, TriggerShell? snapshotTrigger,
            string? loadedLogicalJobName, JobContainer jobContainer, JobPackage jobPackage)
        {
            if (snapshotTrigger != null) // Wenn dieser Job ein SubSnapshot in einem übergeordneten Job ist,
            {
                // wird von dort der JobTrigger für diesen (Snapshot-)Job übergeben.
                jobPackage.Job.JobTrigger = snapshotTrigger;
            }
            JobContainerTrigger? jobTrigger = jobContainer.JobTrigger;
            if (jobTrigger != null)
            {
                string? tmpString = jobTrigger.Parameters;
                string? paraString = tmpString
                    == null ? null : this._appSettings.ReplaceWildcardsAndPathes(
                        tmpString, _appSettings.AssemblyDirectories.ToArray());
                tmpString = jobTrigger.Reference;
                if (tmpString != null)
                {
                    if (paraString == loadedLogicalJobName)
                    {
                        paraString = logicalJobName; // Referenz ggf. umbiegen
                    }
                    jobPackage.Job.JobTrigger = new TriggerShell(tmpString, paraString, false);
                }
                else
                {
                    tmpString = jobTrigger.PhysicalPath;
                    if (tmpString != null)
                    {
                        jobPackage.Job.JobTrigger
                            = new TriggerShell(this._appSettings.ReplaceWildcardsAndPathes(
                                tmpString, _appSettings.AssemblyDirectories.ToArray()), paraString);
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Wenn 'Reference' nicht gesetzt ist, muss 'PhysicalPath' gesetzt werden (JobTrigger).");
                    }
                }
            }
        }

        //===================================================================================================
        // JobSnapshotTrigger: lädt einen optionalen Trigger zur Erzeugung von Snapshots für den Job.
        //===================================================================================================
        private void LoadJobSnapshotTrigger(string logicalJobName, bool isRootJob,
            string? loadedLogicalJobName, JobContainer jobContainer, JobPackage jobPackage)
        {
            // Trigger, der steuert, wann ein Snapshot des aktuellen Jobs erzeugt werden soll
            // (geht nur bei isRootJob == true).
            if (isRootJob)
            {
                string snapshotDirectory = _appSettings.ReplaceWildcards(_appSettings.SnapshotDirectory);
                if (!Directory.Exists(_appSettings.SnapshotDirectory))
                {
                    Directory.CreateDirectory(_appSettings.SnapshotDirectory);
                }
                JobContainerTrigger? jobSnapshotTrigger = jobContainer.JobSnapshotTrigger;
                if (jobSnapshotTrigger != null)
                {
                    string? tmpString = jobSnapshotTrigger.Parameters;
                    string? paraString = tmpString
                        == null ? null : this._appSettings.ReplaceWildcardsAndPathes(
                            tmpString, _appSettings.AssemblyDirectories.ToArray());
                    tmpString = jobSnapshotTrigger.Reference;
                    if (tmpString != null)
                    {
                        if (paraString == loadedLogicalJobName)
                        {
                            paraString = logicalJobName; // Referenz ggf. umbiegen
                        }
                        if (paraString != null)
                        {
                            jobPackage.Job.JobSnapshotTrigger
                                = new TriggerShell(tmpString, paraString, false);
                        }
                        else
                        {
                            throw new ArgumentException(
                                "Wenn 'Reference' gesetzt ist, muss auch 'Parameters' gesetzt werden (JobSnapshotTrigger).");
                        }
                    }
                    else
                    {
                        tmpString = jobSnapshotTrigger.PhysicalPath;
                        if (tmpString != null)
                        {
                            jobPackage.Job.JobSnapshotTrigger
                                = new TriggerShell(this._appSettings.ReplaceWildcardsAndPathes(
                                    tmpString, _appSettings.AssemblyDirectories.ToArray()), paraString);
                        }
                        else
                        {
                            throw new ArgumentException(
                                "Wenn 'Reference' nicht gesetzt ist, muss 'PhysicalPath' gesetzt werden (JobSnapshotTrigger).");
                        }
                    }
                }
            }
        }

        #endregion LoadJobJriggers

        #region LoadJobLogger

        //===================================================================================================
        // JobLogger: lädt einen optionalen Logger für den Job selbst.
        //===================================================================================================
        private void LoadJobLogger(string logicalJobName, string? loadedLogicalJobName,
            JobContainer jobContainer, JobPackage jobPackage)
        {
            JobContainerLogger? jobLogger = jobContainer.JobLogger;
            if (jobLogger != null)
            {
                string? tmpString = jobLogger.Parameters;
                string? paraString = tmpString
                    == null ? null : this._appSettings.ReplaceWildcardsAndPathes(
                        tmpString, _appSettings.AssemblyDirectories.ToArray());
                if (paraString == loadedLogicalJobName)
                {
                    paraString = logicalJobName; // Referenz ggf. umbiegen
                }
                tmpString = jobLogger.Reference;
                if (tmpString != null)
                {
                    jobPackage.Job.JobLogger = new LoggerShell(tmpString);
                }
                else
                {
                    tmpString = jobLogger.PhysicalPath;
                    if (tmpString != null && paraString != null)
                    {
                        jobPackage.Job.JobLogger
                            = new LoggerShell(this._appSettings.ReplaceWildcardsAndPathes(
                                tmpString, _appSettings.AssemblyDirectories.ToArray()), paraString);
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Wenn 'Reference' nicht gesetzt ist, müssen 'PhysicalPath' und 'Parameters' gesetzt werden (JobLogger).");
                    }
                }
            }
        }

        #endregion LoadJobLogger

        #endregion Job

        #region LoadCheckers

        //===================================================================================================
        // Checkers: lädt alle Checker, die dem aktuellen Job zugeordnet sind.
        //===================================================================================================
        private void LoadCheckers(string physicalJobPath, string logicalJobName,
            string? loadedLogicalJobName, JobContainer jobContainer, JobPackage jobPackage)
        {
            string? tmpString;
            string? paraString;
            foreach (JobContainerChecker jobChecker in jobContainer.Checkers.CheckersList)
            {
                TriggerShell? trigger = null;
                JobContainerTrigger? checkerTrigger = jobChecker.Trigger;
                if (checkerTrigger != null)
                {
                    tmpString = checkerTrigger.Parameters;
                    paraString = tmpString
                        == null ? null : this._appSettings.ReplaceWildcardsAndPathes(
                            tmpString, _appSettings.AssemblyDirectories.ToArray());
                    tmpString = checkerTrigger.Reference;
                    if (tmpString != null)
                    {
                        if (paraString != null && paraString == loadedLogicalJobName)
                        {
                            paraString = logicalJobName; // Referenz ggf. umbiegen
                        }
                        trigger = new TriggerShell(tmpString, paraString, false);
                    }
                    else
                    {
                        tmpString = checkerTrigger.PhysicalPath;
                        if (tmpString != null)
                        {
                            trigger = new TriggerShell(this._appSettings.ReplaceWildcardsAndPathes(
                                tmpString, _appSettings.AssemblyDirectories.ToArray()), paraString);
                        }
                        else
                        {
                            throw new ArgumentException(
                                "Wenn 'Reference' nicht gesetzt ist, muss 'PhysicalPath' gesetzt werden (CheckerTrigger).");
                        }
                    }
                }
                LoggerShell? logger = null;
                JobContainerLogger? checkerLogger = jobChecker.Logger;
                if (checkerLogger != null)
                {
                    tmpString = checkerLogger.Reference;
                    if (tmpString != null)
                    {
                        logger = new LoggerShell(tmpString);
                    }
                    else
                    {
                        tmpString = checkerLogger.Parameters;
                        paraString = tmpString
                            == null ? null : this._appSettings.ReplaceWildcardsAndPathes(
                                tmpString, _appSettings.AssemblyDirectories.ToArray());
                        if (paraString == loadedLogicalJobName)
                        {
                            paraString = logicalJobName; // Referenz ggf. umbiegen
                        }
                        tmpString = checkerLogger.PhysicalPath;
                        if (paraString != null && tmpString != null)
                        {
                            logger = new LoggerShell(
                                this._appSettings.ReplaceWildcardsAndPathes(
                                    tmpString, _appSettings.AssemblyDirectories.ToArray()), paraString);
                        }
                        else
                        {
                            throw new ArgumentException(
                                "Wenn 'Reference' nicht gesetzt ist, müssen 'PhysicalPath' und 'Parameters' gesetzt werden (CheckerLogger).");
                        }
                    }
                }
                bool isMirror = false;
                tmpString = jobChecker.Parameters;
                paraString = tmpString
                    == null ? null : this._appSettings.ReplaceWildcardsAndPathes(
                        tmpString, _appSettings.AssemblyDirectories.ToArray());
                isMirror = paraString != null && paraString.ToUpper().Equals("ISMIRROR");
                string? physicalPath = jobChecker.PhysicalPath;
                if (physicalPath == null)
                {
                    throw new ArgumentException("Ein Checker muss einen PhysicalPath zugeordnet bekommen.");
                }
                string physicalDllPath
                  = this._appSettings.ReplaceWildcardsAndPathes(
                      physicalPath, _appSettings.AssemblyDirectories.ToArray());
                isMirror |= Path.GetFileNameWithoutExtension(physicalDllPath).ToUpper().Equals("TRIGGEREVENTMIRRORCHECKER");
                bool alwaysReloadChecker = _appSettings.UncachedCheckers.Contains(
                    Path.GetFileNameWithoutExtension(physicalDllPath));

                string? singleNodeUserControlPath = jobChecker.SingleNodeUserControlPath;
                tmpString = jobChecker.UserControlPath;
                singleNodeUserControlPath = tmpString == null ? singleNodeUserControlPath : tmpString;

                bool threadLocked = false;
                string? lockName = null;
                try
                {
                    JobContainerThreadLock? threadLock = jobChecker.StructuredThreadLock;
                    if (threadLock != null)
                    {
                        threadLocked = threadLock.ThreadLocked;
                        lockName = threadLock.LockName;
                    }
                    bool initNodes = jobChecker.InitNodes;
                    int triggeredRunDelay = jobChecker.TriggeredRunDelay;
                    bool isGlobal = jobChecker.IsGlobal;
                    CheckerShell checkerShell = new CheckerShell(
                        physicalDllPath, paraString, paraString, trigger, logger, alwaysReloadChecker);
                    if (threadLocked)
                    {
                        checkerShell.ThreadLocked = true;
                        checkerShell.LockName = lockName;
                    }
                    checkerShell.UserControlPath
                        = String.IsNullOrEmpty(singleNodeUserControlPath) ? jobPackage.Job.SingleNodeUserControlPath : singleNodeUserControlPath;
                    checkerShell.IsMirror = isMirror;
                    checkerShell.InitNodes = initNodes;
                    checkerShell.IsGlobal = isGlobal;
                    checkerShell.TriggeredRunDelay = triggeredRunDelay;
                    checkerShell.CanRunDllPath = jobChecker.CanRunDllPath;
                    if (checkerShell.CheckerParameters != null)
                    {
                        checkerShell.CheckerParameters = this._appSettings.ReplaceWildcardsAndPathes(
                            checkerShell.CheckerParameters, _appSettings.AssemblyDirectories.ToArray());
                    }
                    tmpString = jobChecker.LogicalName;
                    if (String.IsNullOrEmpty(tmpString))
                    {
                        throw new ArgumentException("'LogicalName' muss gesetzt werden(Checker).");
                    }
                    jobPackage.Job.Checkers.Add(tmpString, checkerShell);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(String.Format("Fehler in {0}({1})\n{2}", logicalJobName, physicalJobPath, ex.Message), ex);
                }
            }
        }

        #endregion LoadCheckers

        #region LoadValueModifiers

        //===================================================================================================
        // ValueModifiers: lädt alle ValueModifier, die dem aktuellen Job zugeordnet sind.
        //===================================================================================================
        private void LoadValueModifiers(string physicalJobPath, string logicalJobName,
            JobContainer jobContainer, JobPackage jobPackage)
        {
            foreach (JobContainerValueModifier jobValueModifier
                in jobContainer.ValueModifiers.ValueModifiersList)
            {
                string? tmpString = jobValueModifier.PhysicalPath;
                string? jobValueModifierSlave
                    = tmpString == null ? null : this._appSettings.ReplaceWildcardsAndPathes(
                    tmpString, _appSettings.AssemblyDirectories.ToArray());
                string? jobValueModifierFormat = jobValueModifier.Format;
                string singleNodeUserControlPath = jobValueModifier.SingleNodeUserControlPath == null ?
                    jobPackage.Job.SingleNodeUserControlPath : jobValueModifier.SingleNodeUserControlPath;
                singleNodeUserControlPath = jobValueModifier.UserControlPath == null ?
                    singleNodeUserControlPath : jobValueModifier.UserControlPath;
                string? logicalName = jobValueModifier.LogicalName;
                if (logicalName != null)
                {
                    try
                    {
                        bool isGlobal = jobValueModifier.IsGlobal;
                        tmpString = jobValueModifier.Reference;
                        if (String.IsNullOrEmpty(tmpString))
                        {
                            throw new ArgumentException("Referenz nicht gefunden."
                                + " Ein ValueModifier braucht zwingend eine gültige 'Reference'.");
                        }
                        string reference = tmpString;
                        switch ((jobValueModifier.Type ?? "").ToUpper())
                        {
                            case "STRING":
                                jobPackage.Job.Checkers.Add(logicalName,
                                  createTypedValueModifier<String>(reference,
                                    jobValueModifierSlave,
                                    jobValueModifierFormat,
                                    singleNodeUserControlPath,
                                    isGlobal));
                                break;
                            case "BOOL":
                            case "BOOLEAN":
                                jobPackage.Job.Checkers.Add(logicalName,
                                  createTypedValueModifier<Boolean>(reference,
                                    jobValueModifierSlave,
                                    jobValueModifierFormat,
                                    singleNodeUserControlPath,
                                    isGlobal));
                                break;
                            case "INT":
                            case "INTEGER":
                                jobPackage.Job.Checkers.Add(logicalName,
                                  createTypedValueModifier<int>(reference,
                                    jobValueModifierSlave,
                                    jobValueModifierFormat,
                                    singleNodeUserControlPath,
                                    isGlobal));
                                break;
                            case "DATETIME":
                                jobPackage.Job.Checkers.Add(logicalName,
                                  createTypedValueModifier<DateTime>(reference,
                                    jobValueModifierSlave,
                                    jobValueModifierFormat,
                                    singleNodeUserControlPath,
                                    isGlobal));
                                break;
                            default:
                                jobPackage.Job.Checkers.Add(logicalName,
                                  createTypedValueModifier<String>(reference,
                                    jobValueModifierSlave,
                                    jobValueModifierFormat,
                                    singleNodeUserControlPath,
                                    isGlobal));
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(String.Format("Fehler in {0}({1})\n{2}",
                            logicalJobName, physicalJobPath, ex.Message), ex);
                    }
                }
            }
        }

        private ValueModifier<T> createTypedValueModifier<T>(
            string referenceName, string? slavePath, string? format, string userControlPath, bool isGlobal)
        {
            if (slavePath != null)
            {
                ValueModifier<T> typedValueModifier = new ValueModifier<T>(slavePath, referenceName);
                typedValueModifier.UserControlPath = userControlPath;
                typedValueModifier.IsGlobal = isGlobal;
                return typedValueModifier;
            }
            else
            {
                ValueModifier<T> typedValueModifier = new ValueModifier<T>(format, referenceName);
                typedValueModifier.UserControlPath = userControlPath;
                typedValueModifier.IsGlobal = isGlobal;
                return typedValueModifier;
            }
        }

        #endregion LoadValueModifiers

        #region LoadTriggers

        //===================================================================================================
        // Triggers: lädt alle Trigger, die dem aktuellen Job zugeordnet sind.
        //===================================================================================================
        private void LoadTriggers(string logicalJobName, string? loadedLogicalJobName,
            JobContainer jobContainer, JobPackage jobPackage)
        {
            foreach (JobContainerTrigger singleJobTrigger in jobContainer.Triggers.TriggersList)
            {
                TriggerShell trigger;
                string? paraString = null;
                string? tmpString = singleJobTrigger.Parameters;
                if (tmpString != null)
                {
                    paraString = this._appSettings.ReplaceWildcardsAndPathes(
                        tmpString, _appSettings.AssemblyDirectories.ToArray());
                }
                tmpString = singleJobTrigger.Reference;
                if (tmpString != null)
                {
                    if (paraString != null && paraString == loadedLogicalJobName)
                    {
                        paraString = logicalJobName; // Referenz ggf. umbiegen
                    }
                    trigger = new TriggerShell(tmpString, paraString, false);
                    string? name = singleJobTrigger.LogicalName;
                    if (name != null)
                    {
                        jobPackage.Job.Triggers.Add(name, trigger);
                    }
                }
                else
                {
                    string? tmpPhysicalPath = singleJobTrigger.PhysicalPath;
                    string? tmpLogicalName = singleJobTrigger.LogicalName;
                    if (tmpPhysicalPath == null || tmpLogicalName == null)
                    {
                        throw new ArgumentException(
                            "'Reference' und/oder 'PhysicalPath' nicht gefunden. Ein Trigger ohne"
                            + " 'Reference' muss einen 'PhysicalPath' und einen 'LogicalName' haben?");
                    }
                    trigger = new TriggerShell(this._appSettings.ReplaceWildcardsAndPathes(
                        tmpPhysicalPath, _appSettings.AssemblyDirectories.ToArray()), paraString);
                    jobPackage.Job.Triggers.Add(tmpLogicalName, trigger);
                }
            }
        }

        #endregion LoadTriggers

        #region LoadLoggers

        //===================================================================================================
        // Loggers: lädt alle Logger, die dem aktuellen Job zugeordnet sind.
        //===================================================================================================
        private void LoadLoggers(string logicalJobName, string? loadedLogicalJobName,
            JobContainer jobContainer, JobPackage jobPackage)
        {
            foreach (JobContainerLogger singleJobLogger in jobContainer.Loggers.LoggersList)
            {
                string? tmpString = singleJobLogger.Parameters;
                string? paraString = tmpString
                    == null ? null : this._appSettings.ReplaceWildcardsAndPathes(
                        tmpString, _appSettings.AssemblyDirectories.ToArray());
                if (paraString == loadedLogicalJobName)
                {
                    paraString = logicalJobName; // Referenz ggf. umbiegen
                }
                tmpString = singleJobLogger.PhysicalPath;
                if (tmpString != null && paraString != null)
                {
                    LoggerShell logger = new LoggerShell(this._appSettings.ReplaceWildcardsAndPathes(
                        tmpString, _appSettings.AssemblyDirectories.ToArray()), paraString);
                    tmpString = singleJobLogger.LogicalName;
                    if (String.IsNullOrEmpty(tmpString))
                    {
                        throw new ArgumentException(
                            "'LogicalName' ist nicht gesetzt (Logger).");
                    }
                    jobPackage.Job.Loggers.Add(tmpString, logger);
                }
                else
                {
                    throw new ArgumentException(
                        "'PhysicalPath' und/oder 'Parameters' sind nicht gesetzt (Logger).");
                }
            }
        }

        #endregion LoadLoggers

        #region LoadWorkers

        //===================================================================================================
        // Workers: lädt alle Worker, die im aktuellen Job definiert sind.
        //===================================================================================================
        private void LoadWorkers(string logicalJobName, string? loadedLogicalJobName,
            JobContainer jobContainer, JobPackage jobPackage)
        {
            foreach (JobContainerWorker jobWorker in jobContainer.Workers.WorkersList)
            {
                string? tmpString = jobWorker.LogicalExpression;
                if (tmpString == null)
                {
                    throw new ArgumentException("'LogicalExpression' ist nicht gesetzt (Worker).");
                }
                string nodeId_Events = tmpString;
                string nodeId = (nodeId_Events + ":").Split(new char[] { ':' })[0].Trim();
                if (nodeId != null && nodeId == loadedLogicalJobName)
                {
                    nodeId_Events = nodeId_Events.Replace(nodeId, logicalJobName);
                    nodeId = logicalJobName; // Referenz ggf. umbiegen
                }
                string events = (nodeId_Events + ":").Split(new char[] { ':' })[1].Trim();
                if (!String.IsNullOrEmpty(events))
                {
                    List<WorkerShell> subWorkerList = new List<WorkerShell>();
                    foreach (JobContainerSubWorker subWorker in jobWorker.SubWorkers.SubWorkersList)
                    {
                        TriggerShell? trigger = null;
                        JobContainerTrigger? workerTrigger = subWorker.Trigger;
                        if (workerTrigger != null)
                        {
                            string? paraString = workerTrigger.Parameters;
                            tmpString = workerTrigger.Reference;
                            if (tmpString != null)
                            {
                                if (paraString != null && paraString == loadedLogicalJobName)
                                {
                                    paraString = logicalJobName; // Referenz ggf. umbiegen
                                }
                                trigger = new TriggerShell(tmpString, paraString, false);
                            }
                            else
                            {
                                tmpString = workerTrigger.PhysicalPath;
                                if (tmpString != null)
                                {
                                    trigger = new TriggerShell(this._appSettings.ReplaceWildcardsAndPathes(
                                        tmpString, _appSettings.AssemblyDirectories.ToArray()), paraString);
                                }
                                else
                                {
                                    throw new ArgumentException(
                                        "'PhysicalPath' ist nicht gesetzt (Trigger).");
                                }
                            }
                        }
                        string? subWorkerPara = subWorker.Parameters;
                        if (subWorkerPara != null)
                        {
                            bool transportByFile = false;
                            subWorker.StructuredParameters =
                                SerializationUtility.DeserializeFromJsonOrXml<StructuredParameters>(subWorkerPara, noException: true);
                            if (subWorker.StructuredParameters != null)
                            {
                                transportByFile = 
                                    subWorker.StructuredParameters.Transport?.ToLower().Equals("file") ?? false;
                                foreach (JobContainerSubWorker subSubWorker
                                    in subWorker.StructuredParameters.SubWorkers.SubWorkersList)
                                {
                                    tmpString = subSubWorker.PhysicalPath;
                                    if (tmpString != null)
                                    {
                                        string newPhysicalPath = this._appSettings.ReplaceWildcardsAndPathes(
                                            tmpString, _appSettings.AssemblyDirectories.ToArray());
                                        subSubWorker.PhysicalPath = newPhysicalPath;
                                    }
                                }
                            }
                            if (!transportByFile)
                            {
                                subWorkerPara = Regex.Replace(subWorkerPara,
                                    @"\<\/{0,1}parameters\.*?\>", "", RegexOptions.IgnoreCase);
                                if (subWorkerPara.StartsWith(@"""") && subWorkerPara.EndsWith(@""""))
                                {
                                    subWorkerPara = Regex.Replace(subWorkerPara, @"^""|""$", "");
                                }
                                subWorkerPara = Regex.Unescape(subWorkerPara);
                            }
                            tmpString = subWorker.PhysicalPath;
                            if (tmpString != null)
                            {
                                subWorkerList.Add(new WorkerShell(this._appSettings.ReplaceWildcardsAndPathes(
                                    tmpString, _appSettings.AssemblyDirectories.ToArray()),
                                  subWorkerPara, transportByFile, trigger));
                            }
                        }
                    }
                    if (subWorkerList.Count > 0)
                    {
                        WorkerShell[] subWorkerArray = new WorkerShell[subWorkerList.Count];
                        subWorkerList.CopyTo(subWorkerArray);
                        jobPackage.Job.Workers.Add(nodeId_Events, subWorkerArray);
                    }
                }
            }
        }

        #endregion LoadWorkers

        #region LoadSubJobs

        //===================================================================================================
        // SubJobs: rekursiv - lädt alle Snapshots, die im aktuellen Job definiert sind.
        //===================================================================================================
        private int LoadSubJobs(JobContainer jobContainer, JobPackage jobPackage)
        {
            int subJobDepth;
            jobPackage.Job.MaxSubJobDepth = 0;
            int hasSubJobs = 0;
            bool startSubJobCollapsed;
            foreach (JobContainer subJob in jobContainer.SubJobs.SubJobsList)
            {
                hasSubJobs = 1;
                startSubJobCollapsed = subJob.StartCollapsed;
                string physicalPath = "";
                string? tmpString = subJob.PhysicalPath;
                string? logicalName = subJob.LogicalName;
                if (tmpString != null)
                {
                    physicalPath = this._appSettings.ReplaceWildcardsAndPathes(
                        tmpString, _appSettings.JobDirPathes.ToArray());
                }
                else
                {
                    if (subJob?.LogicalExpression == null)
                    {
                        if (logicalName != null)
                        {
                            physicalPath = this._appSettings.ReplaceWildcardsAndPathes(
                                logicalName, _appSettings.JobDirPathes.ToArray());
                        }
                        else
                        {
                            throw new ArgumentException(
                                "Ladefehler: SubJob ohne externen Pfad und ohne LogicalExpression.");
                        }
                    }
                }
                int depth = 0;
                try
                {
                    //====================== Rekursion ======================================================
                    LoadJob(jobPackage.Job, physicalPath, logicalName, subJob,
                      startSubJobCollapsed, null, null, false, false, out depth);
                    //====================== Rekursion ======================================================
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(
                        String.Format($"Ladefehler auf {physicalPath}\r\n{ex.Message}"));
                }
                if (depth > jobPackage.Job.MaxSubJobDepth)
                {
                    jobPackage.Job.MaxSubJobDepth = depth;
                }
            }
            jobPackage.Job.MaxSubJobDepth += hasSubJobs;
            subJobDepth = jobPackage.Job.MaxSubJobDepth;
            int calcDepth = jobPackage.Job.MaxSubJobDepth;
            jobPackage.Job.LogicalChangedDelay =
                (int)(_appSettings.LogicalChangedDelay
                * Math.Pow(_appSettings.LogicalChangedDelayPower, calcDepth));
            return subJobDepth;
        }

        #endregion LoadSubJobs

        #region LoadSnapshots

        //===================================================================================================
        // Snapshots: rekursiv - lädt alle Snapshots, die im aktuellen Job definiert sind.
        //===================================================================================================
        private void LoadSnapshots(string physicalJobPath, string logicalJobName,
            string? loadedLogicalJobName, JobContainer jobContainer, JobPackage jobPackage)
        {
            foreach (JobContainer snapshot in jobContainer.Snapshots.SnapshotsList)
            {
                bool startSnapshotCollapsed;
                string physicalSnapshotName;
                string physicalSnapshotPath;
                TriggerShell? trigger = null;
                string logicalTriggerName;
                string logicalSnapshotName;
                try
                {
                    string? tmpString = snapshot.LogicalName;
                    logicalSnapshotName = tmpString ?? throw new ArgumentException(
                        "'LogicalName' ist nicht angegeben (Snapshot).");
                    jobPackage.Job.SnapshotNames.Add(logicalSnapshotName);
                    physicalSnapshotName = logicalSnapshotName.TrimStart('#');
                    startSnapshotCollapsed = snapshot.StartCollapsed;
                    if (snapshot.PhysicalPath != null)
                    {
                        physicalSnapshotPath = snapshot.PhysicalPath;
                    }
                    else
                    {
                        physicalSnapshotPath = physicalSnapshotName;
                    }
                    physicalSnapshotPath =
                        _appSettings.ReplaceWildcards(physicalSnapshotPath);
                    if (physicalSnapshotPath.ToLower().EndsWith("jobsnapshot.xml")
                        || physicalSnapshotPath.ToLower().EndsWith("jobsnapshot.info"))
                    {
                        physicalSnapshotPath = Path.GetDirectoryName(physicalSnapshotPath) ??
                            Directory.GetCurrentDirectory();
                    }
                    if (!(physicalSnapshotPath.Contains(":")
                          || physicalSnapshotPath.StartsWith(@"/")
                          || physicalSnapshotPath.StartsWith(@"\")))
                    {
                        physicalSnapshotPath =
                            Path.Combine(_appSettings.SnapshotDirectory, physicalSnapshotPath);
                    }
                    physicalSnapshotPath = Path.Combine(physicalSnapshotPath, "JobSnapshot.info");
                    logicalTriggerName = logicalSnapshotName + "Trigger";
                    JobContainerTrigger? jobContainerTrigger = snapshot.Trigger;
                    if (jobContainerTrigger != null)
                    {
                        string? paraString = null;
                        tmpString = jobContainerTrigger.Parameters;
                        if (tmpString != null)
                        {
                            paraString = this._appSettings.ReplaceWildcardsAndPathes(
                                tmpString.Replace("JobSnapshot.xml", "JobSnapshot.info"), new string[] { });
                        }
                        else
                        {
                            paraString = physicalSnapshotPath + "|InitialFire";
                        }
                        if (paraString.ToUpper().Contains("%SNAPSHOTDIRECTORIES%"))
                        {
                            paraString = Regex.Replace(paraString, "%SNAPSHOTDIRECTORIES%",
                                String.Join(",", Path.GetDirectoryName(physicalSnapshotPath)),
                                RegexOptions.IgnoreCase);
                        }
                        tmpString = jobContainerTrigger.Reference;
                        if (tmpString != null)
                        {
                            if (paraString != null && paraString == loadedLogicalJobName)
                            {
                                paraString = logicalJobName; // Referenz ggf. umbiegen
                            }
                            trigger = new TriggerShell(tmpString, paraString, false);
                        }
                        else
                        {
                            tmpString = jobContainerTrigger.PhysicalPath
                                ?? throw new ArgumentException(
                                    "'Reference' und/oder 'PhysicalPath' nicht gefunden." +
                                    " Ein Trigger ohne 'Reference' muss einen 'PhysicalPath' haben?");
                            trigger = new TriggerShell(this._appSettings.ReplaceWildcardsAndPathes(
                                tmpString, _appSettings.AssemblyDirectories.ToArray()), paraString);
                        }
                        tmpString = jobContainerTrigger.LogicalName;
                        if (tmpString != null)
                        {
                            logicalTriggerName = tmpString;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(String.Format("Fehler in {0}({1})\n{2}",
                        logicalJobName, physicalJobPath, ex.Message), ex);
                }
                int depth = 0;
                try
                {
                    string logicalName = String.Empty;
                    //====================== Rekursion ======================================================
                    LoadJob(jobPackage.Job, physicalSnapshotPath, logicalSnapshotName, snapshot,
                      startSnapshotCollapsed, trigger, logicalTriggerName, false, true, out depth);
                    //====================== Rekursion ======================================================
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(
                        String.Format($"Ladefehler auf {physicalSnapshotPath}\r\n{ex.Message}"));
                }
            }
        }

        #endregion LoadSnapshots

        #endregion private members
    }
}
