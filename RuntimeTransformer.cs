﻿
using System;
using System.IO;
using System.Data;
using SyncroSim.Core;
using SyncroSim.StochasticTime;
using System.Globalization;
using System.Collections.Generic;

namespace SyncroSim.NetLogo
{
    class RuntimeTransformer : StochasticTimeTransformer
    {
        private string m_ExeName;
        private string m_JarFileName;
        private DataSheet m_RunControl;
        private DataSheet m_InputFileSymbols;
        private DataSheet m_OtherSymbols;
        private DataSheet m_OutputVariable;
        private DataSheet m_OutputVariableRaster;
        private InputFileMap m_InputFileMap;
        private int m_MinimumIteration;
        private int m_MaximumIteration;
        private int m_MinimumTimestep;
        private int m_MaximumTimestep;
        private string m_TemplateFileName;
        private string m_ExperimentName;
        private const string DEFAULT_NETLOGO_FOLDER = "NetLogo 6.0";
        private const string NETLOGO_EXE_NAME = "NetLogo.exe";

        public override void Initialize()
        {
            base.Initialize();

            this.InitializeDataSheets();
            this.InitializeRunControl();
            this.InitializeExeName();
            this.InitializeJarFileName();
            this.InitializeInputFiles();
        }

        private void InitializeDataSheets()
        {
            this.m_RunControl = this.ResultScenario.GetDataSheet("NetLogo_RunControl");
            this.m_InputFileSymbols = this.ResultScenario.GetDataSheet("NetLogo_InputFileSymbol");
            this.m_OtherSymbols = this.ResultScenario.GetDataSheet("NetLogo_OtherSymbol");
            this.m_OutputVariable = this.ResultScenario.GetDataSheet("NetLogo_OutputVariable");
            this.m_OutputVariableRaster = this.ResultScenario.GetDataSheet("NetLogo_OutputVariableRaster");
        }

        private void InitializeRunControl()
        {
            this.m_MinimumIteration = Convert.ToInt32(this.GetRunControlValue("MinimumIteration"), CultureInfo.InvariantCulture);
            this.m_MaximumIteration = Convert.ToInt32(this.GetRunControlValue("MaximumIteration"), CultureInfo.InvariantCulture);
            this.m_MinimumTimestep= Convert.ToInt32(this.GetRunControlValue("MinimumTimestep"), CultureInfo.InvariantCulture);
            this.m_MaximumTimestep = Convert.ToInt32(this.GetRunControlValue("MaximumTimestep"), CultureInfo.InvariantCulture);
            this.m_TemplateFileName = Convert.ToString(this.GetRunControlValue("TemplateFile"), CultureInfo.InvariantCulture);
            this.m_ExperimentName = Convert.ToString(this.GetRunControlValue("Experiment"), CultureInfo.InvariantCulture);
        }

        private void InitializeExeName()
        {
            this.m_ExeName = this.GetExternalExecutableName(NETLOGO_EXE_NAME, DEFAULT_NETLOGO_FOLDER);

            if (this.m_ExeName == null || !Path.IsPathRooted(this.m_ExeName))
            {
                throw new InvalidOperationException("Cannot find NetLogo.exe - please configure your library properties.");
            }
        }

        private void InitializeJarFileName()
        {
            string AppDir = Path.Combine(Path.GetDirectoryName(this.m_ExeName), "app");
            string[] Files = Directory.GetFiles(AppDir, "netlogo-*.jar");

            if (Files.Length == 1)
            {
                this.m_JarFileName = Files[0];
            }
            else
            {
                throw new InvalidOperationException("Cannot find NetLogo Jar file in: " + AppDir);
            }
        }

        void InitializeInputFiles()
        {
            this.m_InputFileMap = new InputFileMap();           
            DataTable dt = this.m_InputFileSymbols.GetData();

            foreach (DataRow dr in dt.Rows)
            {
                Nullable<int> Iteration = GetNullableInt(dr, "Iteration");
                string Symbol = (string)dr["Symbol"];
                string Filename = (string)dr["Filename"];

                this.m_InputFileMap.AddInputFileRecord(Iteration, Symbol, Filename);
            }
        }

        protected override void OnIteration(int iteration)
        {
            base.OnIteration(iteration);

            string TemplateFileName = this.CreateNetLogoTemplateFile(iteration);

            if (this.IsUserInteractive())
            {
                base.ExternalTransform(this.m_ExeName, null, TemplateFileName, null);
            }
            else
            {
                string args = string.Format(CultureInfo.InvariantCulture,
                    "-Xmx1024m -Dfile.encoding=UTF-8 -cp \"{0}\" org.nlogo.headless.Main --model \"{1}\" --experiment {2}",
                    this.m_JarFileName, TemplateFileName, this.m_ExperimentName);

                base.ExternalTransform("java", null, args, null);
            }

            this.RetrieveOutputFiles(iteration);
        }

        private void RetrieveOutputFiles(int iteration)
        {
            //DataTable OutRastTable = this.m_OutputRasterFiles.GetData();
            //DataTable OutFileSymsTable = this.m_OutputFileSymbols.GetData();
            //string TempFolderName = this.Library.CreateTempFolder("NetLogo", false);
            //string OutRasterFolderName = this.Library.GetFolderName(LibraryFolderType.Output, this.m_OutputRasterFiles, true);

            //foreach (DataRow dr in OutFileSymsTable.Rows)
            //{
            //    string BaseName = (string)dr["Filename"];
            //    string SourceFileName = Path.Combine(TempFolderName, BaseName);

            //    if (File.Exists(SourceFileName))
            //    {
            //        string FormattedBaseName = string.Format(
            //            CultureInfo.InvariantCulture, 
            //            "{0}-It{1}-Ts{2}", 
            //            Path.GetFileNameWithoutExtension(BaseName), 
            //            iteration, timestep);

            //        string AsciiName = Path.Combine(OutRasterFolderName, FormattedBaseName + ".asc");
            //        string TifName = Path.Combine(OutRasterFolderName, FormattedBaseName + ".tif");
            //        string OtherName = Path.Combine(OutRasterFolderName, FormattedBaseName + Path.GetExtension(SourceFileName));

            //        if (Path.GetExtension(SourceFileName).ToUpperInvariant() == ".ASC")
            //        {
            //            if (!Translate.GdalTranslate(SourceFileName, TifName, GdalOutputFormat.GTiff, GdalOutputType.Float64, GeoTiffCompressionType.None, null))
            //            {
            //                throw new InvalidOperationException("Cannot translate from ASCII: " + SourceFileName);
            //            }

            //            OutRastTable.Rows.Add(new object[] { iteration, timestep, Path.GetFileName(TifName) });
            //        }
            //        else if(Path.GetExtension(SourceFileName).ToUpperInvariant() == ".TIF")
            //        {
            //            File.Copy(SourceFileName, AsciiName);
            //            OutRastTable.Rows.Add(new object[] { iteration, timestep, Path.GetFileName(TifName) });
            //        }
            //        else
            //        {
            //            File.Copy(SourceFileName, OtherName);
            //        }
            //    }
            //}
        }

        private string CreateNetLogoTemplateFile(int iteration)
        {       
            string TempFolderName = this.Library.CreateTempFolder("NetLogo", true);
            string f1 = this.GetRunControlFileName(this.m_TemplateFileName);
            string f2 = Path.Combine(TempFolderName, this.m_TemplateFileName);

            if (!File.Exists(f1))
            {
                throw new InvalidOperationException("The NetLogo template file was not found.");
            }

            this.WriteNetLogoTemplate(f1, f2, iteration, TempFolderName);
            return f2;
        }

        private void WriteNetLogoTemplate(string source, string target, int iteration, string tempFolderName)
        {
            string IterString = iteration.ToString(CultureInfo.InvariantCulture);
            string TickString = (this.m_MaximumTimestep - this.m_MinimumTimestep + 1).ToString(CultureInfo.InvariantCulture);
            string VariableFileName = Path.Combine(tempFolderName, "OutputVariable.csv");
            string VariableRasterFileName = Path.Combine(tempFolderName, "OutputVariableRaster.csv");

            string vf = "\"" + VariableFileName.Replace(@"\", @"\\") + "\"";
            string vrf = "\"" + VariableRasterFileName.Replace(@"\", @"\\") + "\"";

            using (StreamReader s = new StreamReader(source))
            {
                string line;

                using (StreamWriter t = new StreamWriter(target))
                {
                    while ((line = s.ReadLine()) != null)
                    {
                        line = this.ProcessSystemSymbols(line, IterString, TickString, vf, vrf);
                        line = this.ProcessInputFileSymbols(line, iteration, tempFolderName);
                        line = this.ProcessOtherSymbols(line);

                        t.WriteLine(line);
                    }
                }
            }
        }

        private string ProcessSystemSymbols(
            string line, 
            string iteration, 
            string ticks, 
            string variableFileName, 
            string variableRasterFileName)
        {
            string l = line;

            l = l.Replace("%SSIM_ITERATION%", iteration);
            l = l.Replace("%SSIM_TICKS%", ticks);
            l = l.Replace("%SSIM_VARIABLE_FILENAME%", variableFileName);
            l = l.Replace("%SSIM_VARIABLE_RASTER_FILENAME%", variableRasterFileName);

            return (l);
        }

        private string ProcessInputFileSymbols(string line, int iteration, string tempFolderName)
        {
            string l = line;
            List<InputFileRecord> recs = this.m_InputFileMap.GetInputFileRecords(iteration);

            if (recs != null)
            {
                foreach (InputFileRecord r in recs)
                {
                    string sym = "%" + r.Symbol + "%";

                    if (l.Contains(sym))
                    {
                        string f1 = this.GetInputFileName(r.Filename);
                        string f2 = Path.Combine(tempFolderName, r.Filename);
                        string val = "\"" + f2.Replace(@"\", @"\\") + "\"";

                        l = l.Replace(sym, val);

                        if (!File.Exists(f2))
                        {
                            File.Copy(f1, f2);
                        }
                    }
                }
            }

            return (l);
        }

        private string ProcessOtherSymbols(string line)
        {
            string l = line;

            foreach (DataRow dr in this.m_OtherSymbols.GetData().Rows)
            {
                string sym = "%" + (string)dr["Symbol"] + "%";
                string val = (string)dr["Value"];

                l = l.Replace(sym, val);
            }             

            return (l);
        }

        private object GetRunControlValue(string columnName)
        {
            DataRow dr = this.m_RunControl.GetDataRow();

            if (dr == null || dr[columnName] == DBNull.Value)
            {
                throw new ArgumentException("The run control data is missing for: " + columnName);
            }

            return dr[columnName];
        }

        private static int? GetNullableInt(DataRow dr, string columnName)
        {
            object value = dr[columnName];

            if (object.ReferenceEquals(value, DBNull.Value) || object.ReferenceEquals(value, null))
            {
                return null;
            }
            else
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }

        private string GetRunControlFileName(string fileName)
        {
            string f = this.Library.GetFolderName(LibraryFolderType.Input, this.m_RunControl, false);
            return (Path.Combine(f, fileName));
        }

        private string GetInputFileName(string fileName)
        {
            string f = this.Library.GetFolderName(LibraryFolderType.Input, this.m_InputFileSymbols, false);
            return (Path.Combine(f, fileName));
        }
    }
}
