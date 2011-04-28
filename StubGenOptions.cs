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
using System.IO;

namespace StubGen
{
	public class StubGenOptions
	{
		string authorName;
		string authorEmail;
		string copyrightHolder;
		
		public bool ShowHelp { get; set; }
		public bool NoHeader { get; set; }
		public string LicenseName { get; set; }
		public string OutputDir { get; set; }
		public bool OverwriteAll { get; set; }
		public bool Debug { get; set; }
		public bool IncludeNonPublic { get; set; }
		
		public string AuthorName { 
			get {
				if (authorName == null)
					authorName = GetMacroValue ("AuthorName", "STUBGEN_AUTHOR_NAME");
				
				return authorName;
			}
			
			set { authorName = value; }
		}
		
		public string AuthorEmail { 
			get {
				if (authorEmail == null)
					authorEmail = GetMacroValue ("AuthorEmail", "EMAIL", "STUBGEN_AUTHOR_EMAIL");
				
				return authorEmail;
			}
			
			set { authorEmail = value; }
		}
		
		public string CopyrightHolder { 
			get {
				if (copyrightHolder == null)
					copyrightHolder = GetMacroValue ("CopyrightHolder", "STUBGEN_COPYRIGHT_HOLDER", "STUBGEN_AUTHOR_NAME");
				
				return copyrightHolder;
			}
			
			set { copyrightHolder = value; }
		}
		
		public StubGenOptions ()
		{
			LicenseName = "MIT";
			OutputDir = Directory.GetCurrentDirectory ();
		}
		
		string GetMacroValue (string macroName, params string[] envVars)
		{
			string ret = null;
			
			if (envVars != null && envVars.Length > 0) {
				foreach (string name in envVars) {
					ret = Environment.GetEnvironmentVariable (name);
					if (!String.IsNullOrEmpty (ret))
						break;
				}
			}
			
			if (String.IsNullOrEmpty (ret)) {
				Console.Write ("\tPlease enter value for the {0} macro and press ENTER: ", macroName);
				ret = Console.ReadLine ();
			}
			
			if (String.IsNullOrEmpty (ret))
				return String.Empty;
			
			return ret;
		}
	}
}