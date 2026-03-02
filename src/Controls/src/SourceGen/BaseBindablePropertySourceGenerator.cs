using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Maui.Controls.SourceGen;

public abstract class BaseBindablePropertySourceGenerator : IIncrementalGenerator
{
	public abstract void Initialize(IncrementalGeneratorInitializationContext context);

	protected static void FormatText(ref string classSource, CSharpParseOptions? options = null)
	{
		options ??= CSharpParseOptions.Default;

		var sourceCode = CSharpSyntaxTree.ParseText(SourceText.From(classSource, Encoding.UTF8), options);
		var formattedRoot = (CSharpSyntaxNode)sourceCode.GetRoot().NormalizeWhitespace();
		classSource = CSharpSyntaxTree.Create(formattedRoot).ToString();
	}
	
	protected static string GetNamedTypeArgumentsAttributeValueForDefaultBindingMode(AttributeData attribute, string name, string placeholder)
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		return data.Value is null ? placeholder : $"({data.Type}){data.Value}";
	}

	protected static string GetNamedMethodGroupArgumentsAttributeValueByNameAsString(AttributeData attribute, string name)
	{
		var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

		return data.Value?.ToString() ?? "null";
	}

	protected record BindablePropertySemanticValues(ClassInformation ClassInformation, EquatableArray<BindablePropertyModel> BindableProperties);

	protected record AttachedBindablePropertySemanticValues(ClassInformation ClassInformation, EquatableArray<AttachedBindablePropertyModel> BindableProperties);

	protected readonly record struct ClassInformation(string ClassName, string DeclaredAccessibility, string ContainingNamespace, string ContainingTypes = "", string GenericTypeParameters = "");

	protected readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
		where T : IEquatable<T>
	{
		/// <summary>
		/// The underlying <typeparamref name="T"/> array.
		/// </summary>
		readonly T[]? array;

		/// <summary>
		/// Creates a new <see cref="EquatableArray{T}"/> instance.
		/// </summary>
		/// <param name="array">The input <see cref="ImmutableArray"/> to wrap.</param>
		public EquatableArray(ImmutableArray<T> array)
		{
			this.array = Unsafe.As<ImmutableArray<T>, T[]?>(ref array);
		}

		/// <summary>
		/// Gets a reference to an item at a specified position within the array.
		/// </summary>
		/// <param name="index">The index of the item to retrieve a reference to.</param>
		/// <returns>A reference to an item at a specified position within the array.</returns>
		public ref readonly T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref AsImmutableArray().ItemRef(index);
		}

		/// <summary>
		/// Gets a value indicating whether the current array is empty.
		/// </summary>
		public bool IsEmpty
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => AsImmutableArray().IsEmpty;
		}

		/// <sinheritdoc/>
		public bool Equals(EquatableArray<T> array)
		{
			return AsSpan().SequenceEqual(array.AsSpan());
		}

		/// <sinheritdoc/>
		public override bool Equals(object? obj)
		{
			return obj is EquatableArray<T> array && Equals(this, array);
		}

		/// <sinheritdoc/>
		public override int GetHashCode()
		{
			if (this.array is not T[] array)
			{
				return 0;
			}

			MutableHashCode mutableHashCode = default;

			foreach (T item in array)
			{
				mutableHashCode.Add(item);
			}

			return mutableHashCode.ToHashCode();
		}

		/// <summary>
		/// Gets an <see cref="ImmutableArray{T}"/> instance from the current <see cref="EquatableArray{T}"/>.
		/// </summary>
		/// <returns>The <see cref="ImmutableArray{T}"/> from the current <see cref="EquatableArray{T}"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ImmutableArray<T> AsImmutableArray()
		{
			return Unsafe.As<T[]?, ImmutableArray<T>>(ref Unsafe.AsRef(in this.array));
		}

		/// <summary>
		/// Creates an <see cref="EquatableArray{T}"/> instance from a given <see cref="ImmutableArray{T}"/>.
		/// </summary>
		/// <param name="array">The input <see cref="ImmutableArray{T}"/> instance.</param>
		/// <returns>An <see cref="EquatableArray{T}"/> instance from a given <see cref="ImmutableArray{T}"/>.</returns>
		public static EquatableArray<T> FromImmutableArray(ImmutableArray<T> array)
		{
			return new(array);
		}

		/// <summary>
		/// Returns a <see cref="ReadOnlySpan{T}"/> wrapping the current items.
		/// </summary>
		/// <returns>A <see cref="ReadOnlySpan{T}"/> wrapping the current items.</returns>
		public ReadOnlySpan<T> AsSpan()
		{
			return AsImmutableArray().AsSpan();
		}

		/// <summary>
		/// Copies the contents of this <see cref="EquatableArray{T}"/> instance. to a mutable array.
		/// </summary>
		/// <returns>The newly instantiated array.</returns>
		public T[] ToArray()
		{
			return AsImmutableArray().ToArray();
		}

		/// <summary>
		/// Gets an <see cref="ImmutableArray{T}.Enumerator"/> value to traverse items in the current array.
		/// </summary>
		/// <returns>An <see cref="ImmutableArray{T}.Enumerator"/> value to traverse items in the current array.</returns>
		public ImmutableArray<T>.Enumerator GetEnumerator()
		{
			return AsImmutableArray().GetEnumerator();
		}

		/// <sinheritdoc/>
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();
		}

		/// <sinheritdoc/>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)AsImmutableArray()).GetEnumerator();
		}

		/// <summary>
		/// Implicitly converts an <see cref="ImmutableArray{T}"/> to <see cref="EquatableArray{T}"/>.
		/// </summary>
		/// <returns>An <see cref="EquatableArray{T}"/> instance from a given <see cref="ImmutableArray{T}"/>.</returns>
		public static implicit operator EquatableArray<T>(ImmutableArray<T> array)
		{
			return FromImmutableArray(array);
		}

		/// <summary>
		/// Implicitly converts an <see cref="EquatableArray{T}"/> to <see cref="ImmutableArray{T}"/>.
		/// </summary>
		/// <returns>An <see cref="ImmutableArray{T}"/> instance from a given <see cref="EquatableArray{T}"/>.</returns>
		public static implicit operator ImmutableArray<T>(EquatableArray<T> array)
		{
			return array.AsImmutableArray();
		}

		/// <summary>
		/// Checks whether two <see cref="EquatableArray{T}"/> values are the same.
		/// </summary>
		/// <param name="left">The first <see cref="EquatableArray{T}"/> value.</param>
		/// <param name="right">The second <see cref="EquatableArray{T}"/> value.</param>
		/// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are equal.</returns>
		public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Checks whether two <see cref="EquatableArray{T}"/> values are not the same.
		/// </summary>
		/// <param name="left">The first <see cref="EquatableArray{T}"/> value.</param>
		/// <param name="right">The second <see cref="EquatableArray{T}"/> value.</param>
		/// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are not equal.</returns>
		public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
		{
			return !left.Equals(right);
		}

		struct MutableHashCode
		{
			const uint prime1 = 2654435761U;
			const uint prime2 = 2246822519U;
			const uint prime3 = 3266489917U;
			const uint prime4 = 668265263U;
			const uint prime5 = 374761393U;

			static readonly uint seed = GenerateGlobalSeed();

			uint v1, v2, v3, v4;
			uint queue1, queue2, queue3;
			uint length;

			/// <summary>
			/// Initializes the default seed.
			/// </summary>
			/// <returns>A random seed.</returns>
			static unsafe uint GenerateGlobalSeed()
			{
				byte[] bytes = new byte[4];

				RandomNumberGenerator.Create().GetBytes(bytes);

				return BitConverter.ToUInt32(bytes, 0);
			}

			/// <summary>
			/// Adds a single value to the current hash.
			/// </summary>
			/// <typeparam name="TValue">The type of the value to add into the hash code.</typeparam>
			/// <param name="value">The value to add into the hash code.</param>
			public void Add<TValue>(TValue value)
			{
				Add(value?.GetHashCode() ?? 0);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
			{
				v1 = seed + prime1 + prime2;
				v2 = seed + prime2;
				v3 = seed;
				v4 = seed - prime1;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static uint Round(uint hash, uint input)
			{
				return RotateLeft(hash + input * prime2, 13) * prime1;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static uint QueueRound(uint hash, uint queuedValue)
			{
				return RotateLeft(hash + queuedValue * prime3, 17) * prime4;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static uint MixState(uint v1, uint v2, uint v3, uint v4)
			{
				return RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static uint MixEmptyState()
			{
				return seed + prime5;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static uint MixFinal(uint hash)
			{
				hash ^= hash >> 15;
				hash *= prime2;
				hash ^= hash >> 13;
				hash *= prime3;
				hash ^= hash >> 16;

				return hash;
			}

			void Add(int value)
			{
				uint val = (uint)value;
				uint previousLength = length++;
				uint position = previousLength % 4;

				if (position == 0)
				{
					queue1 = val;
				}
				else if (position == 1)
				{
					queue2 = val;
				}
				else if (position == 2)
				{
					queue3 = val;
				}
				else
				{
					if (previousLength == 3)
					{
						Initialize(out v1, out v2, out v3, out v4);
					}

					v1 = Round(v1, queue1);
					v2 = Round(v2, queue2);
					v3 = Round(v3, queue3);
					v4 = Round(v4, val);
				}
			}

			/// <summary>
			/// Gets the resulting hashcode from the current instance.
			/// </summary>
			/// <returns>The resulting hashcode from the current instance.</returns>
			public int ToHashCode()
			{
				uint length = this.length;
				uint position = length % 4;
				uint hash = length < 4 ? MixEmptyState() : MixState(v1, v2, v3, v4);

				hash += length * 4;

				if (position > 0)
				{
					hash = QueueRound(hash, queue1);

					if (position > 1)
					{
						hash = QueueRound(hash, queue2);

						if (position > 2)
						{
							hash = QueueRound(hash, queue3);
						}
					}
				}

				hash = MixFinal(hash);

				return (int)hash;
			}
#pragma warning disable CS0809
			/// <inheritdoc/>
			[Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes. Use ToHashCode to retrieve the computed hash code.", error: true)]
			[EditorBrowsable(EditorBrowsableState.Never)]
			public override int GetHashCode() => throw new NotSupportedException();

			/// <inheritdoc/>
			[Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes.", error: true)]
			[EditorBrowsable(EditorBrowsableState.Never)]
			public override bool Equals(object? obj) => throw new NotSupportedException();
#pragma warning restore CS0809

			/// <summary>
			/// Rotates the specified value left by the specified number of bits.
			/// Similar in behavior to the x86 instruction ROL.
			/// </summary>
			/// <param name="value">The value to rotate.</param>
			/// <param name="offset">The number of bits to rotate by.
			/// Any value outside the range [0..31] is treated as congruent mod 32.</param>
			/// <returns>The rotated value.</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static uint RotateLeft(uint value, int offset)
			{
				return (value << offset) | (value >> (32 - offset));
			}
		}
	}
}