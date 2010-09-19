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
using System.Text;

namespace StubGen
{
	public static class MiscExtensions
	{
		public static void AddUsing (this List <string> usings, string ns)
		{
			if (usings == null || String.IsNullOrEmpty (ns))
				return;
			
			if (!usings.Contains (ns))
				usings.Add (ns);
		}
		
		public static StringBuilder AppendFormatIndent (this StringBuilder sb, string format, params object[] args)
		{
			if (sb == null)
				return null;
			
			sb.Append (Utils.IndentString);
			sb.AppendFormat (format, args);
			
			return sb;
		}
		
		public static StringBuilder AppendIndent (this StringBuilder sb)
		{
			if (sb == null)
				return null;
			
			sb.Append (Utils.IndentString);
			return sb;
		}
		
		public static StringBuilder AppendIndent (this StringBuilder sb, string str)
		{
			if (sb == null)
				return null;
			
			sb.Append (Utils.IndentString);
			sb.Append (str);
			return sb;
		}
		
		public static StringBuilder AppendLineIndent (this StringBuilder sb)
		{
			if (sb == null)
				return null;
			
			sb.Append (Utils.IndentString);
			sb.AppendLine ();
			
			return sb;
		}
		
		public static StringBuilder AppendLineIndent (this StringBuilder sb, string str)
		{
			if (sb == null)
				return null;
			
			sb.Append (Utils.IndentString);
			sb.AppendLine (str);
			
			return sb;
		}
	}
}

