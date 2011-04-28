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
using System.IO;

using Mono.Options;

namespace StubGen
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var sgopts = new StubGenOptions ();
			var opts = new OptionSet () {
				{ "o=|output-dir=", "Directory to generate classes in. Defaults to current directory", v => sgopts.OutputDir = v },
				{ "no-header", "Do not put header with license in generated files", v => sgopts.NoHeader = true },
				{ "l|license=", "Name of the license or path to a text file with license text (defaults to MIT/X11)", v => sgopts.LicenseName = v },
				{ "a|author=", "Author name", v => sgopts.AuthorName = v },
				{ "e|email=", "Author email", v => sgopts.AuthorEmail = v },
				{ "c|copyright=", "Copyright holder", v => sgopts.CopyrightHolder = v },
				{ "f|force|overwrite-all", "Overwrite all files without prompting.", v => sgopts.OverwriteAll = true },
				{ "d|debug", "Show more information on errors.", v => sgopts.Debug = true },
				{ "p|private|non-public", "Include private/internal members in the outline.", v => sgopts.IncludeNonPublic = true },
				{ "h|help|?", "Show this help screen", v => sgopts.ShowHelp = true }
			};
			
			if (sgopts.ShowHelp)
				ShowHelp (opts);
			
			List <string> assemblies = opts.Parse (args);
			if (assemblies == null || assemblies.Count == 0)
				ShowHelp (opts);
			
			foreach (string ap in assemblies)
				ProcessAssembly (ap, sgopts);
		}
		
		static void ProcessAssembly (string path, StubGenOptions opts)
		{
			string aname = Path.GetFileNameWithoutExtension (path);
			Console.Error.WriteLine ("Processing assembly {0}", aname);
			string outdir = Path.Combine (opts.OutputDir, aname);
			
			try {
				if (!Directory.Exists (outdir))
					Directory.CreateDirectory (outdir);
				Generator.Run (path, opts, outdir);
			} catch (Exception ex) {
				Console.Error.WriteLine ("\tFailure. {0}", ex.Message);
				if (opts.Debug) {
					Console.Error.WriteLine (ex.StackTrace);
					Console.Error.WriteLine ();
				}
			}
		}
		
		static void ShowHelp (OptionSet opts)
		{
			Console.WriteLine ("Usage: stubgen [OPTIONS] assembly_path [assembly_path ...]");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			opts.WriteOptionDescriptions (Console.Out);
			Environment.Exit (0);
		}
	}
}
