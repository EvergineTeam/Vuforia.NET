using CppAst;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VuforiaGen
{
	public static class Helpers
	{
		public static List<string> TypedefList = new List<string>();

		// Delegate types that are defined as function pointer typedefs
		public static HashSet<string> DelegateTypes = new HashSet<string>();

		private static readonly Dictionary<string, string> csNameMappings = new()
		{
			{ "bool", "byte" },
			{ "uint8_t", "byte" },
			{ "uint16_t", "ushort" },
			{ "uint32_t", "uint" },
			{ "uint64_t", "ulong" },
			{ "int8_t", "sbyte" },
			{ "int16_t", "short" },
			{ "int32_t", "int" },
			{ "int64_t", "long" },
			{ "char", "byte" },
			{ "size_t", "nuint" },
			{ "intptr_t", "nint" },
			{ "uintptr_t", "nuint" },
			{ "VuBool", "uint" },
			{ "VuErrorCode", "int" },
			{ "VuFlags", "uint" },
			{ "VuObserverType", "int" },
			{ "VuObservationType", "int" },
			{ "VuRecordingDataFlags", "uint" },
		};

		public enum Family
		{
			Parameter,
			Field,
			ReturnValue,
		}

		public static string ConvertToCSharpType(CppType type, bool isPointer = false)
		{
			if (type is CppPrimitiveType primitiveType)
			{
				return primitiveType.Kind switch
				{
					CppPrimitiveKind.Void => isPointer ? "void" : "void",
					CppPrimitiveKind.Bool => "byte",
					CppPrimitiveKind.Char => "byte",
					CppPrimitiveKind.Short => "short",
					CppPrimitiveKind.Int => "int",
					CppPrimitiveKind.LongLong => "long",
					CppPrimitiveKind.UnsignedChar => "byte",
					CppPrimitiveKind.UnsignedShort => "ushort",
					CppPrimitiveKind.UnsignedInt => "uint",
					CppPrimitiveKind.UnsignedLongLong => "ulong",
					CppPrimitiveKind.Float => "float",
					CppPrimitiveKind.Double => "double",
					CppPrimitiveKind.WChar => "char",
					_ => "IntPtr",
				};
			}

			if (type is CppQualifiedType qualifiedType)
			{
				return ConvertToCSharpType(qualifiedType.ElementType, isPointer);
			}

			if (type is CppEnum enumType)
			{
				return GetCsCleanName(enumType.Name);
			}

			if (type is CppTypedef typedefType)
			{
				var name = GetCsCleanName(typedefType.Name);
				return name;
			}

			if (type is CppClass classType)
			{
				return GetCsCleanName(classType.Name);
			}

			if (type is CppPointerType pointerType)
			{
				var elementType = ConvertToCSharpType(pointerType.ElementType, true);

				// void* stays as void*
				if (elementType == "void")
					return "void*";

				// const char* → byte* for fields, string for params
				if (elementType == "byte" && IsCharPointer(pointerType))
					return "byte*";

				return elementType + "*";
			}

			if (type is CppArrayType arrayType)
			{
				return ConvertToCSharpType(arrayType.ElementType);
			}

			return "IntPtr";
		}

		public static string ShowAsMarshalType(string type, Family family, CppType originalType = null)
		{
			switch (family)
			{
				case Family.Parameter:
					// const char* → string for parameters
					if (originalType is CppPointerType ptrType && IsCharPointer(ptrType))
						return "[MarshalAs(UnmanagedType.LPStr)] string";
					if (type == "uint" && originalType is CppTypedef td && td.Name == "VuBool")
						return "[MarshalAs(UnmanagedType.Bool)] bool";
					break;
				case Family.Field:
					// Fields use raw types for blittable layout
					break;
				case Family.ReturnValue:
					break;
			}

			return type;
		}

		public static string GetCsCleanName(string name)
		{
			if (string.IsNullOrEmpty(name))
				return "IntPtr";

			// Function pointer typedefs → IntPtr
			if (name.StartsWith("PFN"))
				return "IntPtr";

			// Known type mappings
			if (csNameMappings.TryGetValue(name, out var mapped))
				return mapped;

			// Opaque handle types → keep as their named handle type (we generate wrapper structs)
			if (TypedefList.Contains(name))
				return name;

			return name;
		}

		public static bool IsCharPointer(CppPointerType ptrType)
		{
			var elementType = ptrType.ElementType;
			if (elementType is CppQualifiedType qt)
				elementType = qt.ElementType;
			if (elementType is CppPrimitiveType pt && (pt.Kind == CppPrimitiveKind.Char))
				return true;
			return false;
		}

		public static void PrintComments(StreamWriter writer, CppComment comment, string tabs, bool newLine = true)
		{
			if (comment == null)
				return;

			var text = GetCommentText(comment).Trim();
			if (string.IsNullOrEmpty(text))
				return;

			writer.WriteLine($"{tabs}/// <summary>");
			foreach (var line in text.Split('\n'))
			{
				var trimmed = line.Trim();
				if (!string.IsNullOrEmpty(trimmed))
					writer.WriteLine($"{tabs}/// {System.Security.SecurityElement.Escape(trimmed)}");
			}
			writer.WriteLine($"{tabs}/// </summary>");
		}

		private static string GetCommentText(CppComment comment)
		{
			if (comment == null)
				return string.Empty;

			if (comment.Kind == CppCommentKind.Text)
				return comment.ToString();

			if (comment.Children != null && comment.Children.Count > 0)
			{
				return string.Join("\n", comment.Children.Select(c => GetCommentText(c)));
			}

			return comment.ToString();
		}

		public static string EscapeReservedKeyword(string name)
		{
			var reserved = new HashSet<string>
			{
				"abstract", "as", "base", "bool", "break", "byte", "case", "catch",
				"char", "checked", "class", "const", "continue", "decimal", "default",
				"delegate", "do", "double", "else", "enum", "event", "explicit",
				"extern", "false", "finally", "fixed", "float", "for", "foreach",
				"goto", "if", "implicit", "in", "int", "interface", "internal",
				"is", "lock", "long", "namespace", "new", "null", "object",
				"operator", "out", "override", "params", "private", "protected",
				"public", "readonly", "ref", "return", "sbyte", "sealed", "short",
				"sizeof", "stackalloc", "static", "string", "struct", "switch",
				"this", "throw", "true", "try", "typeof", "uint", "ulong",
				"unchecked", "unsafe", "ushort", "using", "virtual", "void",
				"volatile", "while",
			};

			if (reserved.Contains(name))
				return "@" + name;

			return name;
		}

		public static bool IsOpaqueType(CppTypedef typedef)
		{
			// Pattern 1: typedef struct Foo_T* Foo (pointer to forward-declared struct)
			if (typedef.ElementType is CppPointerType ptrType
				&& ptrType.ElementType.TypeKind != CppTypeKind.Function
				&& !(ptrType.ElementType is CppPrimitiveType))
				return true;

			// Pattern 2: typedef struct Foo_ Foo (Vuforia pattern - forward-declared struct, no fields)
			if (typedef.ElementType is CppClass classType
				&& classType.Name.EndsWith("_")
				&& classType.Fields.Count == 0
				&& !classType.IsDefinition)
				return true;

			return false;
		}

		public static bool IsFunctionPointerTypedef(CppTypedef typedef)
		{
			if (typedef.ElementType is CppPointerType ptrType)
			{
				if (ptrType.ElementType is CppFunctionType)
					return true;
			}

			// Also check direct function types
			if (typedef.ElementType is CppFunctionType)
				return true;

			return false;
		}

		public static string GetCallingConvention(string apiCall)
		{
			// Vuforia uses VU_API_CALL which is __stdcall on Windows
			return "CallingConvention.StdCall";
		}
	}
}
