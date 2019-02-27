### Testing different methods for getting the last byte from GUID from performance perspective

In a high-performance application we needed to know the last byte of GUID, used for table sharding. These are the results of several different approaches, some of which probably could be optimized further.

See [here](https://github.com/SanderSade/GuidLastByteSpeed/blob/master/GuidHandlingTest/Program.cs) for the methods.

``` ini

BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17763.316 (1809/October2018Update/Redstone5)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 4 logical and 4 physical cores
  [Host] : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.3324.0
  Clr    : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.3324.0

Job=Clr  Runtime=Clr  

```
|        Method |       Mean |     Error |    StdDev | Ratio | Rank | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|-------------- |-----------:|----------:|----------:|------:|-----:|------------:|------------:|------------:|--------------------:|
|     ViaString | 2,326.3 us | 46.081 us | 68.972 us |  1.00 |    1 |    238.2813 |           - |           - |           986.38 KB |
|               |            |           |           |       |      |             |             |             |                     |
|  ViaByteArray |   220.8 us |  4.815 us |  9.726 us |  1.00 |    1 |     69.0918 |           - |           - |           283.24 KB |
|               |            |           |           |       |      |             |             |             |                     |
|   **ViaDelegate** |   168.9 us |  1.919 us |  1.795 us |  1.00 |    1 |      2.4414 |      0.7324 |           - |            10.67 KB |
|               |            |           |           |       |      |             |             |             |                     |
| ViaReflection | 1,962.7 us | 22.117 us | 20.688 us |  1.00 |    1 |     85.9375 |           - |           - |           361.39 KB |
