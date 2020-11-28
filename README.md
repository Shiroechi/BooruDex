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
Some client may not have some feature listed above, because there's no public API for it.

## Feature by template

Only GET method.

| Template | Artist API | Pool API | Post API | Tag API | Wiki API |
| --- | --- | --- | --- | --- | --- |
| Danbooru | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| Gelbooru | ❌ | ❌ | ✔️ | ✔️ | ❌ |
| Gelbooru beta 0.2 | ❌ | ❌ | ✔️ | ❌ | ❌ |
| Moebooru | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |

# Benchmark

This benchmark compare with other library, [BooruSharp](https://github.com/Xwilarg/BooruSharp)

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1198 (1909/November2018Update/19H2)
AMD FX-8800P Radeon R7, 12 Compute Cores 4C+8G, 1 CPU, 4 logical and 4 physical cores
.NET Core SDK=5.0.100
  [Host]    : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
  MediumRun : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|             Method |     Mean |    Error |   StdDev |      Min |      Max | Rank | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------- |---------:|---------:|---------:|---------:|---------:|-----:|------:|------:|------:|----------:|
| DanbooruBooruDexV1 | 410.5 ms | 17.26 ms | 23.62 ms | 380.5 ms | 465.0 ms |    1 |     - |     - |     - |  253.5 KB |
| DanbooruBooruSharp | 418.4 ms | 25.73 ms | 36.91 ms | 370.9 ms | 532.5 ms |    1 |     - |     - |     - | 344.31 KB |
| DanbooruBooruDexV2 | 438.7 ms | 56.41 ms | 80.90 ms | 383.5 ms | 720.6 ms |    1 |     - |     - |     - |  68.76 KB |

**Note**
*Speed or perfomace may not accurate because internet connection or server response*

All library retrieve 10 random post from [danbooru](https://danbooru.donmai.us/).

[BooruDexV1](https://www.nuget.org/packages/BooruDex/1.0.0) was more effecient in handling memory(RAM) usage than [BooruSharp](https://github.com/Xwilarg/BooruSharp). 
Both library use [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) for processing JSON response.

But `BooruDexV2` is more effecient after migrating from [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) to [System.Text.Json](https://www.nuget.org/packages/System.Text.Json). The memory(RAM) usage is more reduced. Compared from `v1` and `v2`, `v2` is more smaller al least 3 times than `v1`.

Refer to this [repository](https://github.com/Shiroechi/BooruDex.Test) for the benchmark log and source code.

# Download

[![Nuget](https://img.shields.io/nuget/v/BooruDex?label=BooruDex)](https://www.nuget.org/packages/BooruDex)
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

More example can be found [here](https://github.com/Shiroechi/BooruDex/wiki/Example).

# Documentation

For documentation: [Wiki](https://github.com/Shiroechi/BooruDex/wiki)

# Donation

Like this library? Please consider donation

[![ko-fi](https://www.ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/X8X81SP2L)
