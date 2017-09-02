﻿
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System;
using System.Windows.Forms;

namespace Pyramid
{

    class CodeXLResultSet : IResultSet
    {
        private AMDResultsPanel m_ResultsPanel ;
        private CodeXLAnalysisPanel m_AnalysisPanel;

        public CodeXLResultSet()
        {
            m_ResultsPanel = new AMDResultsPanel();
            m_AnalysisPanel = new CodeXLAnalysisPanel();
            m_ResultsPanel.AsicChanged +=
                delegate(string asic)
                {
                    m_AnalysisPanel.SetAsic(asic);
                };
        }

        public Control ResultsPanel  { get { return m_ResultsPanel; } }
        public Control AnalysisPanel { get { return m_AnalysisPanel; } }
        public string Name { get { return "CodeXL"; } }
        public int ResultCount { get { return m_ResultsPanel.ResultCount; } }

        public void AddCompileResult( string asic, string il, string isa )
        {
            m_ResultsPanel.AddResult(asic, il, isa);
        }

        public void AddAnalysisResult( string asic, Dictionary<string,string> vals )
        {
            m_AnalysisPanel.AddResult(asic, vals);
        }

        public void DisplayAsic(string asic)
        {
            m_AnalysisPanel.SetAsic(asic);
            m_ResultsPanel.SetAsic(asic);
        }
    }

    class CodeXLBackendOptions : IBackendOptions
    {
        private List<string> m_RequestedAsics;

        public CodeXLBackendOptions(List<string> requestedAsics)
        {
            m_RequestedAsics = (requestedAsics != null) ? requestedAsics : new List<string>();
        }

        public List<string> Asics { get { return m_RequestedAsics; } }
    }

    class CodeXLBackend : IBackend
    {
        private List<string> m_SupportedAsics = new List<string>();
        private string m_CodeXL = "";
        private string m_D3DCompiler = "";
        private string m_TempPath = "";

        public string Name { get { return "CodeXL"; } }
        public IEnumerable<string> Asics { get { return m_SupportedAsics; } }

        public static List<String> GetSupportedAsics( string CodeXLPath, string d3dpath  )
        {
            // try to run CodeXLAnalyzer to get the asic list
            // -s "HLSL" --DXLocation s -l"
            string CommandLine = string.Format("-s \"HLSL\" --DXLocation {0} -l", d3dpath);
            List<String> asics = new List<string>();

            ProcessStartInfo pi = new ProcessStartInfo();
            pi.RedirectStandardOutput = true;
            pi.RedirectStandardInput = true;
            pi.RedirectStandardError = true;
            pi.CreateNoWindow = true;
            pi.Arguments = CommandLine;
            pi.FileName = CodeXLPath;
            pi.UseShellExecute = false;

            try
            {
                Process p = Process.Start(pi);
                p.WaitForExit();

                while (!p.StandardOutput.EndOfStream)
                {
                    string s = p.StandardOutput.ReadLine();
                    asics.Add(s.TrimEnd().TrimStart());
                }

                // skip whatever text they're emitting, up to 'Devices:'
                while (!asics[0].Equals("Devices:"))
                    asics.RemoveAt(0);
                asics.RemoveAt(0); // remove 'Devices'

                p.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "uh-oh, couldn't get CodeXL asic list", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return asics;
        }

        public CodeXLBackend( string CodeXLPath, string D3DCompilerPath, string TempPath )
        {
            m_SupportedAsics = GetSupportedAsics(CodeXLPath, D3DCompilerPath);

            m_CodeXL        = CodeXLPath;
            m_D3DCompiler   = D3DCompilerPath;
            m_TempPath      = TempPath;
        }

        private bool CompileForAsic( List<string> asics, string asic )
        {
            if (asics == null || asics.Count == 0)
                return true;

            return asics.Contains(asic);
        }

        public IResultSet Compile(IShader shader, IBackendOptions options)
        {
            if ( !(shader is HLSLShader) || !(options is CodeXLBackendOptions) )
                return null;

            // TODO: Modify CodeXL backend so that it re-uses blob from
            //    FXC backend where available.  It'd be nice not to have to 
            //    have CodeXL recompile it for us
            HLSLShader shaderHLSL = shader as HLSLShader;
            IHLSLOptions hlslOpts = shaderHLSL.CompileOptions;
            CodeXLBackendOptions backendOptions = options as CodeXLBackendOptions;
            string text = shader.Code;

            if (shaderHLSL.WasCompiledWithErrors)
                return null;

            string tmpFile = Path.Combine(m_TempPath, "PYRAMID_amdcodexl");
            try
            {
                StreamWriter stream = File.CreateText(tmpFile);
                stream.Write(text);
                stream.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "uh-oh, couldn't create temp file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            string isaPath = Path.Combine(m_TempPath, "pyramid.isa");
            string analysisPath = Path.Combine(m_TempPath, "analysis");
            string CommandLine = "-s \"HLSL\"";
            CommandLine = String.Concat( CommandLine, String.Format( " -p {0}",hlslOpts.Target.ToString()) );
            CommandLine = String.Concat( CommandLine, String.Format( " -f {0} ",hlslOpts.EntryPoint ));
            CommandLine = String.Concat( CommandLine, String.Format( " --DXLocation \"{0}\"",m_D3DCompiler ));
            CommandLine = String.Concat( CommandLine, String.Format( " --isa \"{0}\"",isaPath ));
            CommandLine = String.Concat( CommandLine, String.Format( " -a \"{0}\"",analysisPath ));
            CommandLine = String.Concat( CommandLine, String.Format( " --DXFlags {0} ",hlslOpts.GetD3DCompileFlagBits() ));

            CommandLine = String.Concat(CommandLine, tmpFile);

            foreach (string asic in m_SupportedAsics)
            {
                if (CompileForAsic(backendOptions.Asics, asic))
                {
                    CommandLine = String.Concat(CommandLine, " -c ", asic, " ");
                }
            }
            
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.RedirectStandardOutput = true;
            pi.RedirectStandardInput = true;
            pi.RedirectStandardError = true;
            pi.CreateNoWindow = true;
            pi.Arguments = CommandLine;
            pi.FileName = m_CodeXL;
            pi.UseShellExecute = false;

            string output;
            try
            {
                int TimeOut = 15000; // TODO: Put in options
                Process p = Process.Start(pi);
                output = p.StandardOutput.ReadToEnd();
                if (p.WaitForExit(TimeOut))
                {
                    MessageBox.Show("CodeXL took more than 15 seconds");
                }
                p.Close();
                File.Delete(tmpFile);
            }
            catch( Exception e )
            {
                MessageBox.Show(e.Message, "uh-oh, couldn't run CodeXL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // Compile results are emitted in one set of files per asic
            string defaultAsic = "";
            CodeXLResultSet results  = new CodeXLResultSet();
            foreach (string asic in m_SupportedAsics)
            {
                if (!CompileForAsic(backendOptions.Asics, asic))
                {
                    continue;
                }

                if (defaultAsic == "")
                {
                    defaultAsic = asic;
                }

                string isaFile = Path.Combine(m_TempPath, String.Format("{0}_pyramid.isa", asic));

                try
                {
                    string isa = File.ReadAllText(isaFile);
                    results.AddCompileResult(asic, "CodeXL doesn't support IL output for HLSL", isa);

                    File.Delete(isaFile);
                }
                catch (Exception )
                {      
                    // may occur in the event of a compile error. 
                }
            }

            // Analysis results are emitted in a big CSV file
            try
            {
                string[] lines = File.ReadAllLines(analysisPath);
                File.Delete(analysisPath);

                try
                {
                    // first line is column names
                    string columns = lines[0];
                    string[] cols = columns.Split(',');

                    // first column is asic, remaining columns are fields we want to display
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] data = lines[i].Split(',');
                        string asic = data[0];
                        Dictionary<string, string> vals = new Dictionary<string, string>();
                        for (int j = 1; j < cols.Length; j++)
                        {
                            if( !String.IsNullOrEmpty(data[j]) && !String.IsNullOrEmpty(cols[j]))
                                vals.Add(cols[j], data[j]);
                        }

                        results.AddAnalysisResult(asic, vals);
                    }
                }
                catch( Exception e )
                {
                    MessageBox.Show(e.Message, "uh-oh. Couldn't parse CodeXL analysis file.  Did it change?", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch( Exception )
            {
                // compile error
            }

            if (results.ResultCount > 0)
            {
                results.DisplayAsic(defaultAsic);
                return results;
            }
            else
                return null;
        }
    }


}

