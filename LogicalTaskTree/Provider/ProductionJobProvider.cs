using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NetEti.ApplicationControl;
using NetEti.CustomControls;
using NetEti.FileTools.Zip;
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
    /// </remarks>
    public class ProductionJobProvider : JobProviderBase
    {
        #region protected members
        /// <summary>
        ///   Fügt dem Dictionary LoadedJobPackages das JobPackage
        ///   mit dem logischen Pfad logicalJobName hinzu.
        ///   Im Fehlerfall wird einfach nichts hinzugefügt.
        /// </summary>
        /// <param name="logicalJobName">Der logische Name des Jobs oder null beim Root-Job.</param>
        protected override void TryLoadJobPackage(ref string logicalJobName)
        {
            if (String.IsNullOrEmpty(logicalJobName))
            {
                this._appSettings.JobDirPathes = new Stack<string>();
                this._appSettings.JobDirPathes.Push("");
                this._appSettings.JobDirPathes.Push(this._appSettings.ApplicationRootPath);
                //this._appSettings.JobDirPathes.Push(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                string rootJobPackageTmp = this._appSettings.RootJobPackagePath ?? this._appSettings.ApplicationRootPath;
                if (this._appSettings.RootJobXmlName.ToLower() != "jobdescription.xml")
                {
                    rootJobPackageTmp = Path.Combine(rootJobPackageTmp, Path.GetFileName(this._appSettings.RootJobXmlName));
                }
                string jobXml = replaceWildcardsNPathes(rootJobPackageTmp, this._appSettings.JobDirPathes.ToArray());
                if (!jobXml.ToLower().EndsWith(".xml"))
                {
                    jobXml = Path.Combine(jobXml, "JobDescription.xml");
                }
                try
                {
                    int depth = 0;
                    logicalJobName = loadJob(null, jobXml, logicalJobName,
                      null, false, null, null, true, false, out depth);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(String.Format($"Ladefehler auf {jobXml} ({Directory.GetCurrentDirectory()}" +
                        $" {(System.Reflection.Assembly.GetExecutingAssembly().Location)})\r\n{ex.Message}"));
                }
            }
        }

        #endregion protected members

        #region private members

        private string? _resolvedJobDir;

        private void createAndRememberZipRelativeDummyDirectory(string zipPath)
        {
            string fallbackDirectory = Directory.GetCurrentDirectory();
            string randomDirectoryName = Path.Combine(Path.GetDirectoryName(zipPath) ?? fallbackDirectory,
                Path.GetRandomFileName() + ".v");
            Directory.CreateDirectory(randomDirectoryName);
            this._appSettings.ZipRelativeDummyDirectory = randomDirectoryName;
            if (!_appSettings.JobDirPathes.Contains(randomDirectoryName))
            {
                _appSettings.JobDirPathes.Push(randomDirectoryName);
            }
        }

        // Wenn der Job ein ZIP-Archiv ist, wird der Job ins WorkingDirectory
        // entpackt und ein angepasster Pfad zum Job zurückgeliefert.
        private string unpackIfPacked(string jobPath, bool isRootJob)
        {
            Stack<string> searchedPathes = new Stack<string>();
            string jobDir = Path.GetDirectoryName(jobPath) ?? "";
            string jobFile = Path.GetFileName(jobPath);
            searchedPathes.Push(jobPath);
            ZipAccess zipAccess = new ZipAccess();
            try
            {
                if (File.Exists(jobPath))
                {
                    if (zipAccess.IsZip(jobPath))
                    {
                        if (isRootJob) this.createAndRememberZipRelativeDummyDirectory(jobPath);
                        jobDir = unpack(jobPath, zipAccess);
                        jobFile = "JobDescription.xml";
                    }
                }
                else
                {
                    if (zipAccess.IsZip(jobDir))
                    {
                        if (isRootJob) this.createAndRememberZipRelativeDummyDirectory(jobDir);
                        jobDir = unpack(jobDir, zipAccess);
                    }
                    else
                    {
                        if (Directory.Exists(jobDir))
                        {
                            if (Directory.Exists(jobPath))
                            {
                                jobDir = jobPath;
                                jobFile = "JobDescription.xml";
                            }
                        }
                        else
                        {
                            searchedPathes.Push(jobPath + ".zip");
                            if (zipAccess.IsZip(jobPath + ".zip"))
                            {
                                if (isRootJob) this.createAndRememberZipRelativeDummyDirectory(jobPath);
                                jobDir = unpack(jobPath + ".zip", zipAccess);
                            }
                            else
                            {
                                searchedPathes.Push(jobDir + ".zip");
                                if (zipAccess.IsZip(jobDir + ".zip"))
                                {
                                    if (isRootJob) this.createAndRememberZipRelativeDummyDirectory(jobDir);
                                    jobDir = unpack(jobDir + ".zip", zipAccess);
                                }
                                else
                                {
                                    string searchedPathesString = "";
                                    while (searchedPathes.Count > 0)
                                    {
                                        searchedPathesString += Environment.NewLine + searchedPathes.Pop();
                                    }
                                    throw new FileNotFoundException(String.Format("Der Job wurde nicht gefunden:{0}.", searchedPathesString));
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                zipAccess.Dispose();
            }
            return Path.Combine(jobDir, jobFile);
        }

        // Entpackt einen Job aus einem ZIP-Archiv.
        private string unpack(string archive, ZipAccess zipAccess)
        {
            string rtn = Directory.GetCurrentDirectory();
            zipAccess.UnZipArchive(archive, _appSettings.WorkingDirectory, null, false);
            string archivePath = Path.Combine(Path.GetDirectoryName(archive) ?? "", Path.GetFileNameWithoutExtension(archive));
            if (!_appSettings.AssemblyDirectories.Contains(archivePath))
            {
                _appSettings.AssemblyDirectories.Add(archivePath);
            }
            if (!_appSettings.JobDirPathes.Contains(archivePath))
            {
                _appSettings.JobDirPathes.Push(archivePath);
            }
            rtn = Path.Combine(_appSettings.WorkingDirectory, Path.GetFileNameWithoutExtension(archive));
            return rtn;
        }

        // Lädt einen externen Job/SubJob aus einem XML oder einem gepackten XML (ZIP).
        private string loadJobXMLFile(string jobXML, bool isRootJob, bool isSnapshot,
                                      ref XDocument? xmlDoc, ref bool isDefaultSnapshot)
        {
            if (!isSnapshot)
            {
                string tmpPath = Path.GetFullPath(replaceWildcardsNPathes(jobXML, _appSettings.JobDirPathes.ToArray()));
                if (!String.IsNullOrEmpty(this._appSettings.ZipRelativeDummyDirectory)
                  && !Directory.Exists(tmpPath) && !File.Exists(tmpPath))
                {
                    tmpPath = Path.GetFullPath(replaceWildcardsNPathes(jobXML + ".zip", _appSettings.JobDirPathes.ToArray()));
                }
                if (!Directory.Exists(tmpPath) && !File.Exists(tmpPath))
                {
                    tmpPath = Path.GetFullPath(replaceWildcardsNPathes(jobXML + ".xml", _appSettings.JobDirPathes.ToArray()));
                }
                jobXML = unpackIfPacked(tmpPath, isRootJob);
                _resolvedJobDir = Path.GetDirectoryName(replaceWildcardsNPathes(jobXML, _appSettings.JobDirPathes.ToArray())) ?? "";
                _appSettings.JobDirPathes.Push(_resolvedJobDir);
                if (!_appSettings.AssemblyDirectories.Contains(Path.Combine(_resolvedJobDir, "Plugin")))
                {
                    _appSettings.AssemblyDirectories.Insert(0, Path.Combine(_resolvedJobDir, "Plugin"));
                }
                if (!_appSettings.AssemblyDirectories.Contains(_resolvedJobDir))
                {
                    _appSettings.AssemblyDirectories.Insert(1, _resolvedJobDir);
                }
            }
            else
            {
                string tmpPath = Path.GetFullPath(replaceWildcardsNPathes(jobXML, new string[] { }));
                string tmpDir = Path.GetDirectoryName(tmpPath) ?? "";
                if (!File.Exists(tmpPath) && !Directory.Exists(tmpDir))
                {
                    if (Directory.Exists(Path.GetDirectoryName(tmpDir)))
                    {
                        Directory.CreateDirectory(tmpDir);
                    }
                    else
                    {
                        throw new FileNotFoundException(String.Format("Weder der SnapShot, noch sein Verzeichnis wurden gefunden: {0}.", jobXML));
                    }
                }
                else
                {
                    if (File.Exists(tmpPath))
                    {
                        jobXML = File.ReadAllText(tmpPath);
                    }
                    else
                    {
                        jobXML = Path.Combine(tmpDir, Path.GetFileName(tmpPath));
                    }
                    _resolvedJobDir = Path.GetDirectoryName(jobXML) ?? "";
                }
            }
            xmlDoc = getVishnuJobXML(jobXML);
            if (xmlDoc == null)
            {
                if (!isSnapshot)
                {
                    throw new FileLoadException(jobXML);
                }
                else
                {
                    isDefaultSnapshot = true;
                    xmlDoc = XDocument.Load(@"LoadExceptionJob.xml");
                }
            }
            string logicalName = (xmlDoc.Descendants("JobDescription").Elements("LogicalName").First().Value).ToString();
            return logicalName;
        }

        private XDocument? getVishnuJobXML(string jobXmlPath)
        {
            XDocument? xmlDoc = null;
            if (File.Exists(jobXmlPath))
            {
                xmlDoc = XDocument.Load(jobXmlPath);
            }
            else
            {
                string jobDir = Path.GetDirectoryName(jobXmlPath) ?? "";
                if (Directory.Exists(jobDir))
                {
                    foreach (string dll in Directory.GetFiles(jobDir, "*.dll"))
                    {
                        if (!(new List<string>() { "7z64.dll", "7z32.dll" }).Contains(Path.GetFileName(dll)))
                        {
                            IVishnuJobProvider? provider
                                = (IVishnuJobProvider?)VishnuAssemblyLoader.GetAssemblyLoader().DynamicLoadObjectOfTypeFromAssembly(
                                    dll, typeof(IVishnuJobProvider));
                            if (provider != null)
                            {
                                xmlDoc = provider.GetVishnuJobXml(jobDir);
                                break;
                            }
                        }
                    }
                }
            }
            return xmlDoc;
        }

        // Lädt einen Job oder SubJob; SubJobs können intern oder extern (eigenes XML-File) angelegt sein.
        private string loadJob(Job? mother, string jobXML, string? logicalJobName, XElement? xSubJob, bool startCollapsed,
                               TriggerShell? snapshotTrigger, string? snapshotTriggerName, bool isRootJob, bool isSnapshot, out int subJobDepth)
        {
            string? splashMessageJobName = logicalJobName;
            if (String.IsNullOrEmpty(splashMessageJobName))
            {
                if (Path.GetFileName(jobXML).ToLower() == "jobdescription.xml"
                    || Path.GetFileName(jobXML).ToLower() == "jobsnapshot.xml")
                {
                    splashMessageJobName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(jobXML));
                }
                else
                {
                    splashMessageJobName = Path.GetFileNameWithoutExtension(jobXML); // bei gezippten Jobs
                }
            }
            InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("lade " + splashMessageJobName ?? "---"));
            //====================== Job =============================================================================================
            bool isDefaultSnapshot = false;
            string? xmlLogicalJobName = logicalJobName; // Vorbelegung
            XElement? tmpSubJobRoot = null;
            if (xSubJob != null)
            {
                tmpSubJobRoot = xSubJob;
            }
            bool popDirs = false;
            if (!String.IsNullOrEmpty(jobXML)) // bei MainJob oder extern definiertem SubJob XML laden
            {
                XDocument? xmlDoc = new XDocument();
                xmlLogicalJobName = loadJobXMLFile(jobXML, isRootJob, isSnapshot, ref xmlDoc, ref isDefaultSnapshot);
                if (string.IsNullOrEmpty(logicalJobName))
                {
                    logicalJobName = xmlLogicalJobName;
                    ConfigurationManager.LoadLocalConfiguration(logicalJobName); // 14.02.2019 Nagel+-
                }
                tmpSubJobRoot = xmlDoc?.Descendants("JobDescription").First();
                if (!isSnapshot)
                {
                    popDirs = true;
                }
            }
            XElement subJobRoot = tmpSubJobRoot ?? throw new XmlException("Es konnte kein Job/Subjob gelesen werden.");
            if (String.IsNullOrEmpty(logicalJobName))
            {
                throw new ArgumentException("'LogicalJobName' ist nicht gesetzt(Job).");
            }

            JobPackage jobPackage = new JobPackage(jobXML, logicalJobName);
            jobPackage.Job.IsDefaultSnapshot = isDefaultSnapshot;
            string logicalExpression = subJobRoot.Elements("LogicalExpression").First().Value;
            jobPackage.Job.LogicalExpression = logicalExpression;

            string? tmpString;
            string? paraString;
            XElement? tmpXElement;

            //Die UserControl-Pfade sind alle mit Default ungleich null vor-initialisiert (JobPackage.Job-Konstruktor),
            //deshalb kann hier keine verinfachende null-propagation kodiert werden,
            //da sonst die Vorbelegungen ggf. mit null überschrieben würden.
            //Die Variante: "jobPackage.Job.JobListUserControlPath = tmpVar ??= jobPackage.Job.JobListUserControlPath;"
            //würde zwar funktionieren, hätte aber im Fall "tmpVar == null" eine unnötige Zuweisung
            //"jobPackage.Job.JobListUserControlPath = jobPackage.Job.JobListUserControlPath;" zur Folge.

            //XElement tmpVar = (subJobRoot.Elements("JobListUserControlPath").FirstOrDefault());
            //jobPackage.Job.JobListUserControlPath = tmpVar == null ? mother?.JobListUserControlPath : Convert.ToString(tmpVar.Value);
            //jobPackage.Job.JobListUserControlPath = tmpVar ??= jobPackage.Job.JobListUserControlPath;
            tmpString = subJobRoot.Elements("JobListUserControlPath").FirstOrDefault()?.Value ?? mother?.JobListUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.JobListUserControlPath = tmpString;
            }
            //tmpVar = (subJobRoot.Elements("UserControlPath").FirstOrDefault());
            //jobPackage.Job.JobListUserControlPath = tmpVar == null ? null : Convert.ToString(tmpVar.Value);
            //jobPackage.Job.JobListUserControlPath = tmpVar ??= jobPackage.Job.JobListUserControlPath;
            tmpString = subJobRoot.Elements("UserControlPath").FirstOrDefault()?.Value;
            if (tmpString != null)
            {
                jobPackage.Job.JobListUserControlPath = tmpString;
            }
            //tmpVar = (subJobRoot.Elements("JobConnectorUserControlPath").FirstOrDefault());
            //jobPackage.Job.JobConnectorUserControlPath = tmpVar == null ? mother?.JobConnectorUserControlPath : Convert.ToString(tmpVar.Value);
            tmpString = subJobRoot.Elements("JobConnectorUserControlPath").FirstOrDefault()?.Value ?? mother?.JobConnectorUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.JobConnectorUserControlPath = tmpString;
            }
            //tmpVar = (subJobRoot.Elements("NodeListUserControlPath").FirstOrDefault());
            //jobPackage.Job.NodeListUserControlPath = tmpVar == null ? mother?.NodeListUserControlPath : Convert.ToString(tmpVar.Value);
            tmpString = subJobRoot.Elements("NodeListUserControlPath").FirstOrDefault()?.Value ?? mother?.NodeListUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.NodeListUserControlPath = tmpString;
            }
            //tmpVar = (subJobRoot.Elements("SingleNodeUserControlPath").FirstOrDefault());
            //jobPackage.Job.SingleNodeUserControlPath = tmpVar == null ? mother?.SingleNodeUserControlPath : Convert.ToString(tmpVar.Value);
            tmpString = subJobRoot.Elements("SingleNodeUserControlPath").FirstOrDefault()?.Value ?? mother?.SingleNodeUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.SingleNodeUserControlPath = tmpString;
            }
            //tmpVar = (subJobRoot.Elements("ConstantNodeUserControlPath").FirstOrDefault());
            //jobPackage.Job.ConstantNodeUserControlPath = tmpVar == null ? mother?.ConstantNodeUserControlPath : Convert.ToString(tmpVar.Value);
            tmpString = subJobRoot.Elements("ConstantNodeUserControlPath").FirstOrDefault()?.Value ?? mother?.ConstantNodeUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.ConstantNodeUserControlPath = tmpString;
            }
            //tmpVar = (subJobRoot.Elements("SnapshotUserControlPath").FirstOrDefault());
            //jobPackage.Job.SnapshotUserControlPath = tmpVar == null ? mother?.SnapshotUserControlPath : Convert.ToString(tmpVar.Value);
            tmpString = subJobRoot.Elements("SnapshotUserControlPath").FirstOrDefault()?.Value ?? mother?.SnapshotUserControlPath;
            if (tmpString != null)
            {
                jobPackage.Job.SnapshotUserControlPath = tmpString;
            }
            try
            {
                //tmpVar = (subJobRoot.Elements("BreakWithResult").FirstOrDefault());
                //jobPackage.Job.BreakWithResult = tmpVar == null ? false : Convert.ToBoolean(tmpVar.Value);
                //
                //jobPackage.Job.BreakWithResult = false;
                //tmpString = subJobRoot.Elements("BreakWithResult").FirstOrDefault()?.Value;
                //if (tmpString != null)
                //{
                //    jobPackage.Job.BreakWithResult = Convert.ToBoolean(tmpString);
                //}
                jobPackage.Job.BreakWithResult
                    = (subJobRoot.Elements("BreakWithResult").FirstOrDefault()?.Value) != null
                        && Convert.ToBoolean(subJobRoot.Elements("BreakWithResult").FirstOrDefault()?.Value);

                jobPackage.Job.ThreadLocked = false;
                tmpXElement = subJobRoot.Elements("ThreadLocked").FirstOrDefault();
                if (tmpXElement != null)
                {
                    jobPackage.Job.ThreadLocked = Convert.ToBoolean(tmpXElement.Value);
                    XAttribute? tmpXAttribute = tmpXElement.Attributes("LockName").FirstOrDefault();
                    // DONE: austesten, ob bei der auskommentierten Variante unten auch eine Zuweisung erfolgt,
                    //       wenn tmpXAttribute?.Value == null ist.
                    //       24.03.2023 Erik Nagel: es wird null zugewiesen!
                    jobPackage.Job.LockName = tmpXAttribute?.Value;
                }

                jobPackage.Job.StartCollapsed
                    = (subJobRoot.Elements("StartCollapsed").FirstOrDefault()?.Value) != null
                        && Convert.ToBoolean(subJobRoot.Elements("StartCollapsed").FirstOrDefault()?.Value);
                if (startCollapsed) // Parameter
                {
                    jobPackage.Job.StartCollapsed = true;
                }

                //tmpvar = subJobRoot.Elements("InitNodes").FirstOrDefault();
                //jobPackage.Job.InitNodes = tmpVar == null ? false : Convert.ToBoolean(tmpVar.Value);
                jobPackage.Job.InitNodes
                    = (subJobRoot.Elements("InitNodes").FirstOrDefault()?.Value) != null
                        && Convert.ToBoolean(subJobRoot.Elements("InitNodes").FirstOrDefault()?.Value);

                tmpString = subJobRoot.Elements("TriggeredRunDelay").FirstOrDefault()?.Value;
                jobPackage.Job.TriggeredRunDelay = tmpString == null ? 0 : Convert.ToInt32(tmpString);

                //tmpString = subJobRoot.Elements("IsVolatile").FirstOrDefault()?.Value;
                //jobPackage.Job.IsVolatile = tmpString == null ? false : Convert.ToBoolean(tmpString);
                jobPackage.Job.IsVolatile
                    = (subJobRoot.Elements("IsVolatile").FirstOrDefault()?.Value) != null
                        && Convert.ToBoolean(subJobRoot.Elements("IsVolatile").FirstOrDefault()?.Value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(String.Format("Fehler in {0}({1})\n{2}", logicalJobName, jobXML, ex.Message), ex);
            }

            //====================== JobSnapshotTrigger ==============================================================================
            // Trigger, der steuert, wann ein Snapshot des aktuellen Jobs erzeugt werden soll (geht nur bei isRootJob == true).
            if (isRootJob)
            {
                string snapshotDirectory = _appSettings.ReplaceWildcards(_appSettings.ResolvedSnapshotDirectory);
                if (!Directory.Exists(_appSettings.ResolvedSnapshotDirectory))
                {
                    Directory.CreateDirectory(_appSettings.ResolvedSnapshotDirectory);
                }
                tmpXElement = (subJobRoot.Elements("JobSnapshotTrigger").FirstOrDefault());
                if (tmpXElement != null)
                {
                    //string? paraString = null;
                    //XElement? para = tmpXElement.Element("Parameters");
                    //if (para != null)
                    //{
                    //    paraString = replaceWildcardsNPathes(para.Value, _appSettings.AssemblyDirectories.ToArray());
                    //}

                    tmpString = tmpXElement.Element("Parameters")?.Value;
                    paraString = tmpString
                        == null ? null : replaceWildcardsNPathes(tmpString, _appSettings.AssemblyDirectories.ToArray());
                    tmpString = tmpXElement.Element("Reference")?.Value;
                    if (tmpString != null)
                    {
                        if (paraString == xmlLogicalJobName)
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
                        tmpString = tmpXElement.Element("PhysicalPath")?.Value;
                        if (tmpString != null)
                        {
                            jobPackage.Job.JobSnapshotTrigger
                                = new TriggerShell(replaceWildcardsNPathes(tmpString,
                              _appSettings.AssemblyDirectories.ToArray()), paraString);
                        }
                        else
                        {
                            throw new ArgumentException(
                                "Wenn 'Reference' nicht gesetzt ist, muss 'PhysicalPath' gesetzt werden (JobSnapshotTrigger).");
                        }
                    }
                }
            }

            //====================== JobTrigger ======================================================================================
            if (snapshotTrigger != null) // Wenn dieser Job ein SubSnapshot in einem übergeordneten Job ist,
            {
                // wird von dort der JobTrigger für diesen (Snapshot-)Job übergeben.
                jobPackage.Job.JobTrigger = snapshotTrigger;
            }
            tmpXElement = (subJobRoot.Elements("JobTrigger").FirstOrDefault());
            if (tmpXElement != null)
            {
                //string? paraString = null;
                //XElement? para = tmpXElement.Element("Parameters");
                //if (para != null)
                //{
                //    paraString = replaceWildcardsNPathes(para.Value, _appSettings.AssemblyDirectories.ToArray());
                //}

                //tmpString = (subJobRoot.Elements("JobTrigger").FirstOrDefault());
                //if (tmpString != null)
                //{
                //    string paraString = null;
                //    XElement para = tmpString.Element("Parameters");
                //    if (para != null)
                //    {
                //        paraString = replaceWildcardsNPathes(para.Value, _appSettings.AssemblyDirectories.ToArray());
                //    }
                //    if (tmpString.Element("Reference") != null)
                //    {
                //        if (paraString != null && paraString == xmlLogicalJobName)
                //        {
                //            paraString = logicalJobName; // Referenz ggf. umbiegen
                //        }
                //        jobPackage.Job.JobTrigger = new TriggerShell(tmpString.Element("Reference").Value, paraString, false);
                //    }
                //    else
                //    {
                //        jobPackage.Job.JobTrigger = new TriggerShell(replaceWildcardsNPathes(tmpString.Element("PhysicalPath").Value,
                //          _appSettings.AssemblyDirectories.ToArray()), paraString);
                //    }
                //}
                tmpString = tmpXElement.Element("Parameters")?.Value;
                paraString = tmpString
                    == null ? null : replaceWildcardsNPathes(tmpString, _appSettings.AssemblyDirectories.ToArray());
                tmpString = tmpXElement.Element("Reference")?.Value;
                if (tmpString != null)
                {
                    if (paraString == xmlLogicalJobName)
                    {
                        paraString = logicalJobName; // Referenz ggf. umbiegen
                    }
                    if (paraString != null)
                    {
                        jobPackage.Job.JobTrigger
                            = new TriggerShell(tmpString, paraString, false);
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Wenn 'Reference' gesetzt ist, muss auch 'Parameters' gesetzt werden (JobTrigger).");
                    }
                }
                else
                {
                    tmpString = tmpXElement.Element("PhysicalPath")?.Value;
                    if (tmpString != null)
                    {
                        jobPackage.Job.JobTrigger
                            = new TriggerShell(replaceWildcardsNPathes(tmpString,
                          _appSettings.AssemblyDirectories.ToArray()), paraString);
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Wenn 'Reference' nicht gesetzt ist, muss 'PhysicalPath' gesetzt werden (JobTrigger).");
                    }
                }
            }
            //====================== JobLogger =======================================================================================
            tmpXElement = (subJobRoot.Elements("JobLogger").FirstOrDefault());
            if (tmpXElement != null)
            {
                tmpString = tmpXElement.Element("Parameters")?.Value;
                paraString = tmpString
                    == null ? null : replaceWildcardsNPathes(tmpString, _appSettings.AssemblyDirectories.ToArray());
                if (paraString == xmlLogicalJobName)
                {
                    paraString = logicalJobName; // Referenz ggf. umbiegen
                }
                tmpString = tmpXElement.Element("Reference")?.Value;
                if (tmpString != null)
                {
                    jobPackage.Job.JobLogger = new LoggerShell(tmpString);
                }
                else
                {
                    tmpString = tmpXElement.Element("PhysicalPath")?.Value;
                    if (tmpString != null && paraString != null)
                    {
                        jobPackage.Job.JobLogger
                            = new LoggerShell(replaceWildcardsNPathes(tmpString, _appSettings.AssemblyDirectories.ToArray()),
                                paraString);
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Wenn 'Reference' nicht gesetzt ist, müssen 'PhysicalPath' und 'Parameters' gesetzt werden (JobLogger).");
                    }
                }
            }
            //====================== Checkers ========================================================================================
            IEnumerable<XElement> jobCheckers = from item in subJobRoot.Elements("Checkers").Elements() select item;
            foreach (XElement jobChecker in jobCheckers)
            {
                TriggerShell? trigger = null;
                tmpXElement = jobChecker.Element("Trigger");
                if (tmpXElement != null)
                {
                    tmpString = tmpXElement.Element("Parameters")?.Value;
                    paraString = tmpString
                        == null ? null : replaceWildcardsNPathes(tmpString, _appSettings.AssemblyDirectories.ToArray());
                    tmpString = tmpXElement.Element("Reference")?.Value;
                    if (tmpString != null)
                    {
                        if (paraString != null && paraString == xmlLogicalJobName)
                        {
                            paraString = logicalJobName; // Referenz ggf. umbiegen
                        }
                        trigger = new TriggerShell(tmpString, paraString, false);
                    }
                    else
                    {
                        tmpString = tmpXElement.Element("PhysicalPath")?.Value;
                        if (tmpString != null)
                        {
                            trigger = new TriggerShell(replaceWildcardsNPathes(tmpString,
                              _appSettings.AssemblyDirectories.ToArray()), paraString);
                        }
                        else
                        {
                            throw new ArgumentException(
                                "Wenn 'Reference' nicht gesetzt ist, muss 'PhysicalPath' gesetzt werden (CheckerTrigger).");
                        }
                    }
                }
                LoggerShell? logger = null;
                tmpXElement = jobChecker.Element("Logger");
                if (tmpXElement != null)
                {
                    tmpString = tmpXElement.Element("Reference")?.Value;
                    if (tmpString != null)
                    {
                        logger = new LoggerShell(tmpString);
                    }
                    else
                    {
                        tmpString = tmpXElement.Element("Parameters")?.Value;
                        paraString = tmpString
                            == null ? null : replaceWildcardsNPathes(tmpString, _appSettings.AssemblyDirectories.ToArray());
                        if (paraString == xmlLogicalJobName)
                        {
                            paraString = logicalJobName; // Referenz ggf. umbiegen
                        }
                        tmpString = tmpXElement.Element("PhysicalPath")?.Value;
                        if (paraString != null && tmpString != null)
                        {
                            logger = new LoggerShell(
                                replaceWildcardsNPathes(tmpString, _appSettings.AssemblyDirectories.ToArray()), paraString);
                        }
                        else
                        {
                            throw new ArgumentException(
                                "Wenn 'Reference' nicht gesetzt ist, müssen 'PhysicalPath' und 'Parameters' gesetzt werden (CheckerLogger).");
                        }
                    }
                }
                bool isMirror = false;
                tmpString = jobChecker.Element("Parameters")?.Value;
                paraString = tmpString
                    == null ? null : replaceWildcardsNPathes(tmpString, _appSettings.AssemblyDirectories.ToArray());
                isMirror = paraString != null && paraString.ToUpper().Equals("ISMIRROR");
                tmpXElement = jobChecker.Elements("PhysicalPath").FirstOrDefault();
                if (tmpXElement == null)
                {
                    throw new ArgumentException("Ein Checker muss einen PhysicalPath zugeordnet bekommen.");
                }
                string physicalDllPath
                  = replaceWildcardsNPathes(tmpXElement.Value, _appSettings.AssemblyDirectories.ToArray());
                isMirror |= Path.GetFileNameWithoutExtension(physicalDllPath).ToUpper().Equals("TRIGGEREVENTMIRRORCHECKER");
                bool alwaysReloadChecker = _appSettings.UncachedCheckers.Contains(
                    Path.GetFileNameWithoutExtension(physicalDllPath));

                tmpXElement = jobChecker.Elements("SingleNodeUserControlPath").FirstOrDefault();
                string? singleNodeUserControlPath = tmpXElement?.Value;
                tmpXElement = jobChecker.Elements("UserControlPath").FirstOrDefault();
                singleNodeUserControlPath = tmpXElement == null ? singleNodeUserControlPath : tmpXElement.Value;

                bool threadLocked = false;
                string? lockName = null;
                try
                {
                    tmpXElement = jobChecker.Element("ThreadLocked");
                    if (tmpXElement != null)
                    {
                        threadLocked = Convert.ToBoolean(tmpXElement.Value);
                        XAttribute? xVar = tmpXElement.Attributes("LockName").FirstOrDefault();
                        lockName = xVar?.Value;
                    }
                    tmpXElement = jobChecker.Element("InitNodes");
                    bool initNodes = tmpXElement != null && Convert.ToBoolean(tmpXElement.Value);
                    tmpXElement = jobChecker.Element("TriggeredRunDelay");
                    int triggeredRunDelay = tmpXElement == null ? 0 : Convert.ToInt32(tmpXElement.Value);
                    tmpXElement = jobChecker.Element("IsGlobal");
                    bool isGlobal = tmpXElement != null && Convert.ToBoolean(tmpXElement.Value);
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
                    tmpXElement = jobChecker.Element("CanRunDllPath");
                    if (tmpXElement != null)
                    {
                        checkerShell.CanRunDllPath = tmpXElement.Value;
                    }
                    // string pluginDirectory = Path.Combine(this._resolvedJobDir, "Plugin");
                    if (checkerShell.CheckerParameters != null)
                    {
                        checkerShell.CheckerParameters = replaceWildcardsNPathes(checkerShell.CheckerParameters, _appSettings.AssemblyDirectories.ToArray());
                    }
                    tmpString = jobChecker.Element("LogicalName")?.Value;
                    if (String.IsNullOrEmpty(tmpString))
                    {
                        throw new ArgumentException("'LogicalName' muss gesetzt werden(Checker).");
                    }
                    jobPackage.Job.Checkers.Add(tmpString, checkerShell);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(String.Format("Fehler in {0}({1})\n{2}", logicalJobName, jobXML, ex.Message), ex);
                }
            }

            //====================== ValueModifiers ==================================================================================
            IEnumerable<XElement> jobValueModifiers = from item in subJobRoot.Elements("ValueModifiers").Elements() select item;
            foreach (XElement jobValueModifier in jobValueModifiers)
            {
                XElement? item = jobValueModifier.Element("PhysicalPath");
                string? jobValueModifierSlave = item == null ? null : replaceWildcardsNPathes(item.Value, _appSettings.AssemblyDirectories.ToArray());
                item = jobValueModifier.Element("Format");
                string? jobValueModifierFormat = item?.Value;
                item = jobValueModifier.Elements("SingleNodeUserControlPath").FirstOrDefault();
                string singleNodeUserControlPath = item == null ? jobPackage.Job.SingleNodeUserControlPath : Convert.ToString(item.Value);
                item = jobValueModifier.Elements("UserControlPath").FirstOrDefault();
                singleNodeUserControlPath = item == null ? singleNodeUserControlPath : Convert.ToString(item.Value);
                string? logicalName = jobValueModifier.Element("LogicalName")?.Value;
                if (logicalName != null)
                {
                    try
                    {
                        item = jobValueModifier.Element("IsGlobal");
                        bool isGlobal = item != null && Convert.ToBoolean(item.Value);
                        tmpString = jobValueModifier.Element("Reference")?.Value;
                        if (String.IsNullOrEmpty(tmpString))
                        {
                            throw new ArgumentException("Referenz nicht gefunden. Ein ValueModifier braucht zwingend eine gültige 'Reference'.");
                        }
                        string reference = tmpString;
                        switch (jobValueModifier.Element("Type")?.Value.ToUpper())
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
                        throw new ArgumentException(String.Format("Fehler in {0}({1})\n{2}", logicalJobName, jobXML, ex.Message), ex);
                    }
                }
            }

            //====================== Triggers ========================================================================================
            IEnumerable<XElement> jobTriggers = from item in subJobRoot.Elements("Triggers").Elements() select item;
            foreach (XElement singleJobTrigger in jobTriggers)
            {
                TriggerShell trigger;
                paraString = null;
                tmpXElement = singleJobTrigger.Element("Parameters");
                if (tmpXElement != null)
                {
                    paraString = replaceWildcardsNPathes(tmpXElement.Value, _appSettings.AssemblyDirectories.ToArray());
                }
                tmpString = singleJobTrigger.Element("Reference")?.Value;
                if (tmpString != null)
                {
                    if (paraString != null && paraString == xmlLogicalJobName)
                    {
                        paraString = logicalJobName; // Referenz ggf. umbiegen
                    }
                    trigger = new TriggerShell(tmpString, paraString, false);
                    XElement? name = singleJobTrigger.Element("LogicalName");
                    if (name != null)
                    {
                        jobPackage.Job.Triggers.Add(name.Value, trigger);
                    }
                }
                else
                {
                    string? tmpPhysicalPath = singleJobTrigger.Element("PhysicalPath")?.Value;
                    string? tmpLogicalName = singleJobTrigger.Element("LogicalName")?.Value;
                    if (tmpPhysicalPath == null || tmpLogicalName == null)
                    {
                        throw new ArgumentException(
                            "'Reference' und/oder 'PhysicalPath' nicht gefunden. Ein Trigger ohne 'Reference' muss einen 'PhysicalPath' und einen 'LogicalName' haben?");
                    }
                    trigger = new TriggerShell(replaceWildcardsNPathes(tmpPhysicalPath,
                      _appSettings.AssemblyDirectories.ToArray()), paraString);
                    jobPackage.Job.Triggers.Add(tmpLogicalName, trigger);
                }
            }

            //====================== Loggers =========================================================================================
            IEnumerable<XElement> jobLoggers = from item in subJobRoot.Elements("Loggers").Elements() select item;
            foreach (XElement singleJobLogger in jobLoggers)
            {
                tmpString = singleJobLogger.Element("Parameters")?.Value;
                paraString = tmpString
                    == null ? null : replaceWildcardsNPathes(tmpString, _appSettings.AssemblyDirectories.ToArray());
                if (paraString == xmlLogicalJobName)
                {
                    paraString = logicalJobName; // Referenz ggf. umbiegen
                }
                tmpString = singleJobLogger.Element("PhysicalPath")?.Value;
                if (tmpString != null && paraString != null)
                {
                    LoggerShell logger = new LoggerShell(replaceWildcardsNPathes(tmpString,
                      _appSettings.AssemblyDirectories.ToArray()), paraString);
                    tmpString = singleJobLogger.Element("LogicalName")?.Value;
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

            //====================== Workers =========================================================================================
            IEnumerable<XElement> jobWorkers = from item in subJobRoot.Elements("Workers").Elements() select item;
            foreach (XElement jobWorker in jobWorkers)
            {
                tmpString = jobWorker.Element("LogicalExpression")?.Value;
                if (tmpString == null)
                {
                    throw new ArgumentException("'LogicalExpression' ist nicht gesetzt (Worker).");
                }
                string nodeId_Events = tmpString;
                string nodeId = (nodeId_Events + ":").Split(new char[] { ':' })[0].Trim();
                if (nodeId != null && nodeId == xmlLogicalJobName)
                {
                    nodeId_Events = nodeId_Events.Replace(nodeId, logicalJobName);
                    nodeId = logicalJobName; // Referenz ggf. umbiegen
                }
                string events = (nodeId_Events + ":").Split(new char[] { ':' })[1].Trim();
                if (!String.IsNullOrEmpty(events))
                {
                    List<WorkerShell> subWorkerList = new List<WorkerShell>();
                    IEnumerable<XElement> subWorkers = from item in jobWorker.Elements("SubWorkers").Elements() select item;
                    foreach (XElement subWorker in subWorkers)
                    {
                        TriggerShell? trigger = null;
                        bool transportByFile = false;
                        XElement? workerTrigger = subWorker.Element("Trigger");
                        if (workerTrigger != null)
                        {
                            paraString = null;
                            XElement? para = workerTrigger.Element("Parameters");
                            if (para != null)
                            {
                                paraString = para.Value;
                            }
                            tmpString = workerTrigger.Element("Reference")?.Value;
                            if (tmpString != null)
                            {
                                if (paraString != null && paraString == xmlLogicalJobName)
                                {
                                    paraString = logicalJobName; // Referenz ggf. umbiegen
                                }
                                trigger = new TriggerShell(tmpString, paraString, false);
                            }
                            else
                            {
                                tmpString = workerTrigger.Element("PhysicalPath")?.Value;
                                if (tmpString != null)
                                {
                                    trigger = new TriggerShell(replaceWildcardsNPathes(tmpString,
                                      _appSettings.AssemblyDirectories.ToArray()), paraString);
                                }
                                else
                                {
                                    throw new ArgumentException(
                                        "'PhysicalPath' ist nicht gesetzt (Trigger).");
                                }
                            }
                        }
                        XElement? subWorkerPara = subWorker.Element("Parameters");
                        if (subWorkerPara != null)
                        {
                            XAttribute? xVar = subWorkerPara.Attributes("Transport").FirstOrDefault();
                            transportByFile = xVar != null && xVar.Value.ToLower() == "file";
                            IEnumerable<XElement> subWorkerParametersSubWorkers = from item in subWorkerPara.Elements("SubWorkers").Elements() select item;
                            foreach (XElement subSubWorker in subWorkerParametersSubWorkers)
                            {
                                tmpString = subSubWorker.Element("PhysicalPath")?.Value;
                                if (tmpString != null)
                                {
                                    string newPhysicalPath = replaceWildcardsNPathes(tmpString,
                                      _appSettings.AssemblyDirectories.ToArray());
                                    subSubWorker.Element("PhysicalPath")?.ReplaceWith(new XElement("PhysicalPath", newPhysicalPath));
                                }
                            }
                            tmpString = subWorker.Element("PhysicalPath")?.Value;
                            if (tmpString != null)
                            {
                                subWorkerList.Add(new WorkerShell(replaceWildcardsNPathes(tmpString,
                                  _appSettings.AssemblyDirectories.ToArray()),
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

            //====================== SubJobs =========================================================================================
            IEnumerable<XElement> subJobs = from item in subJobRoot.Elements("SubJobs").Elements() select item;
            jobPackage.Job.MaxSubJobDepth = 0;
            int hasSubJobs = 0;
            bool startSubJobCollapsed;
            foreach (XElement subJob in subJobs)
            {
                hasSubJobs = 1;
                try
                {
                    XElement? tmpVar2 = (subJob.Elements("StartCollapsed").FirstOrDefault());
                    startSubJobCollapsed = tmpVar2 != null && Convert.ToBoolean(tmpVar2.Value);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(String.Format("Fehler in {0}({1})\n{2}", logicalJobName, jobXML, ex.Message), ex);
                }
                string physicalPath = "";
                XElement? xvar = subJob.Element("PhysicalPath");
                tmpString = subJob.Element("LogicalName")?.Value;
                if (xvar != null)
                {
                    physicalPath = replaceWildcardsNPathes(xvar.Value, _appSettings.JobDirPathes.ToArray());
                }
                else
                {
                    xvar = subJob.Element("LogicalExpression");
                    if (xvar == null)
                    {
                        if (tmpString != null)
                        {
                            physicalPath = replaceWildcardsNPathes(tmpString, _appSettings.JobDirPathes.ToArray());
                        }
                        else
                        {
                            throw new ArgumentException("");
                        }
                    }
                }
                //====================== Rekursion ======================================================================================
                int depth = 0;
                try
                {
                    loadJob(jobPackage.Job, physicalPath, tmpString, subJob,
                      startSubJobCollapsed, null, null, false, false, out depth);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(String.Format($"Ladefehler auf {physicalPath}\r\n{ex.Message}"));
                }
                if (depth > jobPackage.Job.MaxSubJobDepth)
                {
                    jobPackage.Job.MaxSubJobDepth = depth;
                }
            }
            jobPackage.Job.MaxSubJobDepth += hasSubJobs;
            subJobDepth = jobPackage.Job.MaxSubJobDepth;
            //int calcDepth = (jobPackage.Job.MaxSubJobDepth + 1);
            //jobPackage.Job.LogicalChangedDelay = this._appSettings.LogicalChangedDelay
            //  * (calcDepth * (calcDepth + 1) / 2);
            int calcDepth = jobPackage.Job.MaxSubJobDepth;
            jobPackage.Job.LogicalChangedDelay = (int)(_appSettings.LogicalChangedDelay
                                                        * Math.Pow(_appSettings.LogicalChangedDelayPower, calcDepth));
            //====================== Snapshots =======================================================================================
            IEnumerable<XElement> snapshots = from item in subJobRoot.Elements("Snapshots").Elements() select item;
            foreach (XElement snapshot in snapshots)
            {
                bool startSnapshotCollapsed;
                string physicalSnapshotName;
                string physicalSnapshotPath;
                TriggerShell? trigger = null;
                string logicalTriggerName;
                string logicalSnapshotName;
                try
                {
                    tmpString = snapshot.Element("LogicalName")?.Value;
                    logicalSnapshotName = tmpString ?? throw new ArgumentException("'LogicalName' ist nicht angegeben (Snapshot).");
                    jobPackage.Job.SnapshotNames.Add(logicalSnapshotName);
                    physicalSnapshotName = logicalSnapshotName.TrimStart('#');
                    startSnapshotCollapsed
                        = (snapshot.Elements("StartCollapsed").FirstOrDefault()?.Value) != null
                            && Convert.ToBoolean(snapshot.Elements("StartCollapsed").FirstOrDefault()?.Value);
                    tmpXElement = snapshot.Element("PhysicalPath");
                    if (tmpXElement == null)
                    {
                        physicalSnapshotPath = physicalSnapshotName;
                    }
                    else
                    {
                        physicalSnapshotPath = _appSettings.ReplaceWildcards(tmpXElement.Value);
                        if (physicalSnapshotPath.ToLower().EndsWith("jobsnapshot.xml")
                            || physicalSnapshotPath.ToLower().EndsWith("sobsnapshot.info"))
                        {
                            physicalSnapshotPath = Path.GetDirectoryName(physicalSnapshotPath) ?? Directory.GetCurrentDirectory();
                        }
                    }
                    if (!(physicalSnapshotPath.Contains(":")
                          || physicalSnapshotPath.StartsWith(@"/")
                          || physicalSnapshotPath.StartsWith(@"\")))
                    {
                        //if (!physicalSnapshotPath.ToLower().EndsWith(physicalSnapshotName.ToLower()))
                        //{
                        //  physicalSnapshotPath = Path.Combine(_appSettings.ResolvedSnapshotDirectory, physicalSnapshotName, physicalSnapshotPath);
                        //}
                        //else
                        //{
                        physicalSnapshotPath = Path.Combine(_appSettings.ResolvedSnapshotDirectory, physicalSnapshotPath);
                        //}
                    }
                    physicalSnapshotPath = Path.Combine(physicalSnapshotPath, "JobSnapshot.info");
                    logicalTriggerName = logicalSnapshotName + "Trigger";
                    tmpXElement = snapshot.Element("Trigger");
                    if (tmpXElement != null)
                    {
                        paraString = null;
                        tmpString = tmpXElement.Element("Parameters")?.Value;
                        if (tmpString != null)
                        {
                            paraString = replaceWildcardsNPathes(tmpString.Replace("JobSnapshot.xml", "JobSnapshot.info"), new string[] { });
                        }
                        else
                        {
                            paraString = physicalSnapshotPath + "|InitialFire";
                        }
                        if (paraString.ToUpper().Contains("%SNAPSHOTDIRECTORIES%"))
                        {
                            paraString = Regex.Replace(paraString, "%SNAPSHOTDIRECTORIES%", String.Join(",", Path.GetDirectoryName(physicalSnapshotPath)), RegexOptions.IgnoreCase);
                        }
                        tmpString = tmpXElement.Element("Reference")?.Value;
                        if (tmpString != null)
                        {
                            if (paraString != null && paraString == xmlLogicalJobName)
                            {
                                paraString = logicalJobName; // Referenz ggf. umbiegen
                            }
                            trigger = new TriggerShell(tmpString, paraString, false);
                        }
                        else
                        {
                            tmpString = tmpXElement.Element("PhysicalPath")?.Value
                                ?? throw new ArgumentException("'Reference' und/oder 'PhysicalPath' nicht gefunden. Ein Trigger ohne 'Reference' muss einen 'PhysicalPath' haben?");
                            trigger = new TriggerShell(replaceWildcardsNPathes(tmpString,
                              _appSettings.AssemblyDirectories.ToArray()), paraString);
                        }
                        tmpString = tmpXElement.Element("LogicalName")?.Value;
                        if (tmpString != null)
                        {
                            logicalTriggerName = tmpString;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(String.Format("Fehler in {0}({1})\n{2}", logicalJobName, jobXML, ex.Message), ex);
                }
                //====================== Rekursion ======================================================================================
                int depth = 0;
                try
                {
                    loadJob(jobPackage.Job, physicalSnapshotPath, logicalSnapshotName, snapshot,
                      startSnapshotCollapsed, trigger, logicalTriggerName, false, true, out depth);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(String.Format($"Ladefehler auf {physicalSnapshotPath}\r\n{ex.Message}"));
                }
            }
            try
            {
                LoadedJobPackages.Add(jobPackage.JobName, jobPackage);
            }
            catch
            {
                throw new ApplicationException(String.Format("Job {0}: Ein Job kann nur einmal hinzugefügt werden.\nHinweis: eine mehrfache Referenzierung in einem logischen Ausdruck ist jedoch möglich.", jobPackage.JobName));
            }
            if (popDirs)
            {
                _appSettings.JobDirPathes.Pop();
            }
            return logicalJobName;
        }

        private string replaceWildcardsNPathes(string para, string[] searchDirectories)
        {
            para = _appSettings.ReplaceWildcards(para);
            string paraString = "";
            string delimiter = "";
            foreach (string paraPart in para.Split('|'))
            {
                string modifiedParaPart = paraPart;
                if (modifiedParaPart.Trim() != String.Empty)
                {
                    // TODO: diese Ersetzung noch besser gegen versehentliches Einfügen von Pfaden schützen.
                    modifiedParaPart = GetJobElementResolvedPath(modifiedParaPart, searchDirectories);
                }
                paraString += delimiter + modifiedParaPart;
                delimiter = "|";
            }
            return paraString;
        }

        private ValueModifier<T> createTypedValueModifier<T>(string referenceName, string? slavePath, string? format, string userControlPath, bool isGlobal)
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

        private string GetJobElementResolvedPath(string path, string[] searchDirectories)
        {
            if (path.StartsWith("\\\\") || path.StartsWith(@"//") || (path.Length > 1 && path[1] == ':') || Regex.IsMatch(path, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
            {
                // Path.IsPathRooted erkennt führende IPs nicht und liefert auch bei nur einem führenden '/' oder '\' true.
                return path.Replace(@"Plugin\Plugin", "Plugin");
            }
            else
            {
                if (!(path.Contains(".") || path.Contains("\\") || path.Contains(@"/")))
                {
                    // wird nicht als Pfad betrachtet und direkt zurückgegeben
                    return path;
                }
                string tmpPath = path.Replace(@"Plugin\Plugin", "Plugin");
                try
                {
                    foreach (string tmpDirectory in searchDirectories)
                    {
                        if (tmpDirectory != "")
                        {
                            if (isPathServerReachable(tmpDirectory) && Directory.Exists(tmpDirectory))
                            {
                                //string tmpPath2 = Path.Combine(tmpDirectory, tmpPath);
                                string tmpPath2 = String.IsNullOrEmpty(tmpDirectory) ?
                                    tmpPath : tmpDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar + tmpPath;
                                // Anmerkung: Path.Combine schmeißt den ersten Pfad komplett weg,
                                // wenn der zweite Pfad ein absoluter Pfad ist. Deswegen erfolgt weiter oben
                                // die Abfrage auf absoluten Pfad am Anfang.
                                if (File.Exists(tmpPath2) || Directory.Exists(tmpPath2))
                                {
                                    return tmpPath2;
                                }
                            }
                        }
                    }
                }
                catch (ArgumentException)
                {
                }
                return tmpPath;
            }
        }

        private bool isPathServerReachable(string tmpPath)
        {
            string server = tmpPath;
            bool serverIsIP = true;
            if (server.StartsWith("\\\\") || server.StartsWith(@"//"))
            {
                serverIsIP = false;
                server = server.Substring(2);
            }
            else
            {
                if (!Regex.IsMatch(server, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
                {
                    return true;
                }
            }
            server = Path.GetDirectoryName(server) ?? Directory.GetCurrentDirectory();
            if (!serverIsIP)
            {
                server = "\\\\" + server;
            }
            if (ReachableServers.ContainsKey(server))
            {
                return ReachableServers[server];
            }
            int retries = 3;
            int timeout = 2000;
            int retry = 0;
            while (retry++ < retries && !canPing(server, timeout)) { }
            if (retry > retries)
            {
                ReachableServers.Add(server, false);
            }
            else
            {
                ReachableServers.Add(server, true);
            }
            return ReachableServers[server];
        }

        private bool canPing(string address, int timeout)
        {
            Ping ping = new Ping();
            try
            {
                PingReply reply = ping.Send(address, timeout);
                if (reply == null) { return false; }

                return (reply.Status == IPStatus.Success);
            }
            catch (PingException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
        }
        #endregion private members
    }
}
