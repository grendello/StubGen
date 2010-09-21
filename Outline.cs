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
			string ns = type.Namespace;
			
			if (String.IsNullOrEmpty (ns))
				ns = null;
			
			if (ns != null)
				Utils.Indent++;
			try {
				List <string> usings = Utils.Usings;
				usings.Clear ();
				var sb = new StringBuilder ();
				
				usings.AddUsing ("System");
				WriteType (sb, usings, type);
				
				if (usings.Count > 0) {
					usings.Sort ();
					
					foreach (string u in usings)
						writer.WriteLine ("using {0};", u);
					writer.WriteLine ();
				}
				if (ns != null) {
					writer.WriteLine ("namespace {0}", ns);
					writer.WriteLine ("{");
				}
				
				writer.Write (sb.ToString ());
			
				if (ns != null)
					writer.WriteLine ("}");
			} finally {
				if (ns != null)
					Utils.Indent--;
			}
		}
		
		static void WriteType (StringBuilder sb, List <string> usings, TypeDefinition type)
		{
			Action <StringBuilder, List <string>, TypeDefinition> typeWriter = null;
			
			// TODO: security attributes
			if (type.HasCustomAttributes)
				sb.Append (Utils.FormatCustomAttributes (type.CustomAttributes));
			sb.AppendIndent ();
			
			FormatTypeAttributes (sb, type);
			if (type.IsEnum) {
				sb.Append ("enum");
				typeWriter = EnumWriter;
			} else if (type.IsClass) {
				sb.Append ("class");
				typeWriter = ClassWriter;
			} else if (type.IsInterface) {
				sb.Append ("interface");
				typeWriter = InterfaceWriter;
			} else if (type.IsValueType) {
				if (type.FullName == "System.Delegate" || type.FullName == "System.MulticastDelegate")
					sb.Append ("delegate");
				else
					sb.Append ("struct");
			}
			
			sb.AppendFormat (" {0}", Utils.FormatName (type));
			
			bool haveColon = false;
			bool first = true;
			TypeReference tref = type.BaseType;
			if (WritableBaseType (tref)) {
				sb.Append (" : ");
				haveColon = true;
				first = false;
				
				usings.AddUsing (tref.Namespace);
				sb.Append (Utils.FormatName (tref));
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
					sb.Append (Utils.FormatName (i));
				}
			}
			
			// TODO: output generic parameter constraints
			
			sb.AppendLine ();
			sb.AppendLineIndent ("{");
			
			if (typeWriter != null) {
				Utils.Indent++;
				try {
					typeWriter (sb, usings, type);
				} finally {
					Utils.Indent--;
				}
			}
			
			sb.AppendLineIndent ("}");
		}
		
		static void InterfaceWriter (StringBuilder sb, List <string> usings, TypeDefinition type)
		{
			// TODO: process nested types
			
			if (type.HasFields) {
				foreach (FieldDefinition field in type.Fields)
					sb.Append (Utils.FormatName (field));
			}
			
			// TODO: process events
			
			if (type.HasProperties) {
				foreach (PropertyDefinition prop in type.Properties)
					sb.Append (Utils.FormatName (prop));
			}
			
			if (type.HasMethods) {
				foreach (MethodDefinition method in type.Methods)
					sb.Append (Utils.FormatName (method));
			}
		}
		
		static void ClassWriter (StringBuilder sb, List <string> usings, TypeDefinition type)
		{
			// TODO: process nested types
			
			if (type.HasFields) {
				foreach (FieldDefinition field in type.Fields)
					sb.Append (Utils.FormatName (field));
			}
			
			// TODO: process events
			
			if (type.HasProperties) {
				foreach (PropertyDefinition prop in type.Properties)
					sb.Append (Utils.FormatName (prop));
			}
			
			if (type.HasMethods) {
				foreach (MethodDefinition method in type.Methods)
					sb.Append (Utils.FormatName (method));
			}
		}
		
		static void EnumWriter (StringBuilder sb, List <string> usings, TypeDefinition type)
		{
			if (!type.HasFields)
				return;
			
			int count = type.Fields.Count;
			foreach (FieldDefinition field in type.Fields) {
				count--;
				if (!field.HasConstant)
					continue;
				sb.AppendFormatIndent ("{0} = {1}", field.Name, field.Constant);
				if (count > 0)
					sb.Append (",");
				
				sb.AppendLine ();
			}
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

