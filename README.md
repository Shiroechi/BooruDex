# BooruDex

Library to access the booru website using public API. 
This library only support GET method, it means only listing and searching only. 
For other method like POST, contributions are welcomed.

Currently BooruDex support the following websites:
- [3Dbooru](http://behoimi.org/)
- [Danbooru.donmai.us](https://danbooru.donmai.us/)
- [Gelbooru](http://gelbooru.com/)
- [Konachan.com](http://konachan.com/)
- [Konachan.net](http://konachan.net/)
- [Lolibooru](http://lolibooru.moe/)
- [Realbooru](http://realbooru.com/)
- [Rule34](https://rule34.xxx/)
- [Safebooru](https://safebooru.org/)
- [Safebooru.donmai.us](http://safebooru.donmai.us/)
- [Xbooru](https://xbooru.com/)
- [Yandere](https://yande.re/)

## Feature 
- Artist
  - Search for artist by name
- Pool
  - Search for pool by name 
  - List of post inside pool
- Post
  - Search for multiple random image by tags
  - List of latest post update
- Tag
  - Search for tags by tag name pattern
  - Search for tag related by other tag
- Wiki
  - Search for wiki by title

**Note**
Some client may not have some feature listed above, because the API not supported.

## Feature by template

Only GET method.

| Template | Artist API | Pool API | Post API | Tag API | Wiki API |
| --- | --- | --- | --- | --- | --- |
| Danbooru | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| Gelbooru | ❌ | ❌ | ✔️ | ✔️ | ❌ |
| Gelbooru beta 0.2 | ❌ | ❌ | ✔️ | ❌ | ❌ |
| Moebooru | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |

# Benchmark

This benchmark with other library, [BooruSharp](https://github.com/Xwilarg/BooruSharp)

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1198 (1909/November2018Update/19H2)
AMD FX-8800P Radeon R7, 12 Compute Cores 4C+8G, 1 CPU, 4 logical and 4 physical cores
.NET Core SDK=5.0.100
  [Host]    : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT  [AttachedDebugger]
  MediumRun : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|             Method |       Mean |    Error |   StdDev |     Median |        Min |        Max | Rank | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|-----:|------:|------:|------:|----------:|
|   DanbooruBooruDex |   540.0 ms | 120.7 ms | 173.1 ms |   482.7 ms |   360.2 ms | 1,016.1 ms |    1 |     - |     - |     - | 291.38 KB |
| DanbooruBooruSharp |   841.0 ms | 520.9 ms | 713.1 ms |   458.7 ms |   368.5 ms | 3,243.3 ms |    1 |     - |     - |     - | 363.73 KB |
|    YandereBooruDex | 1,424.7 ms | 486.2 ms | 697.4 ms | 1,168.5 ms |   682.7 ms | 3,131.4 ms |    2 |     - |     - |     - | 270.42 KB |
|  YandereBooruSharp | 1,895.1 ms | 598.7 ms | 896.2 ms | 1,305.9 ms | 1,224.8 ms | 3,977.8 ms |    3 |     - |     - |     - | 302.39 KB |

**Note** 
The speed or perfomance may not accurate because internet connection. But BooruDex is more effecient handling memory(RAM) usage.

Refer to this [repository](https://github.com/Shiroechi/BooruDex.Test) for the benchmark log and source code.

# Download

[![Nuget](https://img.shields.io/nuget/v/BooruDex?label=Litdex.Security.RNG)](https://www.nuget.org/packages/BooruDex)
[![Nuget](https://img.shields.io/nuget/v/Litdex.Security.RNG?label=Litdex.Security.RNG)](https://www.nuget.org/packages/Litdex.Security.RNG)

# Example

Get 10 random post from [danbooru.donmai.us](https://danbooru.donmai.us/).
```C#
var client = new DanbooruDonmai();
var posts = await client.GetRandomPostAsync(10);
foreach (var post in posts)
{
    Console.WriteLine($"Id: { post.ID }");
    Console.WriteLine($"File url: { post.FileUrl }");
}
```

# Documentation

For documentation: [Wiki]()

For how to use: [how to use]()

# Donation

Like this library? Please consider donation

[![ko-fi](https://www.ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/X8X81SP2L)
