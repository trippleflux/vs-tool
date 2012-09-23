﻿using System;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Linq; 
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace pythonemcc
{
    class Program
    {
        /// <summary>
        /// This program is a small tool to workaround issues that Visual Studio/MSBuild
        /// has when it tries to invoke 'python emcc' directly. In particular, VS uses
        /// this concept of 'response files', which is a file that stores the command line
        /// input parameters for a process. This application is called by vs-tool to
        /// route commands from VS to python emcc.
        /// 
        /// Ideally, this tool should never exist. If someone can find an alternative way,
        /// I'll be happy to hear it.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length == 0)
                return;

            List<string> emccargs = new List<string>();
            string firstparam = args[0];
            if (firstparam.StartsWith("@")) // Is this a reference to a .rsp file?
            {
                firstparam = firstparam.Substring(1);
                string s = "";
                using (StreamReader rdr = File.OpenText(firstparam))
                    s = rdr.ReadToEnd();
                emccargs.Add(s);
            }
            else // No .rsp file, pass the arguments directly
                emccargs.AddRange(args);
//          Console.WriteLine("Invoking emcc with cmdline: " + s);
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.FileName = "python";
            // We assume that the 'emcc' python executable and this application reside in the
            // same directory.
            // http://stackoverflow.com/questions/837488/how-can-i-get-the-applications-path-in-net-in-a-console-app
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directory = System.IO.Path.GetDirectoryName(path);

            var toolname = System.IO.Path.GetFileNameWithoutExtension(path);
            string a = directory + "\\" + toolname;
            foreach(string s in emccargs)
                a += " " + s;

            psi.Arguments = a;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            Process p = Process.Start(psi);
            p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            p.ErrorDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            while (!p.HasExited)
            {
                p.WaitForExit(10000);
                if (!p.HasExited)
                    Console.WriteLine(toolname + " running.. please wait.");
            }
        }

        static void p_OutputDataReceived(object sender, DataReceivedEventArgs line)
        {
            if (!String.IsNullOrEmpty(line.Data))
                Console.WriteLine(line.Data.Trim());
        }
    }
}