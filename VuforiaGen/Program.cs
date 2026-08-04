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
				//["Windows"] = new[] { "VU_IS_WINDOWS", "VU_PLATFORM_WINDOWS", "WIN32", "_WIN32" },
				["Android"] = new[] { "__ANDROID__", "VU_PLATFORM_ANDROID" },
				["iOS"]     = new[] { "__APPLE__", "TARGET_OS_IOS=1", "VU_PLATFORM_IOS" },
			};

			var compilations = new List<PlatformCompilation>();

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

				// Fatal, not printed and ignored. A partial compilation still produces output,
				// so the previous behaviour generated bindings from whatever libclang managed
				// to parse and exited 0 -- an incomplete P/Invoke surface that compiles and
				// only fails at the call site. In the sibling repo, making this fatal is what
				// exposed a second fault that the first one had been hiding.
				if (compilation.HasErrors)
				{
					Console.WriteLine($"  Parsing errors ({platform}):");
					foreach (var message in compilation.Diagnostics.Messages)
					{
						if (message.Type == CppLogMessageType.Error)
							Console.WriteLine($"    ERROR: {message}");
					}

					Console.Error.WriteLine(
						$"Refusing to generate from a failed parse of the {platform} " +
						"compilation. Fix the errors above, or the bindings will be missing " +
						"whatever libclang could not read.");
					Environment.Exit(1);
				}

				compilations.Add(new PlatformCompilation(platform, compilation));
			}

			string outputPath = Path.Combine(
				FindRepositoryRoot(),
				"Evergine.Bindings.Vuforia", "Generated");

			// Deliberately not created if missing. This used to be a CreateDirectory, which
			// turned a wrong path into a silent success: under `dotnet publish` the old relative
			// walk landed in VuforiaGen/, the generator wrote six files into a directory nobody
			// reads, exited 0, and the pack used the committed output. Since CI and CD both run
			// the publish output, every run has regenerated into nowhere.
			if (!Directory.Exists(outputPath))
			{
				throw new DirectoryNotFoundException(
					$"Output directory not found: {outputPath}. It is committed to the " +
					"repository, so its absence means the path was resolved wrongly.");
			}

			Console.WriteLine($"Output path: {Path.GetFullPath(outputPath)}");

			CsCodeGenerator.Instance.Generate(compilations, outputPath);

			Console.WriteLine("Code generation complete.");
		}

		// Anchored on binding.yml rather than a count of "..", because the number of levels
		// depends on how the generator was started: `dotnet run` puts the binary in
		// bin/<cfg>/<tfm>/<rid>/ and `dotnet publish` adds a publish/ below that. Five levels
		// were right for one and off by one for the other, and the one it was wrong for is the
		// one CI and CD use. A marker file cannot drift like that, and it also survives
		// somebody moving a project folder.
		static string FindRepositoryRoot()
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);

			while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "binding.yml")))
			{
				dir = dir.Parent;
			}

			if (dir is null)
			{
				throw new DirectoryNotFoundException(
					"Could not find binding.yml walking up from " +
					$"{AppContext.BaseDirectory}. The generator locates its output relative " +
					"to the manifest, so it cannot run outside the repository.");
			}

			return dir.FullName;
		}
	}
}
