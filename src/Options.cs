﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Pyramid
{
    public class Options
    {
        private List<string> m_DisabledBackends = new List<string>();
        private List<string> m_DisabledAMDAsics = new List<string>();
        private List<string> m_DisabledCodeXLAsics = new List<string>();
        private List<string> m_DisabledRGAAsics = new List<string>();

        public string RGAPath { get; set; }
        public string MysteryToolPath { get; set; }
        public string D3DCompilerPath { get; set; }
        public string CodeXLPath { get; set; }
        public string TempPath { get; set; }
        public string PowerVRCompilerPath { get; set; }
        public string DXXDriverPath { get; set; }
        public string MaliSCRoot { get; set; }
        public IEnumerable<string> DisabledBackends { get { return m_DisabledBackends; } }
        public IEnumerable<string> DisabledAMDAsics { get { return m_DisabledAMDAsics; } }
        public IEnumerable<string> DisabledCodeXLAsics { get { return m_DisabledCodeXLAsics; } }
        public IEnumerable<string> DisabledRGAAsics { get { return m_DisabledRGAAsics; } }

        public void DisableBackend(string name)
        {
            m_DisabledBackends.Add(name);
        }
        public bool IsBackendDisabled(string name)
        {
            return m_DisabledBackends.Contains(name);
        }

        public void DisableRGAAsic(string name)
        {
            m_DisabledRGAAsics.Add(name);
        }
        public bool IsRGAAsicDisabled(string name)
        {
            return m_DisabledRGAAsics.Contains(name);
        }


        public void DisableAMDAsic(string name)
        {
            m_DisabledAMDAsics.Add(name);
        }
        public bool IsAMDAsicDisabled(string name)
        {
            return m_DisabledAMDAsics.Contains(name);
        }

        public void DisableCodeXLAsic(string name)
        {
            m_DisabledCodeXLAsics.Add(name);
        }
        public bool IsCodeXLAsicDisabled(string name)
        {
            return m_DisabledCodeXLAsics.Contains(name);
        }

        public static Options GetDefaults()
        {
            Options opts = new Options();
            opts.D3DCompilerPath     = "d3dcompiler_47.dll";
            opts.CodeXLPath          = "CodeXLAnalyzer.exe";
            opts.PowerVRCompilerPath = "PowerVR";
            opts.DXXDriverPath = "atidxx32.dll";
            opts.MaliSCRoot = "MaliSC";
            opts.MysteryToolPath = "";
            opts.RGAPath = "rga\\rga.exe";
            opts.TempPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Pyramid" );

            return opts;
        }

        public static Options Get()
        {
            string OptionsFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Pyramid\\options" );

            Options defaults = GetDefaults();
            if (!System.IO.File.Exists(OptionsFile))
                return defaults;

            Options opts = new Options();
            try
            {
                string[] lines = File.ReadAllLines(OptionsFile);
                Dictionary<string,string> map = new Dictionary<string,string>();
                foreach( string s in lines )
                {
                    string[] keyval = s.Split('=');
                    string key = keyval[0];
                    string value = keyval[1];
                    map.Add(key, value);
                }
                
                string d3dCompiler;
                if (!map.TryGetValue("D3DCompiler", out d3dCompiler))
                    d3dCompiler = defaults.D3DCompilerPath;

                string codeXL;
                if (!map.TryGetValue("CodeXL", out codeXL))
                    codeXL = defaults.CodeXLPath;

                string temp;
                if (!map.TryGetValue("temp", out temp))
                    temp = defaults.TempPath;

                string pvr;
                if (!map.TryGetValue("PowerVR", out pvr))
                    pvr = defaults.PowerVRCompilerPath;

                string dxx;
                if (!map.TryGetValue("DXX", out dxx))
                    dxx = defaults.DXXDriverPath;

                string mali;
                if (!map.TryGetValue("Mali", out mali))
                    mali = defaults.MaliSCRoot;

                string mystery;
                if (!map.TryGetValue("MysteryTool", out mystery))
                    mystery = defaults.MysteryToolPath;


                string disabledBackends;
                if( map.TryGetValue("DisabledBackends", out disabledBackends))
                    opts.m_DisabledBackends.AddRange(disabledBackends.Split(','));

                string disabledAMDAsics;
                if (map.TryGetValue("DisabledAMDAsics", out disabledAMDAsics))
                    opts.m_DisabledAMDAsics.AddRange(disabledAMDAsics.Split(','));

                string disabledCodeXLAsics;
                if (map.TryGetValue("DisabledCodeXLAsics", out disabledCodeXLAsics))
                    opts.m_DisabledCodeXLAsics.AddRange(disabledCodeXLAsics.Split(','));

                string disabledRGAAsics;
                if (map.TryGetValue("DisabledRGAAsics", out disabledRGAAsics))
                    opts.m_DisabledRGAAsics.AddRange(disabledRGAAsics.Split(','));


                string rga;
                if (!map.TryGetValue("RGAPath", out rga))
                    rga = defaults.RGAPath;


                opts.D3DCompilerPath = d3dCompiler;
                opts.CodeXLPath = codeXL;
                opts.TempPath = temp;
                opts.PowerVRCompilerPath = pvr;
                opts.DXXDriverPath = dxx;
                opts.MaliSCRoot = mali;
                opts.MysteryToolPath = mystery;
                opts.RGAPath = rga;
                return opts;
            }
            catch (Exception e)
            {
                // not found
                MessageBox.Show(e.Message, "uh-oh, couldn't read options file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return GetDefaults();
            }
        }

        public void Write()
        {
            string OptionsFile = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "Pyramid\\options");

            try
            {
                string DisabledBackends = String.Join(",",m_DisabledBackends.ToArray());
                string DisabledAMDAsics = String.Join(",",m_DisabledAMDAsics.ToArray());
                string DisabledCodeXLAsics = String.Join(",", m_DisabledCodeXLAsics.ToArray());
                string DisabledRGAAsics = String.Join(",", m_DisabledRGAAsics.ToArray());

                File.WriteAllText(OptionsFile,
                                   String.Format("D3DCompiler={0}\nCodeXL={1}\ntemp={2}\nPowerVR={3}\nMali={4}\nDisabledBackends={5}\nDisabledAMDAsics={6}\nDisabledCodeXLAsics={7}\nMysteryTool={8}\nRGAPath={9}\nDisabledRGAAsics={10}\n",
                                                             D3DCompilerPath,
                                                             CodeXLPath,
                                                             TempPath,
                                                             PowerVRCompilerPath,
                                                             MaliSCRoot,
                                                             DisabledBackends,
                                                             DisabledAMDAsics,
                                                             DisabledCodeXLAsics,
                                                             MysteryToolPath,
                                                             RGAPath,
                                                             DisabledRGAAsics));
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "uh-oh, couldn't write options file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
