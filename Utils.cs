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
using System.Linq;
using System.Text;

using Mono.Cecil;
using Mono.Collections.Generic;

namespace StubGen
{
	public class Utils
	{
		static string indentString = String.Empty;
		static ulong oldIndent = 0;
		static List <string> usings;
		
		public static ulong Indent = 0;
		
		public static string IndentString {
			get {
				if (oldIndent != Utils.Indent) {
					oldIndent = Indent;
					if (Indent == 0)
						indentString = String.Empty;
					else
						indentString = new String ('\t', (int)Indent);
				}
				
				return indentString;
			}
		}
		
		public static List <string> Usings {
			get {
				if (usings == null)
					usings = new List<string> ();
				
				return usings;
			}
		}
		
		public static TypeDefinition ResolveType (TypeReference type)
		{
			if (type == null)
				return null;
			
			TypeDefinition tdef;
			try {
				tdef = type.Resolve ();
			} catch {
				tdef = null;
			}
			
			if (tdef == null)
				Console.WriteLine ("\tUnresolved type '{0}'", type.FullName);
				
			return tdef;
		}
		
		public static string FormatName (TypeReference type)
		{
			if (type == null)
				return String.Empty;
			
			TypeDefinition tdef = ResolveType (type);
			if (tdef == null)
				return type.Name;
			
			if (tdef.HasGenericParameters) {
				Collection <GenericParameter> gpcoll = tdef.GenericParameters;
				string name = tdef.Name;
				int idx = name.IndexOf ('`');
				if (idx >= 0)
					name = name.Substring (0, idx);
				
				var sb = new StringBuilder (name);
				sb.Append (" <");
				
				bool first = true;
				foreach (GenericParameter gp in gpcoll) {
					if (!first)
						sb.Append (", ");
					else
						first = false;

					sb.Append (FormatName (gp));
				}
				
				sb.Append (">");
				return sb.ToString ();
			}
			
			return tdef.Name;
		}
		
		public static string FormatCustomAttributes (Collection <CustomAttribute> attributes)
		{
			if (attributes == null || attributes.Count == 0)
				return String.Empty;
		
			var sb = new StringBuilder ();
			foreach (CustomAttribute attr in attributes) {
				sb.AppendIndent ("[");
				sb.Append (FormatName (attr));
				sb.AppendLine ("]");
			}
			
			return sb.ToString ();
		}
		
		public static string FormatName (GenericParameter gp)
		{
			var sb = new StringBuilder ();
			if (gp.HasCustomAttributes) {
				sb.Append ("[");
				bool first = true;
				foreach (CustomAttribute attr in gp.CustomAttributes) {
					if (!first)
						sb.Append (", ");
					else
						first = false;
					
					sb.Append (FormatName (attr));
				}
				sb.Append ("] ");
			}	
			sb.Append (gp.Name);
			
			return sb.ToString ();
		}
		
		public static string FormatName (CustomAttribute attr)
		{
			if (attr == null)
				return String.Empty;
			
			TypeReference type = attr.AttributeType;
			string str = type.Namespace;
			Usings.AddUsing (str);
			
			var sb = new StringBuilder ();	
			str = type.Name;
			if (str.EndsWith ("Attribute", StringComparison.Ordinal))
				str = str.Substring (0, str.Length - 9);
			
			sb.Append (str);
			bool first = true;
			bool needParen = false;
			if (attr.HasConstructorArguments) {
				needParen = true;
				sb.Append (" (");
				foreach (Mono.Cecil.CustomAttributeArgument arg in attr.ConstructorArguments) {
					if (!first)
						sb.Append (", ");
					else
						first = false;
					sb.Append (FormatValue (arg.Value, arg.Type, usings));
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
					sb.AppendFormat ("{0}={1}", arg.Name, FormatValue (arg.Argument.Value, arg.Argument.Type, usings));
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
					sb.AppendFormat ("{0}={1}", arg.Name, FormatValue (arg.Argument.Value, arg.Argument.Type, usings));
				}
			}
				
			if (needParen)
				sb.Append (")");
			
			return sb.ToString ();
		}
		
		public static string FormatName (FieldDefinition field)
		{
			if (field == null)
				return String.Empty;
			
			var attrs = new List <string> ();
			var sb = new StringBuilder ();
			sb.AppendIndent ();
			if (field.IsPublic)
				attrs.Add ("public");
			else if (field.IsFamily)
				attrs.Add ("protected");
			else if (field.IsFamilyOrAssembly)
				attrs.Add ("protected internal");
			else if (field.IsAssembly)
				attrs.Add ("internal");
			
			if (field.IsStatic)
				attrs.Add ("static");
			
			if (field.IsInitOnly)
				attrs.Add ("readonly");
			
			if (field.IsLiteral)
				attrs.Add ("const");
			
			sb.Append (String.Join (" ", attrs.ToArray ()));
			sb.Append (' ');
			sb.Append (FormatName (field.FieldType));
			sb.Append (' ');
			sb.Append (field.Name);
			
			if (field.HasConstant) {
				sb.Append (" = ");
				sb.Append (FormatValue (field.Constant, field.FieldType));
			}
					
			sb.AppendLine (";");
			
			return sb.ToString ();
		}
		
		public static string FormatValue (object v, TypeReference type)
		{
			return FormatValue (v, type, Usings);
		}
		
		public static string FormatValue (object v, TypeReference type, List <string> usings)
		{	
			if (v == null)
				return "null";
			
			string ns = type.Namespace;
			usings.AddUsing (ns);
			
			TypeDefinition tdef = ResolveType (type);
			
			string ret;
			if (tdef != null && tdef.IsEnum && tdef.HasFields) {
				Collection <CustomAttribute> attrs = tdef.CustomAttributes;
				CustomAttribute flagsAttr = attrs == null || attrs.Count == 0 ? null : attrs.First ((CustomAttribute attr) => {
					if (attr.AttributeType.FullName == "System.FlagsAttribute")
						return true;
					
					return false;
				});
				bool flags = flagsAttr != null;
				ns = tdef.Namespace;
				usings.AddUsing (ns);
				
				string typeName = tdef.Name.Replace ("/", ".");
				var sb = new StringBuilder ();
				bool first = true;
				foreach (FieldDefinition fd in tdef.Fields) {
					if (flags) {
						// TODO: handle masks like AttributeUsage.All - the mask should not be used. If a value has more than one byte set
						// it shall not be output
						if (IsBitSet (v, fd.Constant)) {
							if (first)
								first = false;
							else
								sb.Append (" | ");
							sb.AppendFormat ("{0}.{1}", typeName, fd.Name);
						}
					} else if (v.Equals (fd.Constant)) {
						sb.AppendFormat ("{0}.{1}", typeName, fd.Name);
						break;
					}
				}
				
				return sb.ToString ();
			}
			
			ret = v.ToString ();
			if (v is string)
				return "\"" + ret + "\"";
			
			if (v is bool)
				return ret.ToLower ();
			
			return ret;
		}
		
		static bool IsBitSet (object left, object right)
		{
			if (left == null || right == null)
				return false;
			
			Type ltype = left.GetType ();
			Type rtype = right.GetType ();
			if (ltype != rtype)
				return false;
			
			switch (Type.GetTypeCode (ltype)) {
				case TypeCode.Byte:
					return (((byte)left) & ((byte)right)) != 0;
					
				case TypeCode.SByte:
					return (((sbyte)left) & ((sbyte)right)) != 0;
				
				case TypeCode.Int16:
					return (((short)left) & ((short)right)) != 0;
					
				case TypeCode.Int32:
					return (((int)left) & ((int)right)) != 0;
					
				case TypeCode.Int64:
					return (((long)left) & ((long)right)) != 0;
					
				case TypeCode.UInt16:
					return (((ushort)left) & ((ushort)right)) != 0;
					
				case TypeCode.UInt32:
					return (((uint)left) & ((uint)right)) != 0;
					
				case TypeCode.UInt64:
					return (((ulong)left) & ((ulong)right)) != 0;
					
				default:
					return false;
			}
		}
	}
}

