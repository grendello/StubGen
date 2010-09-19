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
using System.Text;

using Mono.Cecil;

namespace StubGen
{
	public class Outline
	{
		public static void Run (string filePath, string license, StreamWriter writer, TypeDefinition type)
		{
			string indent = "\t";
			var usings = new List <string> ();
			var sb = new StringBuilder ();
			
			usings.AddUsing ("System");
			WriteType (sb, indent, usings, type);
			
			if (usings.Count > 0) {
				usings.Sort ();
			
				foreach (string u in usings)
					writer.WriteLine ("using {0};", u);
				writer.WriteLine ();
			}
			writer.WriteLine ("namespace {0}", type.Namespace);
			writer.WriteLine ("{");
			
			writer.Write (sb.ToString ());
			
			writer.WriteLine ("}");
		}
		
		static void WriteType (StringBuilder sb, string indent, List <string> usings, TypeDefinition type)
		{
			Action <StringBuilder, string, List <string>, TypeDefinition> typeWriter = null;
			
			sb.Append (indent);
			
			// TODO: format custom and security attributes
			FormatTypeAttributes (sb, type);
			if (type.IsEnum) {
				sb.Append ("enum");
			} else if (type.IsClass) {
				sb.Append ("class");
			} else if (type.IsInterface) {
				sb.Append ("interface");
			} else if (type.IsValueType) {
				if (type.FullName == "System.Delegate" || type.FullName == "System.MulticastDelegate")
					sb.Append ("delegate");
				else
					sb.Append ("struct");
			}
			
			// TODO: format generic type parameters
			sb.AppendFormat (" {0}", type.Name);
			
			bool haveColon = false;
			bool first = true;
			TypeReference tref = type.BaseType;
			if (WritableBaseType (tref)) {
				sb.Append (" : ");
				haveColon = true;
				first = false;
				
				usings.AddUsing (tref.Namespace);
				sb.Append (Utils.FormatGenericTypeName (tref));
			}
			
			if (type.HasInterfaces) {
				if (!haveColon)
					sb.Append (" : ");
				foreach (TypeReference i in type.Interfaces) {
					if (first)
						first = false;
					else
						sb.Append (", ");
					
					usings.AddUsing (i.Namespace);
					sb.Append (Utils.FormatGenericTypeName (i));
				}
			}
			sb.AppendLine ();
			sb.AppendFormat ("{0}{{{1}", indent, Generator.NewLine);
			
			if (typeWriter != null)
				typeWriter (sb, indent + "\t", usings, type);
			
			sb.AppendFormat ("{0}}}{1}", indent, Generator.NewLine);
		}
		
		static void FormatTypeAttributes (StringBuilder sb, TypeDefinition type)
		{
			var attrs = new List <string> ();
			
			if (type.IsPublic || type.IsNestedPublic)
				attrs.Add ("public");
			else if (type.IsNestedFamily || type.IsNestedFamilyAndAssembly || type.IsNestedFamilyOrAssembly || type.IsNestedAssembly)
				attrs.Add ("protected");
			else
				attrs.Add ("internal");
			
			if (type.IsSealed && !type.IsEnum) {
				if (!type.IsAbstract)
					attrs.Add ("sealed");
				else
					attrs.Add ("static");
			}
			
			if (type.IsAbstract && !type.IsInterface && !type.IsSealed)
				attrs.Add ("abstract");
			
			if (attrs.Count > 0) {
				sb.Append (String.Join (" ", attrs.ToArray ()));
				sb.Append (' ');
			}
		}
		
		static bool WritableBaseType (TypeReference type)
		{
			if (type == null)
				return false;
			
			switch (type.FullName) {
				case "System.Object":
				case "System.Enum":
					return false;
					
				default:
					return true;
			}
		}
	}
}

