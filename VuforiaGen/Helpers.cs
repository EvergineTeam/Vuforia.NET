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
			// Evergine.Mathematics type mappings
			{ "VuVector2F", "Vector2" },
			{ "VuVector3F", "Vector3" },
			{ "VuVector4F", "Vector4" },
			{ "VuMatrix44F", "Matrix4x4" },
			{ "VuMatrix33F", "Matrix3x3" },
		};

		public static bool IsMappedType(string name) => csNameMappings.ContainsKey(name);

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

		public static string GetCsCleanName(string name)
		{
			if (string.IsNullOrEmpty(name))
				return "IntPtr";

			// Function pointer typedefs → IntPtr
			if (name.StartsWith("PFN"))
				return "IntPtr";

			// Known type mappings (VuBool → uint, etc.)
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
				if (string.IsNullOrEmpty(trimmed))
					continue;

				// Skip Doxygen \var lines (e.g. "\var NAME NAME")
				if (trimmed.StartsWith("\\var ") || trimmed.StartsWith("@var "))
					continue;

				// Skip lines that are just repeated VU_* identifiers (CppAst strips \var but keeps args)
				if (IsVarCommentLine(trimmed))
					continue;

				// Strip Doxygen \brief prefix
				if (trimmed.StartsWith("\\brief "))
					trimmed = trimmed.Substring(7).TrimStart();
				else if (trimmed.StartsWith("@brief "))
					trimmed = trimmed.Substring(7).TrimStart();

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

		/// <summary>
		/// Detects lines that consist solely of repeated VU_* identifiers,
		/// which are artifacts from Doxygen \var commands after CppAst strips the command.
		/// E.g. "VU_OBSERVER_ANCHOR_TYPE VU_OBSERVER_ANCHOR_TYPE"
		/// </summary>
		private static bool IsVarCommentLine(string line)
		{
			var words = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
			if (words.Length == 0)
				return false;

			// All words must be VU_* identifiers
			foreach (var word in words)
			{
				if (!word.StartsWith("VU_") && !word.StartsWith("Vu") && !word.StartsWith("vu"))
					return false;
				// Must look like a C identifier (letters, digits, underscores only)
				foreach (var ch in word)
				{
					if (!char.IsLetterOrDigit(ch) && ch != '_')
						return false;
				}
			}

			return true;
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

		/// <summary>
		/// Strip the Vuforia C prefix from a name.
		/// Handles: VU_ (constants/macros), Vu (types), vu (functions).
		/// </summary>
		public static string StripPrefix(string name)
		{
			if (string.IsNullOrEmpty(name))
				return name;

			// SCREAMING_CASE constants/macros: VU_
			if (name.StartsWith("VU_"))
				return name.Substring(3);

			// PascalCase types: Vu
			if (name.StartsWith("Vu") && name.Length > 2 && char.IsUpper(name[2]))
				return name.Substring(2);

			// camelCase functions: vu
			if (name.StartsWith("vu") && name.Length > 2 && char.IsUpper(name[2]))
				return name.Substring(2);

			return name;
		}

		/// <summary>
		/// Capitalize the first letter of a struct field name (camelCase → PascalCase).
		/// Preserves existing uppercase runs (e.g. bodyID → BodyID).
		/// </summary>
		public static string PascalCaseField(string name)
		{
			if (string.IsNullOrEmpty(name))
				return name;

			if (char.IsUpper(name[0]))
				return name;

			return char.ToUpperInvariant(name[0]) + name.Substring(1);
		}

		/// <summary>
		/// Find the longest common prefix at underscore boundaries among SCREAMING_CASE names.
		/// E.g. given VU_IMAGE_PIXEL_FORMAT_UNKNOWN, VU_IMAGE_PIXEL_FORMAT_RGB565
		/// → common prefix is "VU_IMAGE_PIXEL_FORMAT_"
		/// </summary>
		public static string FindCommonPrefix(IEnumerable<string> names)
		{
			var list = names.ToList();
			if (list.Count == 0)
				return string.Empty;

			if (list.Count == 1)
			{
				// For a single item, find prefix up to last underscore
				var lastUnderscore = list[0].LastIndexOf('_');
				if (lastUnderscore > 0)
					return list[0].Substring(0, lastUnderscore + 1);
				return string.Empty;
			}

			var first = list[0];
			int prefixLen = first.Length;

			for (int i = 1; i < list.Count; i++)
			{
				var other = list[i];
				int maxLen = System.Math.Min(prefixLen, other.Length);
				int match = 0;
				while (match < maxLen && first[match] == other[match])
					match++;
				prefixLen = match;
			}

			// Snap back to the last underscore boundary
			var prefix = first.Substring(0, prefixLen);
			var lastSnap = prefix.LastIndexOf('_');
			if (lastSnap > 0)
				return prefix.Substring(0, lastSnap + 1);

			return string.Empty;
		}

		/// <summary>
		/// Convert a SCREAMING_CASE identifier to PascalCase.
		/// E.g. DONT_ACTIVATE → DontActivate, SUCCESS → Success, 2D → 2D
		/// If the result starts with a digit, prefix with underscore.
		/// </summary>
		public static string ScreamingToPascalCase(string screaming)
		{
			if (string.IsNullOrEmpty(screaming))
				return screaming;

			var parts = screaming.Split('_');
			var result = new System.Text.StringBuilder();

			foreach (var part in parts)
			{
				if (string.IsNullOrEmpty(part))
					continue;

				// Numeric-leading segments preserved as-is (e.g. "2D", "8PARAMS")
				if (char.IsDigit(part[0]))
				{
					result.Append(part);
					continue;
				}

				// Capitalize first letter, lowercase the rest
				result.Append(char.ToUpperInvariant(part[0]));
				if (part.Length > 1)
					result.Append(part.Substring(1).ToLowerInvariant());
			}

			var name = result.ToString();

			// C# identifiers cannot start with a digit — prefix with underscore
			if (name.Length > 0 && char.IsDigit(name[0]))
				name = "_" + name;

			return name;
		}
	}
}
