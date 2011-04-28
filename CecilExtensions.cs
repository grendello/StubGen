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

using Mono.Cecil;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace StubGen
{
	public static class CecilExtensions
	{
		static readonly List <string> accessorPrefixes = new List<string> {
			"get_",
			"set_",
			"add_",
			"remove_"
		};
		
		public static bool IsCompilerGenerated (this MethodDefinition method)
		{
			if (method == null || !method.HasCustomAttributes)
				return false;
			
			return method.CustomAttributes.FirstOrDefault (attr => {
				return String.Compare (attr.AttributeType.FullName, "System.Runtime.CompilerServices.CompilerGeneratedAttribute", StringComparison.Ordinal) == 0;
			}) != null;
		}
		
		public static IEnumerable <TypeReference> OnlyVisible (this IEnumerable <TypeReference> references, bool includePrivate)
		{
			if (references == null)
				return null;
			
			TypeDefinition tdef;
			return references.Where (reference => {
				try {
					tdef = reference.Resolve ();
					if (tdef == null)
						return false;
				} catch {
					return false;
				}
				
				if (tdef.IsNotPublic && !includePrivate)
					return false;
				return true;
			});
		}
		
		public static IEnumerable <TypeReference> OnlyVisible (this Collection <TypeReference> references, bool includePrivate)
		{
			if (references == null || references.Count == 0)
				return references;
			
			return ((IEnumerable <TypeReference>) references).OnlyVisible (includePrivate);
		}
		
		public static IEnumerable <FieldDefinition> OnlyVisible (this Collection <FieldDefinition> fields, bool includePrivate)
		{
			if (fields == null || fields.Count == 0)
				return fields;
			
			return fields.Where (field => {
				if (field.IsPrivate || (!field.IsPublic && !includePrivate))
					return false;
				return true;
			});
		}
		
		public static bool Visible (this MethodDefinition method, bool includePrivate)
		{
			if (method == null)
				return false;
			
			if (method.IsPrivate || (!method.IsPublic && !includePrivate))
				return false;
			
			return true;
		}
		
		public static IEnumerable <EventDefinition> OnlyVisible (this Collection <EventDefinition> events, bool includePrivate)
		{
			if (events == null || events.Count == 0)
				return events;
			
			return events.Where (ev => {
				if (ev.AddMethod.Visible (includePrivate) ||
					ev.InvokeMethod.Visible (includePrivate) ||
					ev.RemoveMethod.Visible (includePrivate))
					return true;
					
				return false;
			});
		}
		
		public static IEnumerable <PropertyDefinition> OnlyVisible (this Collection <PropertyDefinition> properties, bool includePrivate)
		{
			if (properties == null || properties.Count == 0)
				return properties;
			
			return properties.Where (prop => {
				if (prop.GetMethod.Visible (includePrivate) ||
					prop.SetMethod.Visible (includePrivate))
					return true;
					
				return false;
			});
		}
		
		public static IEnumerable <MethodDefinition> OnlyVisible (this Collection <MethodDefinition> methods, bool includePrivate)
		{
			if (methods == null || methods.Count == 0)
				return methods;
			
			return methods.Where (method => {
				return method.Visible (includePrivate);
			});
		}
		
		public static string GetTypeName (this TypeReference type)
		{
			if (type == null)
				return String.Empty;
			
			string fname = type.FullName;
			int idx = fname.IndexOf ("/");
			if (idx >= 0) {
				idx = fname.LastIndexOf (".");
				if (idx >= 0)
					return fname.Substring (idx + 1).Replace ("/", ".");
				else
					return fname;
			}
			return type.Name;
		}
		
		public static void GetInfoForExplicitlyImplementedMethod (this MethodDefinition method, out TypeReference iface, out MethodReference ifaceMethod)
		{
			iface = null;
			ifaceMethod = null;
			if (method == null)
				return;
			if (method.Overrides.Count != 1)
				Console.Error.WriteLine ("\tCould not determine interface type for explicitly-implemented interface member " + method.FullName);
			else {
				iface = method.Overrides [0].DeclaringType;
				ifaceMethod = method.Overrides [0];
			}
		}
		
		public static TypeDefinition ResolveType (this TypeReference type)
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
				Console.Error.WriteLine ("\tUnresolved type '{0}'", type.FullName);
				
			return tdef;
		}
		
		public static bool IsAccessor (this MethodDefinition method)
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
		
		public static bool IsExplicitImplementation (this MethodDefinition method)
		{
			if (method == null)
				return false;
			
			return method.IsPrivate && method.IsFinal && method.IsVirtual;
		}
	}
}