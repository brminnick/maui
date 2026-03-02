using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.SourceGen;

namespace Microsoft.Maui.Controls.Xaml.UnitTests.SourceGen;

public abstract class BaseAttachedBindablePropertyAttributeSourceGeneratorTest : BaseBindablePropertyTest
{
	protected static Task VerifySourceGeneratorAsync(string source, params List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList)
		=> VerifySourceGeneratorAsync<AttachedBindablePropertyAttributeSourceGenerator>(source, expectedGeneratedFilesList);


	protected static Task VerifySourceGeneratorAsync(string source, string expectedGeneratedFile)
	{
		List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList =
		[
			($"{defaultTestClassName}.g.cs", expectedGeneratedFile)
		];

		return VerifySourceGeneratorAsync<AttachedBindablePropertyAttributeSourceGenerator>(source, expectedGeneratedFilesList);
	}
}