using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace GuidHandlingTest
{
	public static class Program
	{
		private static void Main()
		{
			Validate();
			var summary = BenchmarkRunner.Run<GuidBenchmark>();
			Console.Write(summary.AllRuntimes);
			Console.ReadKey();
		}

		/// <summary>
		///     Naive validation of different methods
		/// </summary>
		private static void Validate()
		{
			var guidBenchmark = new GuidBenchmark();
			guidBenchmark.Setup();
			var viaString = guidBenchmark.ViaString();
			var viaByteArray = guidBenchmark.ViaByteArray();
			var viaReflection = guidBenchmark.ViaReflection();
			var viaDelegate = guidBenchmark.ViaDelegate();

			for (var i = 0; i < viaString.Count; i++)
				if (viaString[i] != viaByteArray[i]
				    || viaByteArray[i] != viaReflection[i]
				    || viaReflection[i] != viaDelegate[i])
					throw new InvalidDataException("Byte unmatch!");

			Console.WriteLine("Validation successful");
		}


		[MemoryDiagnoser]
		[ClrJob(true)]
		[RankColumn]
		public class GuidBenchmark
		{
			private const int Count = 10000;
			private List<Guid> _guids;

			[GlobalSetup]
			public void Setup()
			{
				_guids = new List<Guid>(Count);
				for (var i = 0; i < Count; i++)
					_guids.Add(Guid.NewGuid());
			}


			[Benchmark]
			public List<byte> ViaString()
			{
				//after https://stackoverflow.com/a/14335076/3248515
				byte ParseHex(string hexString)
				{
					int ParseNybble(char c)
					{
						unchecked
						{
							var i = (uint) (c - '0');
							if (i < 10)
								return (int) i;
							i = (c & ~0x20u) - 'A';
							if (i < 6)
								return (int) i + 10;
							throw new ArgumentException("Invalid nybble: " + c);
						}
					}

					var high = ParseNybble(hexString[0]);
					var low = ParseNybble(hexString[1]);
					return (byte) ((high << 4) | low);
				}

				var result = new List<byte>(Count);
				foreach (var guid in _guids)
					result.Add(ParseHex(guid.ToString("N").Substring(30, 2)));

				return result;
			}


			[Benchmark]
			public List<byte> ViaByteArray()
			{
				var result = new List<byte>(Count);
				foreach (var guid in _guids)
					result.Add(guid.ToByteArray()[15]);
				return result;
			}

			/// <summary>
			///     From https://stackoverflow.com/a/16222886/3248515, modified
			/// </summary>
			private static Func<S, T> CreateGetter<S, T>()
			{
				var field = typeof(Guid).GetField("_k", BindingFlags.NonPublic | BindingFlags.Instance);
				Debug.Assert(field != null, nameof(field) + " != null");
				var methodName = field.ReflectedType.FullName + ".get_" + field.Name;
				var setterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] {typeof(S)}, true);
				var gen = setterMethod.GetILGenerator();
				if (field.IsStatic)
				{
					gen.Emit(OpCodes.Ldsfld, field);
				}
				else
				{
					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldfld, field);
				}

				gen.Emit(OpCodes.Ret);
				return (Func<S, T>) setterMethod.CreateDelegate(typeof(Func<S, T>));
			}


			[Benchmark]
			public List<byte> ViaDelegate()
			{
				var getter = CreateGetter<Guid, byte>();

				var result = new List<byte>(Count);
				foreach (var guid in _guids)
					result.Add(getter.Invoke(guid));
				return result;
			}

			[Benchmark]
			public List<byte> ViaReflection()
			{
				var field = typeof(Guid).GetField("_k", BindingFlags.NonPublic | BindingFlags.Instance);

				var result = new List<byte>(Count);
				foreach (var guid in _guids)
				{
					Debug.Assert(field != null, nameof(field) + " != null");
					result.Add((byte) field.GetValue(guid));
				}

				return result;
			}
		}
	}
}