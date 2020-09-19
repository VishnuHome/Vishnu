using NetEti.Globals;
using Vishnu.Interchange;

namespace LogicalTaskTree.Provider
{
    /// <summary>
    /// Hart verdrahtete Demo-Version des IJobProvider.
    /// Wird normalerweise nicht verwendet; dient vielmehr zur Demonstration
    /// einer alternativen Ableitung von JobProviderBase.
    /// </summary>
    /// <remarks>
    /// File: DemoJobProvider.cs
    /// Autor: Erik Nagel
    ///
    /// 26.01.2012 Erik Nagel: erstellt
    /// </remarks>
    public class MixedTestJobProvider : JobProviderBase
    {
        private string _interval = "S:10"; // "H:1"; // "S:10"; // "MS:200"; // "H:15";

        /// <summary>
        /// Fügt dem Dictionary LoadedJobPackages das JobPackage
        /// mit dem logischen Pfad logicalJobName hinzu.
        /// Im Fehlerfall wird einfach nichts hinzugefügt.
        /// </summary>
        /// <param name="logicalJobName">Der logische Name des Jobs oder null beim Root-Job.</param>
        protected override void TryLoadJobPackage(ref string logicalJobName)
        {
            if (logicalJobName == null)
            {
                logicalJobName = "Root";
                string physicalJobPath = GenericSingletonProvider.GetInstance<AppSettings>().RootJobPackagePath;
                JobPackage jobPackage = new JobPackage(physicalJobPath, logicalJobName);
                jobPackage.Job.LogicalExpression = @"[[[( [ { SubJob | OR_var } & { c OR d } ] => [f >< g_var ])]]]";

                jobPackage.Job.Checkers.Add("a", new CheckerShell(AppSettings.GetResolvedPath("FalseChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:1|" + this._interval)));
                jobPackage.Job.Checkers.Add("b", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:2|" + this._interval)));
                jobPackage.Job.Checkers.Add("c", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:3|" + this._interval)));
                jobPackage.Job.Checkers.Add("d", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:4|" + this._interval)));
                jobPackage.Job.Checkers.Add("e", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:5|" + this._interval)));
                jobPackage.Job.Checkers.Add("f", new CheckerShell(AppSettings.GetResolvedPath("TrueFalseChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:6|" + this._interval)));
                jobPackage.Job.Checkers.Add("OR_var", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:7|" + this._interval)));
                jobPackage.Job.Checkers.Add("g_var", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:8|" + this._interval)));

                jobPackage.Job.Workers.Add("Root:False", new WorkerShell[] { new WorkerShell(AppSettings.GetResolvedPath("ConsoleMessageBox.exe"), new System.Xml.Linq.XElement("Parameter", "%MachineName%: Root:False")) });
                jobPackage.Job.BreakWithResult = false;
                this.LoadedJobPackages.Add(jobPackage.JobName, jobPackage);
            }
            if (logicalJobName == "SubJob")
            {
                JobPackage jobPackage = new JobPackage(logicalJobName, logicalJobName);
                jobPackage.Job.LogicalExpression = @"!(a < b & c >= d) <= e";

                jobPackage.Job.Checkers.Add("a", new CheckerShell(AppSettings.GetResolvedPath("FalseChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:1|" + this._interval)));
                jobPackage.Job.Checkers.Add("b", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:2|" + this._interval)));
                jobPackage.Job.Checkers.Add("c", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll")));
                jobPackage.Job.Checkers.Add("d", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:3|" + this._interval)));
                jobPackage.Job.Checkers.Add("e", new CheckerShell(AppSettings.GetResolvedPath("TrueChecker.dll"), new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:4|" + this._interval)));
                jobPackage.Job.BreakWithResult = false;
                jobPackage.Job.JobTrigger = new TriggerShell(AppSettings.GetResolvedPath("TimerTrigger.dll"), "S:30");
                this.LoadedJobPackages.Add(jobPackage.JobName, jobPackage);
            }
        }
    }
}
