using CppAst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace VuforiaGen
{
	class Program
	{
		static void Main(string[] args)
		{
			var headerFile = Path.Combine(AppContext.BaseDirectory, "Headers", "VuforiaEngine", "VuforiaEngine.h");
			var includePath = Path.Combine(AppContext.BaseDirectory, "Headers");

			// Parse the umbrella header once per platform to capture platform-specific
			// types and functions that sit behind #if / #elif guards.
			var platformDefines = new Dictionary<string, string[]>
			{
				["Windows"] = new[] { "VU_IS_WINDOWS", "VU_PLATFORM_WINDOWS", "WIN32", "_WIN32" },
				["Android"] = new[] { "__ANDROID__", "VU_PLATFORM_ANDROID" },
				["iOS"]     = new[] { "__APPLE__", "TARGET_OS_IOS=1", "VU_PLATFORM_IOS" },
			};

			var compilations = new List<CppCompilation>();

			foreach (var (platform, defines) in platformDefines)
			{
				Console.WriteLine($"Parsing for platform: {platform}");

				var options = new CppParserOptions
				{
					ParseMacros = true,
					IncludeFolders = { includePath },
				};

				foreach (var define in defines)
				{
					options.Defines.Add(define);
				}

				var compilation = CppParser.ParseFile(headerFile, options);

				if (compilation.HasErrors)
				{
					Console.WriteLine($"  Parsing errors ({platform}):");
					foreach (var message in compilation.Diagnostics.Messages)
					{
						if (message.Type == CppLogMessageType.Error)
							Console.WriteLine($"    ERROR: {message}");
					}
				}

				compilations.Add(compilation);
			}

			string outputPath = Path.Combine(
				AppContext.BaseDirectory, "..", "..", "..", "..", "..",
				"Evergine.Bindings.Vuforia", "Generated");

			if (!Directory.Exists(outputPath))
				Directory.CreateDirectory(outputPath);

			Console.WriteLine($"Output path: {Path.GetFullPath(outputPath)}");

			CsCodeGenerator.Instance.Generate(compilations, outputPath);

			Console.WriteLine("Code generation complete.");
		}
	}
}
