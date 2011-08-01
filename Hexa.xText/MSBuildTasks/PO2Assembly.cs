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

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Tasks;

namespace Hexa.xText.MSBuildTasks
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class PO2Assembly : Task
	{

		#region Properties

		/// <summary>
		/// Gets or sets the PO path.
		/// </summary>
		/// <value>The PO path.</value>
		public ITaskItem[] POFiles { get; set; }

		/// <summary>
		/// Gets or sets the output path.
		/// </summary>
		/// <value>The output path.</value>
		public string OutputPath { get; set; }

		/// <summary>
		/// Gets or sets the gettext assembly path.
		/// </summary>
		/// <value>The gettext path.</value>
		public string GNUGetTextAssemblyPath { get; set; }

		/// <summary>
		/// Gets or sets the name of the assembly without the extension.
		/// </summary>
		/// <value>The name of the assembly.</value>
		public string AssemblyName { get; set; }

		#endregion Properties

		#region Methods

		/// <summary>
		/// Executes a task.
		/// </summary>
		/// <returns>
		/// true if the task executed successfully; otherwise, false.
		/// </returns>
		public override bool Execute()
		{
			if (POFiles != null)
			{
				foreach (ITaskItem item in POFiles)
				{
					// Verify that POFile exists.
					if (!File.Exists(item.ItemSpec))
					{
						_LogMessage(string.Format(CultureInfo.InvariantCulture, "File {0} does not exists.", item.ItemSpec));
						return false;
					}

					// Get file info from file.
					FileInfo fileInfo = new FileInfo(item.ItemSpec);

					// Assume that FileName is in the format: locale.extension
					// Get locale from FileName.
					string locale = fileInfo.Name.Replace(fileInfo.Extension, string.Empty);

					string outputPath = OutputPath + "\\" + locale;

					// If OutputPath directory does not exist, create it.
					if (!Directory.Exists(outputPath))
						Directory.CreateDirectory(outputPath);

					// Create the output assembly name.
					string assemblyFileName = Path.Combine(outputPath, AssemblyName + ".resources.dll");

					// Verify if assemblyFile exists
					if (File.Exists(assemblyFileName))
					{
						//FileInfo dllInfo = new FileInfo(assemblyFileName);
						//if (dllInfo.LastWriteTime.CompareTo(fileInfo.LastWriteTime) > 0)
						//    continue; // Continue to next item cause Assembly is newer than current po file.
						//else
						File.Delete(assemblyFileName);
					}

					//Get the temporary path to store the C# classes.
					string csOutputPath = System.IO.Path.GetTempPath();

					// Get the C# file template from embedded resources.
					string template = Resource.template;

					// Get the file name for the C# class
					string csFileName = Path.Combine(csOutputPath, string.Format(CultureInfo.InvariantCulture, "{0}.{1}", AssemblyName, "cs"));

					// Get the class name for the C# class
					string className = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", ConstructClassName(AssemblyName), locale.Replace("-", "_"));

					// String builder to hold the key value pairs retrieved from the po file.
					StringBuilder s = new StringBuilder();
					using (FileStream fileStream = new FileStream(item.ItemSpec, FileMode.Open))
					{
						// Read the po file.
						_ReadPOFile(s, new StreamReader(fileStream, Encoding.UTF8));
					}

					// Get bytes for the new C# class
					byte[] bytes = Encoding.UTF8.GetBytes(template.Replace("{0}", className).Replace("{1}", s.ToString()));

					// Write the C# class to disk.
					using (FileStream csStream = new FileStream(csFileName, FileMode.Create))
					{
						csStream.Write(bytes, 0, bytes.Length);
					}

					_LogMessage(string.Format(CultureInfo.InvariantCulture, "Created file {0}", csFileName));

					// Log if GNU.Gettext.dll not found.
					if (!File.Exists(GNUGetTextAssemblyPath))
					{
						_LogMessage(string.Format(CultureInfo.InvariantCulture, "Unable to find dependency file: {0}", GNUGetTextAssemblyPath));
						return false;
					}

                    var fileinfo = new FileInfo(GNUGetTextAssemblyPath);

					// Compile c# class.
					Csc csc = new Csc();
					csc.HostObject = this.HostObject;
					csc.BuildEngine = this.BuildEngine;
                    csc.AdditionalLibPaths = new string[] { fileinfo.Directory.FullName };
					csc.TargetType = "library";
                    csc.References = new TaskItem[] { new TaskItem(fileinfo.Name) };
					csc.OutputAssembly = new TaskItem(assemblyFileName);
					csc.Sources = new TaskItem[] { new TaskItem(csFileName) };
					csc.Execute();

					_LogMessage(string.Format(CultureInfo.InvariantCulture, "Created assembly {0}", assemblyFileName));
				}

				return true;
			}
			return true;
		}

		/// <summary>
		/// Reads a file stream and fills the string builder s.
		/// </summary>
		/// <param name="s">The s.</param>
		/// <param name="fileStream">The file stream.</param>
		private static void _ReadPOFile(StringBuilder s, StreamReader fileStream)
		{
            var file = fileStream.ReadToEnd();

            Regex exp = new Regex(@"/*msgid\s*\x22(?<text>(.|[\r\n])*?)\x22");

            List<string> msgids = exp.Matches(file)
                .Cast<Match>()
                .Select(m => m.Groups["text"].Value)
                .ToList();

            exp = new Regex(@"/*msgstr\s*\x22(?<text>(.|[\r\n])*?)\x22");

            List<string> msgstrs = exp.Matches(file)
                .Cast<Match>()
                .Select(m => m.Groups["text"].Value)
                .ToList();

            for (int idx = 0; idx < msgids.Count(); idx++)
            {
                if (!string.IsNullOrEmpty(msgids[idx]) && !string.IsNullOrEmpty(msgstrs[idx]))
                    s.Append(string.Format(CultureInfo.InvariantCulture, "t.Add(\"{0}\",\"{1}\");", msgids[idx], msgstrs[idx])).Append("\n");
            }
		}

		/// <summary>
		/// Logs the message.
		/// </summary>
		/// <param name="message">The message.</param>
		private void _LogMessage(string message)
		{
			if (!System.Diagnostics.Debugger.IsAttached)
			{
				BuildMessageEventArgs args = new BuildMessageEventArgs(message, string.Empty, "PO2Assembly", MessageImportance.Normal);
				BuildEngine.LogMessageEvent(args);
			}
		}

		/// <summary>
		/// Converts a resource name to a class name.
		/// </summary>
		/// <returns>a nonempty string consisting of alphanumerics and underscores
		///          and starting with a letter or underscore</returns>
		private static String ConstructClassName(String resourceName)
		{
			// We could just return an arbitrary fixed class name, like "Messages",
			// assuming that every assembly will only ever contain one
			// GettextResourceSet subclass, but this assumption would break the day
			// we want to support multi-domain PO files in the same format...
			bool valid = (resourceName.Length > 0);
			for (int i = 0; valid && i < resourceName.Length; i++)
			{
				char c = resourceName[i];
				if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c == '_')
					  || (i > 0 && c >= '0' && c <= '9')))
					valid = false;
			}
			if (valid)
				return resourceName;
			else
			{
				// Use hexadecimal escapes, using the underscore as escape character.
				String hexdigit = "0123456789abcdef";
				StringBuilder b = new StringBuilder();
				b.Append("__UESCAPED__");
				for (int i = 0; i < resourceName.Length; i++)
				{
					char c = resourceName[i];
					if (c >= 0xd800 && c < 0xdc00
						&& i + 1 < resourceName.Length
						&& resourceName[i + 1] >= 0xdc00 && resourceName[i + 1] < 0xe000)
					{
						// Combine two UTF-16 words to a character.
						char c2 = resourceName[i + 1];
						int uc = 0x10000 + ((c - 0xd800) << 10) + (c2 - 0xdc00);
						b.Append('_');
						b.Append('U');
						b.Append(hexdigit[(uc >> 28) & 0x0f]);
						b.Append(hexdigit[(uc >> 24) & 0x0f]);
						b.Append(hexdigit[(uc >> 20) & 0x0f]);
						b.Append(hexdigit[(uc >> 16) & 0x0f]);
						b.Append(hexdigit[(uc >> 12) & 0x0f]);
						b.Append(hexdigit[(uc >> 8) & 0x0f]);
						b.Append(hexdigit[(uc >> 4) & 0x0f]);
						b.Append(hexdigit[uc & 0x0f]);
						i++;
					}
					else if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')
								 || (c >= '0' && c <= '9')))
					{
						int uc = c;
						b.Append('_');
						b.Append('u');
						b.Append(hexdigit[(uc >> 12) & 0x0f]);
						b.Append(hexdigit[(uc >> 8) & 0x0f]);
						b.Append(hexdigit[(uc >> 4) & 0x0f]);
						b.Append(hexdigit[uc & 0x0f]);
					}
					else
						b.Append(c);
				}
				return b.ToString();
			}
		}

		#endregion

	}
}
