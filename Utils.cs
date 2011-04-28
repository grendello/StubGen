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
using Mono.Cecil.Metadata;
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
		
		public static string FormatValue (object v, TypeReference type)
		{
			return FormatValue (v, type, Usings);
		}
		
		static int CountBits (object constant)
		{
			if (constant == null)
				return -1;
			
			switch (Type.GetTypeCode (constant.GetType ())) {
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
					break;
					
				default:
					return -1;
			}
			
			ulong value = Convert.ToUInt64 (constant);
			int nbits;
			for (nbits = 0; value > 0; nbits++)
				value &= value - 1;
			
			return nbits;
		}
		
		public static string FormatValue (object v, TypeReference type, List <string> usings)
		{	
			if (v == null)
				return "null";
			
			string ns = type.Namespace;
			usings.AddUsing (ns);
			
			TypeDefinition tdef = type.ResolveType ();
			
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
				
				string typeName = tdef.FormatName (); //tdef.Name.Replace ("/", ".");
				var sb = new StringBuilder ();
				bool first = true;
				object constant;
				int bits;
				foreach (FieldDefinition fd in tdef.Fields) {
					if (flags) {
						constant = fd.Constant;
						bits = CountBits (constant);
						if (bits < 0 || bits > 1)
							continue;
						
						if (IsBitSet (v, constant)) {
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
				return "\"" + ret.Replace ("\\", "\\\\") + "\"";
			
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

