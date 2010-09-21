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
		static readonly List <string> accessorPrefixes = new List<string> {
			"get_",
			"set_",
			"add_",
			"remove_"
		};
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
		
		public static string FormatName (MetadataType type)
		{
			switch (type) {
				case MetadataType.Boolean:
					return "bool";
					
				case MetadataType.Char:
					return "char";
						
				case MetadataType.SByte:
					return "sbyte";
					
				case MetadataType.Byte:
					return "byte";
					
				case MetadataType.Int16:
					return "short";
					
				case MetadataType.UInt16:
					return "ushort";
					
				case MetadataType.Int32:
					return "int";
						
				case MetadataType.UInt32:
					return "uint";
						
				case MetadataType.Int64:
					return "long";
					
				case MetadataType.UInt64:
					return "ulong";
					
				case MetadataType.Single:
					return "float";
					
				case MetadataType.Double:
					return "double";
					
				case MetadataType.String:
					return "string";
			
				default:
					return type.ToString ();
			}
		}
		
		public static string FormatGenericTypeName (string name)
		{
			if (String.IsNullOrEmpty (name))
				return String.Empty;
			
			int idx = name.IndexOf ('`');
			if (idx >= 0)
				name = name.Substring (0, idx);
			
			return name;
		}
		
		public static string FormatName (TypeReference type)
		{
			if (type == null)
				return String.Empty;

			if (type.IsGenericParameter)
				return FormatName (type as GenericParameter);
			
			if (type.IsArray)
				return FormatName (type as ArrayType);
			
			if (type.IsPrimitive)
				return FormatName (type.MetadataType);
			
			switch (type.FullName) {
				case "System.String":
					return "string";
					
				case "System.Object":
					return "object";
					
				case "System.Void":
					return "void";
			}
			
			TypeDefinition tdef = ResolveType (type);
			if (tdef == null)
				return type.Name;
			
			string name = FormatGenericTypeName (tdef.Name);
			var sb = new StringBuilder ();
			
			// TODO: handle nullable types gracefully (to get rid of Nullable <type> in favor of type?)
			bool nullable = type.FullName.StartsWith ("System.Nullable`1", StringComparison.Ordinal);
			if (type.IsPointer)
				sb.Append (FormatName (type as PointerType));
			else
				sb.Append (name);
			
			bool first = true;
			
			if (type.IsGenericInstance) {
				GenericInstanceType git = type as GenericInstanceType;
				sb.Append (" <");
				foreach (TypeReference gtype in git.GenericArguments) {
					if (!first)
						sb.Append (", ");
					else
						first = false;

					sb.Append (FormatName (gtype));
				}
				sb.Append (">");
			} else if (tdef.HasGenericParameters) {
				sb.Append (" <");
				Collection <GenericParameter> gpcoll = tdef.GenericParameters;
				
				foreach (GenericParameter gp in gpcoll) {
					if (!first)
						sb.Append (", ");
					else
						first = false;

					sb.Append (FormatName (gp));
				}
				
				sb.Append (">");
			}
			
			return sb.ToString ();
		}
		
		public static string FormatName (ArrayType type)
		{
			// TODO: add support for multi-dimensional arrays
			// TODO: add support for size spec
			return FormatName (type.ElementType) + "[]";
		}
		
		public static string FormatName (PointerType type)
		{
			return "unsafe " + FormatName (type.ElementType) + "*";
		}
		
		public static string FormatCustomAttributes (Collection <CustomAttribute> attributes)
		{
			if (attributes == null || attributes.Count == 0)
				return String.Empty;
		
			var sb = new StringBuilder ();
			foreach (CustomAttribute attr in attributes) {
				if (attr.AttributeType.FullName == "System.ParamArrayAttribute")
					continue;
				sb.AppendIndent ("[");
				sb.Append (FormatName (attr));
				sb.AppendLine ("]");
			}
			
			return sb.ToString ();
		}

		public static string FormatCustomAttributesOneLine (Collection <CustomAttribute> attributes)
		{
			if (attributes == null || attributes.Count == 0)
				return String.Empty;
		
			bool first = true;
			var sb = new StringBuilder ();
			foreach (CustomAttribute attr in attributes) {
				if (attr.AttributeType.FullName == "System.ParamArrayAttribute")
					continue;
				if (!first)
					sb.Append (", ");
				else {
					sb.Append ("[");
					first = false;
				}
				sb.Append (FormatName (attr));
			}
			if (sb.Length > 0)
				sb.Append ("]");
			
			if (sb.Length == 0)
				return String.Empty;
			
			return sb.ToString ();
		}
		
		public static string FormatName (GenericParameter gp)
		{
			var sb = new StringBuilder ();
			if (gp.HasCustomAttributes) {
				sb.Append (FormatCustomAttributesOneLine (gp.CustomAttributes));
				sb.Append (' ');
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
		
			if (field.HasCustomAttributes)
				FormatCustomAttributes (field.CustomAttributes);
			
			
			var sb = new StringBuilder ();
			sb.AppendIndent (FormatAttributes (field));
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
		
		public static string FormatAttributes (FieldDefinition field)
		{
			if (field == null)
				return String.Empty;
			
			var attrs = new List <string> ();
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
			
			if (attrs.Count == 0)
				return String.Empty;
			
			return String.Join (" ", attrs.ToArray ()) + " ";
		}
		
		public static string FormatAttributes (MethodDefinition method)
		{
			if (method == null || method.DeclaringType.IsInterface)
				return String.Empty;
			
			var attrs = new List <string> ();
			if (method.IsPublic)
				attrs.Add ("public");
			else if (method.IsFamily)
				attrs.Add ("protected");
			else if (method.IsFamilyOrAssembly)
				attrs.Add ("protected internal");
			else if (method.IsAssembly)
				attrs.Add ("internal");
			
			if (method.IsFinal) {
				if (!method.IsVirtual)
					attrs.Add ("sealed");
			} if (method.IsStatic)
				attrs.Add ("static");
			if (method.IsAbstract)
				attrs.Add ("abstract");
			else if (method.IsVirtual) {
				if (method.IsNewSlot)
					attrs.Add ("virtual");
				else
					attrs.Add ("override");
			} else if (method.IsNewSlot)
				attrs.Add ("new");
			
			if (attrs.Count == 0)
				return String.Empty;
			
			return String.Join (" ", attrs.ToArray ()) + " ";
		}
		
		public static string FormatAccessor (string name, MethodDefinition accessor, bool needsAttributes)
		{
			if (accessor == null || String.IsNullOrEmpty (name))
				return String.Empty;
			
			var sb = new StringBuilder ();
			Indent++;
			try {
				sb.AppendLineIndent ();
				if (accessor.HasCustomAttributes)
					sb.Append (FormatCustomAttributes (accessor.CustomAttributes));
				if (needsAttributes) {
					sb.AppendIndent (FormatAttributes (accessor));
					sb.Append (' ');
				} else
					sb.AppendIndent ();
				
				sb.Append (name);
				if (accessor.IsAbstract || accessor.DeclaringType.IsInterface)
					sb.Append (";");
				else
					sb.AppendLine (" { throw new NotImplementedException (); }");
			} finally {
				Indent--;
			}
			
			return sb.ToString ();
		}
		
		static void GatherAccessorInfo (MethodDefinition first, MethodDefinition second, out MethodDefinition moreVisible, out ushort moreVisibleAccessMask,
			out ushort firstAccessMask, out ushort secondAccessMask)
		{
			moreVisible = first != null ? first : second;
			
			if (first != null && second != null) {
				if ((first.Attributes & MethodAttributes.MemberAccessMask) != (second.Attributes & MethodAttributes.MemberAccessMask)) {
					if (first.IsPublic)
						moreVisible = first;
					else if (second.IsPublic)
						moreVisible = second;
					else if (first.IsFamilyOrAssembly)
						moreVisible = first;
					else if (second.IsFamilyOrAssembly)
						moreVisible = second;
					else if ((first.IsAssembly || first.IsFamily))
						moreVisible = first;
					else if ((second.IsAssembly || second.IsAssembly))
						moreVisible = second;
				}
			}
			
			moreVisibleAccessMask = (ushort) (moreVisible.Attributes & MethodAttributes.MemberAccessMask);
			firstAccessMask = first != null ? (ushort) (first.Attributes & MethodAttributes.MemberAccessMask) : (ushort)0;
			secondAccessMask = second != null ? (ushort) (second.Attributes & MethodAttributes.MemberAccessMask) : (ushort)0;
		}
		
		public static string FormatName (EventDefinition ev)
		{
			if (ev == null)
				return String.Empty;
			
			var sb = new StringBuilder ();
			if (ev.HasCustomAttributes)
				sb.Append (FormatCustomAttributes (ev.CustomAttributes));
			
			MethodDefinition adder = ev.AddMethod;
			MethodDefinition remover = ev.RemoveMethod;
			MethodDefinition accessor = null;
			ushort adderAccessMask;
			ushort removerAccessMask;
			ushort evAccessMask;
			
			GatherAccessorInfo (adder, remover, out accessor, out evAccessMask, out adderAccessMask, out removerAccessMask);
			
			sb.AppendIndent (FormatAttributes (accessor));
			sb.Append ("event ");
			sb.Append (FormatName (ev.EventType));
			sb.Append (' ');
			sb.Append (ev.Name);
			sb.Append (' ');
			sb.Append ("{");
			
			sb.Append (FormatAccessor ("add", adder, adderAccessMask != evAccessMask));
			sb.Append (FormatAccessor ("remove", remover, removerAccessMask != evAccessMask));
			
			if (ev.DeclaringType.IsInterface)
				sb.AppendLine ();
			
			sb.AppendLineIndent ("}");
			sb.AppendLine ();
			
			return sb.ToString ();
		}
		
		public static string FormatName (PropertyDefinition prop)
		{
			if (prop == null)
				return String.Empty;
		
			var sb = new StringBuilder ();
			if (prop.HasCustomAttributes)
				sb.Append (FormatCustomAttributes (prop.CustomAttributes));
			
			MethodDefinition getter = prop.GetMethod;
			MethodDefinition setter = prop.SetMethod;
			MethodDefinition accessor = null;
			ushort getterAccessMask;
			ushort setterAccessMask;
			ushort propAccessMask;
				
			GatherAccessorInfo (getter, setter, out accessor, out propAccessMask, out getterAccessMask, out setterAccessMask);
			
			sb.AppendIndent (FormatAttributes (accessor));
			sb.Append (FormatName (prop.PropertyType));
			sb.Append (' ');
			
			if (prop.HasParameters) {
				sb.Append ("this [");
				bool first = true;
				foreach (ParameterDefinition p in prop.Parameters) {
					if (!first)
						sb.Append (", ");
					else
						first = false;
					
					sb.Append (FormatName (p));
				}
				sb.Append ("]");
			} else if (IsExplicitImplementation (accessor)) {
				TypeReference iface;
				MethodReference ifaceMethod;
				
				GetInfoForExplicitlyImplementedMethod (accessor, out iface, out ifaceMethod);
				if (iface != null) {
					sb.Append (iface.Name);
					sb.Append ('.');
					
					string name = prop.Name;
					string iname = iface.FullName + ".";
					if (name.StartsWith (iname, StringComparison.OrdinalIgnoreCase))
						sb.Append (name.Substring (iname.Length));
					else
						sb.Append (name);
				} else
					sb.Append (prop.Name);
			} else
				sb.Append (prop.Name);
			
			sb.Append (' ');
			sb.Append ("{");
			
			sb.Append (FormatAccessor ("get", getter, getterAccessMask != propAccessMask));
			sb.Append (FormatAccessor ("set", setter, setterAccessMask != propAccessMask));
			if (prop.DeclaringType.IsInterface)
				sb.AppendLine ();
			
			sb.AppendLineIndent ("}");
			sb.AppendLine ();
			
			return sb.ToString ();
		}
		
		static string TranslateSpecialName (MethodDefinition method)
		{
			string name = method != null ? method.Name : String.Empty;
			if (String.IsNullOrEmpty (name) || !name.StartsWith ("op_"))
				return name;
			
			string ret;
			switch (name) {
				case "op_Explicit":
				case "op_Implicit":
					return name.Substring (3).ToLower () + " operator " + FormatName (method.ReturnType);
					
				case "op_UnaryPlus": 
					ret = "+";
					break;
					
				case "op_UnaryNegation": 
					ret = "-";
					break;
					
				case "op_LogicalNot": 
					ret = "!";
					break;
					
				case "op_OnesComplement": 
					ret = "~";
					break;
					
				case "op_Increment": 
					ret = "++";
					break;
					
				case "op_Decrement": 
					ret = "--";
					break;
					
				case "op_True": 
					ret = "true";
					break;
					
				case "op_False": 
					ret = "false";
					break;
					
				case "op_Addition": 
					ret = "+";
					break;
					
				case "op_Subtraction": 
					ret = "-";
					break;
					
				case "op_Multiply": 
					ret = "*";
					break;
					
				case "op_Division": 
					ret = "/";
					break;
					
				case "op_Modulus": 
					ret = "%";
					break;
					
				case "op_BitwiseAnd": 
					ret = "&";
					break;
					
				case "op_BitwiseOr": 
					ret = "|";
					break;
					
				case "op_ExclusiveOr": 
					ret = "^";
					break;
					
				case "op_LeftShift": 
					ret = "<<";
					break;
					
				case "op_RightShift": 
					ret = ">>";
					break;
					
				case "op_Equality": 
					ret = "==";
					break;
					
				case "op_Inequality": 
					ret = "!=";
					break;
					
				case "op_GreaterThan": 
					ret = ">";
					break;
					
				case "op_LessThan": 
					ret = "<";
					break;
					
				case "op_GreaterThanOrEqual": 
					ret = ">=";
					break;
					
				case "op_LessThanOrEqual": 
					ret = "<=";
					break;
					
				default:
					ret = name;
					break;
			}
			
			return FormatName (method.ReturnType) + " operator " + ret;
		}
		
		static bool IsAccessor (MethodDefinition method)
		{
			string name = method != null ? method.Name : null;
			if (String.IsNullOrEmpty (name))
				return false;
			
			if (IsExplicitImplementation (method)) {
				TypeReference iface;
				MethodReference ifaceMethod;
				
				GetInfoForExplicitlyImplementedMethod (method, out iface, out ifaceMethod);
				name = ifaceMethod.Name;
			}
			foreach (string prefix in accessorPrefixes)
				if (name.StartsWith (prefix, StringComparison.OrdinalIgnoreCase))
					return true;
			
			return false;
		}
		
		static bool IsExplicitImplementation (MethodDefinition method)
		{
			if (method == null)
				return false;
			
			return method.IsPrivate && method.IsFinal && method.IsVirtual;
		}
		
		public static void GetInfoForExplicitlyImplementedMethod (MethodDefinition method, out TypeReference iface, out MethodReference ifaceMethod)
		{
			iface = null;
			ifaceMethod = null;
			if (method.Overrides.Count != 1)
				Console.WriteLine ("\tCould not determine interface type for explicitly-implemented interface member " + method.FullName);
			else {
				iface = method.Overrides [0].DeclaringType;
				ifaceMethod = method.Overrides [0];
			}
		}

		public static string FormatName (MethodDefinition method)
		{
			if (method == null)
				return String.Empty;
			
			string name = method.Name;
			var sb = new StringBuilder ();
			if (method.HasCustomAttributes)
				sb.Append (FormatCustomAttributes (method.CustomAttributes));
			sb.AppendIndent (FormatAttributes (method));
			if (!method.IsSpecialName) {
				sb.Append (FormatName (method.ReturnType));
				sb.Append (' ');
			} else if (IsAccessor (method))
				return String.Empty;
			
			if (method.IsConstructor)
				sb.Append (FormatGenericTypeName (method.DeclaringType.Name));
			else if (name == "Finalize")
				sb.Append ("~" + FormatGenericTypeName (method.DeclaringType.Name));
			else if (method.IsSpecialName)
				sb.Append (TranslateSpecialName (method));
			else if (IsExplicitImplementation (method)) {
				TypeReference iface;
				MethodReference ifaceMethod;
				
				GetInfoForExplicitlyImplementedMethod (method, out iface, out ifaceMethod);
				if (iface != null) {
					sb.Append (FormatName (iface));
					sb.Append ('.');
					sb.Append (ifaceMethod.Name);
				} else
					sb.Append (method.Name);
			} else
				sb.Append (name);
			
			bool first = true;
			if (method.HasGenericParameters) {
				sb.Append ('<');
				foreach (GenericParameter gp in method.GenericParameters) {
					if (!first)
						sb.Append (", ");
					else
						first = false;
					sb.Append (FormatName (gp));
				}
				sb.Append ("> ");
			}
			
			sb.Append (" (");
			if (method.HasParameters) {
				first = true;
				foreach (ParameterDefinition p in method.Parameters) {
					if (!first)
						sb.Append (", ");
					else
						first = false;
					sb.Append (FormatName (p));
				}
			}
			sb.Append (")");
			if (method.IsAbstract || method.DeclaringType.IsInterface)
				sb.AppendLine (";");
			else {
				sb.AppendLine ();
				sb.AppendLineIndent ("{");
				try {
					Indent++;
					sb.AppendLineIndent ("throw new NotImplementedException ();");
				} finally {
					Indent--;
				}
				sb.AppendLineIndent ("}");
			}
			sb.AppendLine ();
			
			return sb.ToString ();
		}
		
		public static string FormatName (ParameterDefinition parameter)
		{
			if (parameter == null)
				return String.Empty;
			
			var sb = new StringBuilder ();
			bool isParamArray = false;
			if (parameter.HasCustomAttributes) {
				if (parameter.CustomAttributes.First (attr => {
					if (attr.AttributeType.FullName == "System.ParamArrayAttribute")
						return true;
					return false;
				}) != null)
					isParamArray = true;
				if (isParamArray && parameter.CustomAttributes.Count > 1) {
					sb.Append (FormatCustomAttributesOneLine (parameter.CustomAttributes));
					sb.Append (' ');
				}
			}
			
			if (isParamArray)
				sb.Append ("params ");
			sb.Append (FormatName (parameter.ParameterType));
			sb.Append (' ');
			sb.Append (parameter.Name);
			
			return sb.ToString ();
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

