// 
//  Author:
//    Marek Habersack <grendel@twistedcode.net>
// 
//  Copyright (c) 2010, Marek Habersack
// 
//  All rights reserved.
// 
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of Marek Habersack nor the names of the contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;

using Mono.Cecil;
using Mono.Collections.Generic;

namespace StubGen
{
	public class Generator
	{
		static readonly Dictionary <string, string> license2resource = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase)
		{
			{"Apache2", "Apache2LicencePolicy.xml"},
			{"GPL2", "GPL2LicencePolicy.xml"},
			{"GPL3", "GPL3LicencePolicy.xml"},
			{"LGPL2", "LGPL3LicencePolicy.xml"},
			{"NewBSD", "NewBSDLicencePolicy.xml"},
			{"MIT", "MITX11LicencePolicy.xml"}
		};
		
		static readonly List <Macro> macros = new List<Macro>
		{
			{ new Macro { Name="FileName", Handler=MacroFileName } }, 
			{ new Macro { Name="FileNameWithoutExtension", Handler=MacroFileNameWithoutExtension } }, 
			{ new Macro { Name="Directory", Handler=MacroDirectory } }, 
			{ new Macro { Name="FullFileName", Handler=MacroFullFileName } },
			{ new Macro { Name="AuthorName", Handler=MacroAuthorName } },
			{ new Macro { Name="AuthorEmail", Handler=MacroAuthorEmail } },
			{ new Macro { Name="CopyrightHolder", Handler=MacroCopyrightHolder } },
			{ new Macro { Name="Year", Handler=MacroYear } },
		};
		
		public static readonly string NewLine = Environment.NewLine;
		static Dictionary <string, string> licenseCache = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		static string currentFilePath;
		static StubGenOptions sgopts;
		
		static string AssemblyPresent (string name)
		{
			// TODO: should probably call gacutil
			try {
				Assembly.Load (name);
				return "present";
			} catch {
				return "missing";
			}
		}
		
		public static void Run (string assemblyPath, StubGenOptions opts, string outDir)
		{
			sgopts = opts;
			string license = GetLicenseBlock (opts.LicenseName);
			bool overwrite = opts.OverwriteAll;
			
			ModuleDefinition module = ModuleDefinition.ReadModule (assemblyPath);
			if (module.HasAssemblyReferences) {
				var refs = new List <string> ();
				Console.WriteLine ("\tAssembly references:");
				foreach (AssemblyNameReference aref in module.AssemblyReferences)
					refs.Add (aref.FullName);
				
				refs.Sort (StringComparer.Ordinal);
				foreach (string s in refs)
					Console.WriteLine ("\t   [{0}] {1}", AssemblyPresent (s), s);
			}
			
			OutputAssemblyInfo (module, outDir, license, overwrite);
			foreach (TypeDefinition type in module.Types) {
				if (type.IsNotPublic)
					continue;
			
				try {
					currentFilePath = FilePathForType (outDir, type);
					if (!overwrite && File.Exists (currentFilePath)) {
						currentFilePath = null;
						continue;
					}
					
					using (FileStream fs = File.Open (currentFilePath, FileMode.Create, FileAccess.Write)) {
						using (var sw = new StreamWriter (fs, Encoding.UTF8)) {
							WriteHeader (sw, license);
							Outline.Run (currentFilePath, license, sw, type);
						}
					}
				} catch (Exception ex) {
					Console.WriteLine ("\tFailure. Unable to generate file for type {0}. {1}", type.FullName, ex.Message);
				}
			}
		}
		
		static void WriteHeader (StreamWriter writer, string license)
		{
			if (sgopts.NoHeader)
				return;
			writer.WriteLine (ReplaceMacros (license));
		}
		
		static void OutputAssemblyInfo (ModuleDefinition module, string outDir, string license, bool overwrite)
		{
			currentFilePath = Path.Combine (outDir, "Assembly");
			if (!Directory.Exists (currentFilePath))
				Directory.CreateDirectory (currentFilePath);
			currentFilePath = Path.Combine (currentFilePath, "AssemblyInfo.cs");
			if (!overwrite && File.Exists (currentFilePath))
				return;	
			
			// TODO: output TypeForwarded{From,To}
			using (FileStream fs = File.OpenWrite (currentFilePath)) {
				using (var sw = new StreamWriter (fs, Encoding.UTF8)) {
					WriteHeader (sw, license);
					var sb = new StringBuilder ();
					var usings = new List <string> () {
						{"System"},
						{"System.Reflection"}
					};
					if (module.Assembly.HasCustomAttributes)
						OutputAssemblyCustomAttributes (sb, usings, module);
					
					if (module.Assembly.HasSecurityDeclarations)
						OutputAssemblySecurityDeclarations (sb, usings, module);
					
					usings.Sort ();
					foreach (string u in usings)
						sw.WriteLine ("using {0};", u);
					sw.WriteLine ();
					sw.WriteLine (sb.ToString ());
				}
			}
		}
		
		static void OutputAssemblyCustomAttributes (StringBuilder sb, List <string> usings, ModuleDefinition module)
		{
			Collection <CustomAttribute> attrs = module.Assembly.CustomAttributes;
			if (attrs == null || attrs.Count == 0)
				return;
			
			string str;
			TypeReference type;
			bool first;
			foreach (CustomAttribute attr in attrs) {
				type = attr.AttributeType;
				if (type.FullName == "System.Runtime.CompilerServices.ReferenceAssemblyAttribute")
					continue;
				
				str = type.Namespace;
				usings.AddUsing (str);
				
				str = type.Name;
				if (str.EndsWith ("Attribute", StringComparison.Ordinal))
					str = str.Substring (0, str.Length - 9);
				
				sb.AppendFormat ("[assembly: {0}", str);
				first = true;
				bool needParen = false;
				if (attr.HasConstructorArguments) {
					needParen = true;
					sb.Append (" (");
					foreach (CustomAttributeArgument arg in attr.ConstructorArguments) {
						if (!first)
							sb.Append (", ");
						else
							first = false;
						
						sb.Append (Utils.FormatValue (arg.Value, arg.Type, usings));
					}
				}
		
				if (attr.HasFields) {
					if (!needParen) {
						needParen = true;
						sb.Append (" (");
					}
					foreach (Mono.Cecil.CustomAttributeNamedArgument arg in attr.Fields) {
						if (!first)
							sb.Append (", ");
						else
							first = false;
						sb.AppendFormat ("{0}={1}", arg.Name, Utils.FormatValue (arg.Argument.Value, arg.Argument.Type, usings));
					}
				}
				
				if (attr.HasProperties) {
					if (!needParen) {
						needParen = true;
						sb.Append (" (");
					}
					foreach (Mono.Cecil.CustomAttributeNamedArgument arg in attr.Properties) {
						if (!first)
							sb.Append (", ");
						else
							first = false;
						sb.AppendFormat ("{0}={1}", arg.Name, Utils.FormatValue (arg.Argument.Value, arg.Argument.Type, usings));
					}
				}
				
				if (needParen)
					sb.Append (")");
				sb.AppendLine ("]");
			}
		}
		
		static void OutputAssemblySecurityDeclarations (StringBuilder sb, List <string> usings, ModuleDefinition module)
		{
			// TODO: implement
		}
		
		static string FilePathForType (string outDir, TypeDefinition type)
		{
			string dir = Path.Combine (outDir, type.Namespace);
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			return Path.Combine (dir, type.Name + ".cs");
		}
		
		static string GetLicenseBlock (string name)
		{
			if (String.IsNullOrEmpty (name))
				return null;
		
			string str;
			if (licenseCache.TryGetValue (name, out str))
				return str;
			
			if (license2resource.TryGetValue (name, out str)) {
				try {
					Assembly asm = Assembly.GetExecutingAssembly ();
					Stream st = asm.GetManifestResourceStream ("StubGen.resources." + str);
					if (st == null) {
						Console.Error.Write ("\tNo resource for license '{0}'", name);
						return null;
					}
				
					byte[] bytes = new byte [st.Length];
					st.Read (bytes, 0, (int)st.Length);
					str = ExtractTextFromXML (name, Encoding.UTF8.GetString (bytes));
					bytes = null;
				} catch (Exception ex) {
					Console.Error.WriteLine ("\tFailed to get resource for license '{0}'. {1}", name, ex.Message);
					return null;
				}
			} else {
				if (!File.Exists (name)) {
					Console.Error.Write ("\tLicense file '{0}' not found.", name);
					return null;
				}
				
				str = File.ReadAllText (name);
			}
			
			if (String.IsNullOrEmpty (str)) {
				licenseCache.Add (name, null);
				return null;
			}
			
			string[] lines = str.Split ('\n');
			var sb = new StringBuilder ();
			
			foreach (string l in lines)
				sb.Append ("// " + l + NewLine);
			
			str = sb.ToString ();
			licenseCache.Add (name, str);
			return str;
		}
		
		static string ReplaceMacros (string input)
		{
			foreach (Macro m in macros)
				input = input.Replace ("${" + m.Name + "}", m.Handler ());
			
			return input;
		}
		
		static string MacroFileName ()
		{
			if (String.IsNullOrEmpty (currentFilePath))
				return null;
			return Path.GetFileName (currentFilePath);
		}
		
		static string MacroFileNameWithoutExtension ()
		{
			if (String.IsNullOrEmpty (currentFilePath))
				return null;
			return Path.GetFileNameWithoutExtension (currentFilePath);
		}
		
		static string MacroDirectory ()
		{
			if (String.IsNullOrEmpty (currentFilePath))
				return null;
			return Path.GetDirectoryName (currentFilePath);
		}
		
		static string MacroFullFileName ()
		{
			if (String.IsNullOrEmpty (currentFilePath))
				return null;
			return currentFilePath;
		}
		
		static string MacroAuthorName ()
		{
			if (sgopts == null)
				return null;
			
			return sgopts.AuthorName;
		}
		
		static string MacroAuthorEmail ()
		{
			if (sgopts == null)
				return null;
			
			return sgopts.AuthorEmail;
		}
		
		static string MacroCopyrightHolder ()
		{
			if (sgopts == null)
				return null;
			
			return sgopts.CopyrightHolder;
		}
		
		static string MacroYear ()
		{
			return DateTime.Now.Year.ToString ();
		}
		
		static string ExtractTextFromXML (string name, string input)
		{
			try {
				var doc = new XmlDocument ();
				doc.LoadXml (input);
				
				XmlNode node = doc.SelectSingleNode ("StandardHeader");
				if (node == null)
					return null;
				
				XmlAttribute attr = node.Attributes ["Text"];
				if (attr == null)
					return null;
				
				return attr.Value;
			} catch (Exception ex) {
				Console.Error.WriteLine ("\tFailed to parse license '{0}' XML. {1}", name, ex.Message);
			}
			
			return null;
		}
	}
}

