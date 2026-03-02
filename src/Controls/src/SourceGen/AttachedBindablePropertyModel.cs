using Microsoft.CodeAnalysis;

namespace Microsoft.Maui.Controls.SourceGen;

public record AttachedBindablePropertyModel(string PropertyName, ITypeSymbol ReturnType, ITypeSymbol DeclaringType, string DefaultValue, string DefaultBindingMode, string ValidateValueMethodName, string PropertyChangedMethodName, string PropertyChangingMethodName, string CoerceValueMethodName, string DefaultValueCreatorMethodName, string? GetterAccessibility, string? SetterAccessibility, string BindablePropertyAccessibility, bool IsDeclaringTypeNullable, string? BindablePropertyXmlDocumentation, string? GetterMethodXmlDocumentation, string? SetterMethodXmlDocumentation, bool IsReadOnlyBindableProperty)
{
	public string BindablePropertyName => $"{PropertyName}Property";
	public bool ShouldPostpendNullable => ShouldPostpendNullableToType(ReturnType, IsDeclaringTypeNullable);
	public string BindablePropertyKeyName => $"{char.ToLower(PropertyName[0])}{PropertyName.Substring(1)}PropertyKey";

	public string EffectiveBindablePropertyXmlDocumentation => BindablePropertyXmlDocumentation ??
														/* language=C#-test */
														//lang=csharp
														$"""
	                                                     /// <summary>
	                                                     /// Attached BindableProperty for the {PropertyName} property.
	                                                     /// </summary>
	                                                     """;

	public string EffectiveGetterMethodXmlDocumentation => GetterMethodXmlDocumentation ??
													/* language=C#-test */
													//lang=csharp
													$"""
	                                                 /// <summary>
	                                                 /// Gets {PropertyName} for the <paramref name = "bindable"/> child element.
	                                                 /// </summary>
	                                                 """;

	public string EffectiveSetterMethodXmlDocumentation => SetterMethodXmlDocumentation ??
														   /* language=C#-test */
														   //lang=csharp
														   $"""
	                                                        /// <summary>
	                                                        /// Sets {PropertyName} for the <paramref name = "bindable"/> child element.
	                                                        /// </summary>
	                                                        """;

	internal static bool ShouldPostpendNullableToType(ITypeSymbol typeSymbol, bool isDeclaringTypeNullable)
		=> isDeclaringTypeNullable && typeSymbol.OriginalDefinition.SpecialType is not SpecialType.System_Nullable_T;
}