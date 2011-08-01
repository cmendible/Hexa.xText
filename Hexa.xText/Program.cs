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
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Hexa.xText
{
    // xgettext like extraction program. 
	sealed class Program
	{
		static void Main(string[] args)
		{
			//Get starting folder
            string function = args[0];
	        string startFolder = args[1];
            string language = args[2];
            string[] patterns = new string[] { "*.cs", "*.aspx", "*.ascx", "*.Master" };

            if (!Directory.Exists(startFolder + "\\POs\\"))
                Directory.CreateDirectory(startFolder + "\\POs\\");

            string poFileName = startFolder + "\\POs\\" + language  + ".po";

            string[] folders2Exclude = new string[] { };

            if (args.Length > 3)
                folders2Exclude = args[3].Split(',');

            ExtractStringsFromFolder(startFolder, poFileName, patterns, folders2Exclude, function);
		}

        static void ExtractStringsFromFolder(string folder, string poFileName, string[] patterns, string[] folders2Exclude, string function)
        {
            Console.Write(folder);

            var dirInfo = new DirectoryInfo(folder);

            foreach (var pattern in patterns)
            {
                var poExists = File.Exists(poFileName);
                foreach (var file in dirInfo.GetFiles(pattern, SearchOption.AllDirectories))
                {
                    if (!folders2Exclude.Contains(file.DirectoryName))
                    {
                        Console.Write(file);
                        ExtractStringsFromFile(file.FullName, poFileName, poExists, function);
                    }
                }
            }
        }

        static void ExtractStringsFromFile(string fileName, string poFileName, bool append, string function)
		{
			IList<string> extractedStrings = null;
            IList<string> originalStrings = new List<string>();

			string originalPO = string.Empty;

			using (StreamReader fileStream = new StreamReader(fileName, true))
			{
                extractedStrings = _ExtractStrings(fileStream.ReadToEnd(), function);
			}

			FileInfo sourceFileInfo = new FileInfo(fileName);
			if (append)
			{
				using (StreamReader fileStream = new StreamReader(poFileName, true))
				{
					originalPO = fileStream.ReadToEnd();

                    Regex exp = new Regex(@"/*msgid\s*\x22(?<text>(.|[\r\n])*?)\x22");

                    originalStrings = exp.Matches(originalPO)
                    .Cast<Match>()
                    .Select(m => m.Groups["text"].Value)
                    .ToList();
				}
			}

			StringBuilder outputBuilder = new StringBuilder();
			foreach (string extracted in extractedStrings.Distinct())
			{
				if (!originalStrings.Contains(extracted))
					outputBuilder.Append(_CreatePOFormat(sourceFileInfo.Name, extracted));
			}

			if (outputBuilder.Length > 0)
			{
				byte[] bytes = Encoding.UTF8.GetBytes(outputBuilder.ToString());
				using (FileStream poStream = _CreateFileStream(poFileName, append))
				{
					poStream.Write(bytes, 0, bytes.Length);
				}
			}
		}

        /// <summary>
        /// Extract strings.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        private static IList<string> _ExtractStrings(string line, string function)
        {
            // <%$ Code: t("Press Enter to Search") %>
            // <%= t("Press Enter to Search") %>
            Regex exp = new Regex(string.Format(@"/*{0}\x28\x22(?<text>(.|[\r\n])*?)\x22\x29", function)); 

            var lines = exp.Matches(line)
                .Cast<Match>()
                .Select(m => m.Groups["text"].Value)
                .ToList();

            List<string> result = new List<string>();
            exp = new Regex(@"(?<!\w)\x22\s*[\r\n]*\+\s*[\r\n]*\x22");

            foreach (var l in lines)
            {
                result.Add(exp.Replace(l, string.Empty));
            };

            return result;
        }

        /// <summary>
        /// Create PO format.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="extractedString">The extracted string.</param>
        /// <returns></returns>
        private static string _CreatePOFormat(string fileName, string extractedString)
        {
            return string.Format(CultureInfo.InvariantCulture, "#: {0}\r\nmsgid \"{1}\"\r\nmsgstr \"\"\r\n\r\n", fileName, extractedString);
        }

        /// <summary>
        /// Create a file stream.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="append">if set to <c>true</c> [append].</param>
        /// <returns></returns>
        private static FileStream _CreateFileStream(string fileName, bool append)
        {
            if (!append)
                return new FileStream(fileName, FileMode.Create);
            else
            {
                if (!File.Exists(fileName))
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "File: {0} does not exists. Can't append data to it.", fileName));

                return new FileStream(fileName, FileMode.Append);
            }
        }

	}

}