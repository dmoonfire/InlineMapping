﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace InlineMapping.TestHost
{
	public static class Program
	{
		public static void Main() => Program.GenerateHappyPath();

		public static void GenerateHappyPath()
		{
			// TODO: Do this with records and init
			var (diagnostics, output) = Program.GetGeneratedOutput(
@"using InlineMapping;

[MapTo(typeof(Destination))]
public record Source
{
	public int Id { get; init; }
}

public record Destination
{
	public int Id { get; init; }
}");

			Console.Out.WriteLine($"diagnostics.Length is {diagnostics.Length}.");

			foreach(var diagnostic in diagnostics)
			{
				Console.Out.WriteLine(diagnostic);
			}

			Console.Out.WriteLine(output);
		}

		private static (ImmutableArray<Diagnostic>, string) GetGeneratedOutput(string source)
		{
			var syntaxTree = CSharpSyntaxTree.ParseText(source);
			var references = AppDomain.CurrentDomain.GetAssemblies()
				.Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
				.Select(_ => MetadataReference.CreateFromFile(_.Location))
				.Concat(new[] { MetadataReference.CreateFromFile(typeof(MapToAttribute).Assembly.Location) });
			var compilation = CSharpCompilation.Create("generator", new SyntaxTree[] { syntaxTree },
				references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			var generator = new MapToGenerator();

			var driver = CSharpGeneratorDriver.Create(ImmutableArray.Create<ISourceGenerator>(generator));
			driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

			return (diagnostics, outputCompilation.SyntaxTrees.Last().ToString());
		}
	}
}