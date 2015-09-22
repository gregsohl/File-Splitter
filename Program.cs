﻿/*
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace FileSplitter {
    static class Program {
		
		private static Int32 EXIT_CODE_OK= 0;
		private static Int32 EXIT_CODE_FAIL= 1;
		
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static String lastFile = "";

        private static void printHelp() {
            Console.WriteLine("Usage:");
            Console.WriteLine("fsplit -split <size> <unit> <filePath>");
            Console.WriteLine();
            Console.WriteLine("Parameter help:");
            Console.WriteLine();
            Console.WriteLine("-split");
            Console.WriteLine("  size        Size of parts");
            Console.WriteLine("                If size unit is 'l' defines number of lines");
            Console.WriteLine("                in other case the size in the selected unit");
            Console.WriteLine();
            Console.WriteLine("  unit        unit of size 'b' 'kb 'mb' 'gb' 'l'");
            Console.WriteLine("                b  - bytes");
            Console.WriteLine("                kb - Kilobytes");
            Console.WriteLine("                mb - megabytes");
            Console.WriteLine("                gb - gigabytes");
            Console.WriteLine("                l  - lines (based on endline detection)");
            Console.WriteLine();
            Console.WriteLine("  filePath    Path of the file to be split");
            Console.WriteLine();
        }
   
        private static void setConsoleWindowVisibility(bool visible) {
            IntPtr hWnd = FindWindow(null, Console.Title);
            if (hWnd != IntPtr.Zero) {
                if (!visible) {
                    //Hide the window                    
                    ShowWindow(hWnd, 0); // 0 = SW_HIDE                
                } else{
                    //Show window again                    
                    ShowWindow(hWnd, 1); //1 = SW_SHOWNORMA       
                }
            }
        }

        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main(String[] args) {

            // args:
            // 0 - action
            // 1 - split size
            // 2 - split units
            // 3 - file
            Console.Title = Application.ProductName +  " " + Application.ProductVersion + " Console Window";

             if (args != null && args.Length > 1) {
                String command = args[0];

                if (command.Equals("-split")) {
                    if (args.Length < 4) {
                        Console.WriteLine("Missing parameter");
                        printHelp();
                        Environment.Exit(1);  // return an ErrorLevel in case it is processed in a Batch file
                    } else {

                        OPERATION_SPIT mode = OPERATION_SPIT.BY_BYTE;

                        // check size
                        Int64 size = 0;
                        try {
                            size = Convert.ToInt64(args[1]);
                        } catch {
                            Console.WriteLine("Invalid size");
                            printHelp();
                            Environment.Exit(EXIT_CODE_FAIL);
                        }

                        // check units
                        if (args[2].ToLower() == "b") {
                            mode = OPERATION_SPIT.BY_BYTE;
                        } else if (args[2].ToLower() == "kb") {
                            mode = OPERATION_SPIT.BY_KBYTE;
                        } else if (args[2].ToLower() == "mb") {
                            mode = OPERATION_SPIT.BY_MBYTE;
                        } else if (args[2].ToLower() == "gb") {
                            mode = OPERATION_SPIT.BY_GBYTE;
                        } else if (args[2].ToLower() == "l") {
                            mode = OPERATION_SPIT.BY_LINES;
                        } else {
                            Console.WriteLine("Invalid size unit");
                            printHelp();
                            Environment.Exit(EXIT_CODE_FAIL);
                        }

                        size = Utils.unitConverter(size, mode);
                            
                        // check file exists
                        String fileName = args[3];
                        if (File.Exists(fileName)) {
                            FileSplitter fs = new FileSplitter();
                            fs.start += new FileSplitter.StartHandler(fs_splitStart);
                            fs.finish += new FileSplitter.FinishHandler(fs_splitEnd);
                            fs.processing += new FileSplitter.ProcessHandler(fs_splitProcess);
                            fs.message += new FileSplitter.MessageHandler(fs_message);
                            fs.FileName = fileName;
                            fs.PartSize = size;
                            fs.OperationMode = mode;
                            fs.doSplit();
                            Environment.Exit(EXIT_CODE_OK);       // return an ErrorLevel indicating successful launch
                        } else {
                            Console.WriteLine("File does not exist");
                            printHelp();
                            Environment.Exit(EXIT_CODE_FAIL);
                        }
                    }

                    /* }  TODO else if (command.Equals("-join")) {*/

                } else {
                    Console.WriteLine("Unrecognized Command");
                    printHelp();
                    Environment.Exit(EXIT_CODE_FAIL);
                }

            } else {
                setConsoleWindowVisibility(false);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FrmSplitter());
                Environment.Exit(EXIT_CODE_OK);     // although there's not much point - the console window is no longer visible.  Does it need to be closed?
            }
            
        }


        static void fs_message(object server, MessageArgs args){
            Console.WriteLine(Utils.getMessageText(args.Message, args.Parameters));
        }

        static void fs_splitStart(){
            Console.WriteLine("Starting splitting operation");
        }
        
        static void fs_splitProcess(object sender, ProcessingArgs args) {
            if (lastFile != args.FileName) {
                lastFile = args.FileName;
                Console.WriteLine("Writing " + lastFile);
            } 
            
        }

        static void fs_splitEnd() {
            Console.WriteLine("Done!");
        }

     }
}
