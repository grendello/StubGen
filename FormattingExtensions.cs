// 
//  Author:
//    Marek Habersack <grendel@twistedcode.net>
// 
//  Copyright (c) 2010-2011, Marek Habersack
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
	public static class FormattingExtensions
	{
		static Dictionary <string, bool> ignored_attributes = new Dictionary<string, bool> (StringComparer.Ordinal) {
			{ "System.Runtime.TargetedPatchingOptOutAttribute", true },
			{ "System.ParamArrayAttribute", true },
			{ "System.Runtime.CompilerServices.CompilerGeneratedAttribute", true }
		};
		
		public static string FormatName (this MetadataType type)
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
		
		public static string FormatName (this TypeReference type)
		{
			if (type == null)
				return String.Empty;
			
			string ns = type.Namespace;
			if (!String.IsNullOrEmpty (ns))
				Utils.Usings.AddUsing (ns);
			
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
			
			TypeDefinition tdef = type.ResolveType ();
			if (tdef == null)
				return type.GetTypeName ();
			
			string name = FormatGenericTypeName (tdef.GetTypeName ());
			var sb = new StringBuilder ();
			bool nullable = type.FullName.StartsWith ("System.Nullable`1", StringComparison.Ordinal);
			if (!nullable) {
				if (type.IsPointer)
					sb.Append (FormatName (type as PointerType));
				else
					sb.Append (name);
			}
			
			bool first = true;
			
			if (type.IsGenericInstance) {
				GenericInstanceType git = type as GenericInstanceType;
				if (nullable) {
					TypeReference gtype = git.GenericArguments.FirstOrDefault ();
					if (gtype != null)
						sb.AppendFormat ("{0}?", FormatName (gtype));
				} else {
					sb.Append (" <");
					foreach (TypeReference gtype in git.GenericArguments) {
						if (!first)
							sb.Append (", ");
						else
							first = false;

						sb.Append (FormatName (gtype));
					}
					sb.Append (">");
				}
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
		
		public static string FormatName (this ArrayType type)
		{
			if (type == null)
				return String.Empty;
			
			// TODO: add support for multi-dimensional arrays
			// TODO: add support for size spec
			return FormatName (type.ElementType) + "[]";
		}
		
		public static string FormatName (this PointerType type)
		{
			if (type == null)
				return String.Empty;
			
			return "unsafe " + FormatName (type.ElementType) + "*";
		}
		
		public static string Format (this TargetRuntime runtime)
		{
			switch (runtime) {
			case TargetRuntime.Net_1_0:
				return "1.0";
			case TargetRuntime.Net_1_1:
				return "1.1";
			case TargetRuntime.Net_2_0:
				return "2.0/3.5";
			case TargetRuntime.Net_4_0:
				return "4.0";
			default:
				return runtime.ToString ();
			}
		}
		
		public static string Format (this Collection <CustomAttribute> attributes)
		{
			if (attributes == null || attributes.Count == 0)
				return String.Empty;
		
			var sb = new StringBuilder ();
			foreach (CustomAttribute attr in attributes) {
				if (ignored_attributes.ContainsKey (attr.AttributeType.FullName))
					continue;
				sb.AppendIndent ("[");
				sb.Append (FormatName (attr));
				sb.AppendLine ("]");
			}
			
			return sb.ToString ();
		}

		public static string FormatCustomAttributesOneLine (this Collection <CustomAttribute> attributes)
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
		
		public static string FormatName (this GenericParameter gp)
		{
			if (gp == null)
				return String.Empty;
			
			var sb = new StringBuilder ();
			if (gp.HasCustomAttributes) {
				sb.Append (FormatCustomAttributesOneLine (gp.CustomAttributes));
				sb.Append (' ');
			}
			
			sb.Append (gp.Name);
			
			return sb.ToString ();
		}
		
		public static string FormatName (this CustomAttribute attr)
		{
			if (attr == null)
				return String.Empty;
			
			TypeReference type = attr.AttributeType;
			string str = type.Namespace;
			Utils.Usings.AddUsing (str);
			
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
					sb.Append (Utils.FormatValue (arg.Value, arg.Type, Utils.Usings));
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
					sb.AppendFormat ("{0}={1}", arg.Name, Utils.FormatValue (arg.Argument.Value, arg.Argument.Type, Utils.Usings));
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
					sb.AppendFormat ("{0}={1}", arg.Name, Utils.FormatValue (arg.Argument.Value, arg.Argument.Type, Utils.Usings));
				}
			}
				
			if (needParen)
				sb.Append (")");
			
			return sb.ToString ();
		}
		
		public static string FormatName (this FieldDefinition field)
		{
			if (field == null)
				return String.Empty;
		
			if (field.HasCustomAttributes)
				field.CustomAttributes.Format ();
			
			
			var sb = new StringBuilder ();
			sb.AppendIndent (FormatAttributes (field));
			sb.Append (' ');
			sb.Append (FormatName (field.FieldType));
			sb.Append (' ');
			sb.Append (field.Name);
			
			if (field.HasConstant) {
				sb.Append (" = ");
				sb.Append (Utils.FormatValue (field.Constant, field.FieldType));
			}
					
			sb.AppendLine (";");
			
			return sb.ToString ();
		}
		
		public static string FormatAttributes (this FieldDefinition field)
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
		
		public static string FormatAttributes (this MethodDefinition method)
		{
			if (method == null || method.DeclaringType.IsInterface || method.IsExplicitImplementation ())
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
			} else {
				if (method.IsStatic)
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
			}
			if (attrs.Count == 0)
				return String.Empty;
			
			return String.Join (" ", attrs.ToArray ()) + " ";
		}
		
		public static string FormatAccessor (string name, MethodDefinition accessor, bool needsAttributes)
		{
			if (accessor == null || String.IsNullOrEmpty (name))
				return String.Empty;
			
			var sb = new StringBuilder ();
			Utils.Indent++;
			try {
				sb.AppendLine ();
				if (accessor.HasCustomAttributes)
					sb.Append (accessor.CustomAttributes.Format ());
				if (needsAttributes) {
					sb.AppendIndent (FormatAttributes (accessor));
					sb.Append (' ');
				} else
					sb.AppendIndent ();
				
				sb.Append (name);
				bool needNewline = false;
				if (accessor.IsAbstract || accessor.DeclaringType.IsInterface || (needNewline = accessor.IsCompilerGenerated ())) {
					sb.Append (";");
				} else
					sb.Append (" { throw new NotImplementedException (); }");
			} finally {
				Utils.Indent--;
			}
			
			return sb.ToString ();
		}
		
		public static string FormatName (this EventDefinition ev)
		{
			if (ev == null)
				return String.Empty;
			
			var sb = new StringBuilder ();
			if (ev.HasCustomAttributes)
				sb.Append (ev.CustomAttributes.Format ());
			
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
			sb.AppendLine ();
			
			sb.AppendLineIndent ("}");
			sb.AppendLine ();
			
			return sb.ToString ();
		}
		
		public static string FormatName (this PropertyDefinition prop)
		{
			if (prop == null)
				return String.Empty;
		
			var sb = new StringBuilder ();
			if (prop.HasCustomAttributes)
				sb.Append (prop.CustomAttributes.Format ());
			
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
			} else if (accessor.IsExplicitImplementation ()) {
				TypeReference iface;
				MethodReference ifaceMethod;
				
				accessor.GetInfoForExplicitlyImplementedMethod (out iface, out ifaceMethod);
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
			sb.AppendLine ();
			
			sb.AppendLineIndent ("}");
			sb.AppendLine ();
			
			return sb.ToString ();
		}
		
		public static string FormatName (this MethodDefinition method)
		{
			if (method == null)
				return String.Empty;
			
			string name = method.Name;
			var sb = new StringBuilder ();
			if (method.HasCustomAttributes)
				sb.Append (method.CustomAttributes.Format ());
			sb.AppendIndent (FormatAttributes (method));
			if (!method.IsSpecialName) {
				sb.Append (FormatName (method.ReturnType));
				sb.Append (' ');
			} else if (method.IsAccessor ())
				return String.Empty;
			
			if (method.IsConstructor)
				sb.Append (FormatGenericTypeName (method.DeclaringType.Name));
			else if (name == "Finalize")
				sb.Append ("~" + FormatGenericTypeName (method.DeclaringType.Name));
			else if (method.IsSpecialName)
				sb.Append (TranslateSpecialName (method));
			else if (method.IsExplicitImplementation ()) {
				TypeReference iface;
				MethodReference ifaceMethod;
				
				method.GetInfoForExplicitlyImplementedMethod (out iface, out ifaceMethod);
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
					Utils.Indent++;
					sb.AppendLineIndent ("throw new NotImplementedException ();");
				} finally {
					Utils.Indent--;
				}
				sb.AppendLineIndent ("}");
				sb.AppendLine ();
			}
			
			return sb.ToString ();
		}
		
		public static string FormatName (this ParameterDefinition parameter)
		{
			if (parameter == null)
				return String.Empty;
			
			var sb = new StringBuilder ();
			bool isParamArray = false;
			if (parameter.HasCustomAttributes) {
				if (parameter.CustomAttributes.First (attr => {
					if (String.Compare (attr.AttributeType.FullName, "System.ParamArrayAttribute", StringComparison.Ordinal) == 0)
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
		
		public static string FormatGenericTypeName (this string name)
		{
			if (String.IsNullOrEmpty (name))
				return String.Empty;
			
			int idx = name.IndexOf ('`');
			if (idx >= 0)
				name = name.Substring (0, idx);
			
			return name;
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
	}
}