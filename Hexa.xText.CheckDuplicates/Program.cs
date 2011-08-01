#region License

//===================================================================================
//Copyright 2010 HexaSystems Corporation
//===================================================================================
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//http://www.apache.org/licenses/LICENSE-2.0
//===================================================================================
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//===================================================================================

#endregion

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace Hexa.xText.CheckDuplicates
{
    /// <summary>
    /// Small appplication that verifies repeated lines in the translations file
    /// </summary>    
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Declare a fileDialog to get the file location
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Check the user click ok in the dialog
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Create a new thread to make the operation
                Thread newThread = new Thread(Program.CheckDuplicates);
                newThread.Start(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Check for repeated lines in the translation file
        /// </summary>
        /// <param name="file">The file name</param>
        public static void CheckDuplicates(object file)
        {
            int count = 0;
            List<string> tags = new List<string>();
            List<string> repeatedTags = new List<string>();
            string line = "";

            // Create the object to open the file
            FileStream fs = File.Open(file.ToString(), FileMode.Open);

            // Create the object to read the file content
            StreamReader reader = new StreamReader(fs);
            
            // Iterate through the file content
            while ((line = reader.ReadLine()) != null)
            {
                count++;

                // Check that the current line its a msgid
                if (line.Contains("msgid"))
                {
                    // Get the line
                    string theLine = line.Replace("msgid", "").Replace("\"", "").Trim();

                    // If the tag is not already added
                    if (!tags.Contains(theLine))
                    {
                        // Add to the collection
                        tags.Add(theLine);
                    }
                    else
                    {
                        repeatedTags.Add(theLine);

                        // Show the message for repated line to the user
                        Console.WriteLine("Duplicated msgid on line: " + count.ToString() + ", Tag: " + theLine + "\n");
                    }
                }
            }

            reader.Close();
            fs.Close();

            if (repeatedTags.Count > 0)
            {
                Console.WriteLine("Total Duplicated msgid's: " + repeatedTags.Count + "\n");
            }
            else
            {
                Console.WriteLine("PO File is Ok!!!\n");
            }

            Console.WriteLine("Duplicated msgid's Validation is Complete");
            Console.ReadLine();
        }
    }
}
