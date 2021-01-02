# BooruDex

Library to access the booru website using public API. 
This library only support GET method, it means only listing and searching only. 
For other method like POST, contributions are welcomed.

[![CodeFactor](https://www.codefactor.io/repository/github/shiroechi/boorudex/badge?style=for-the-badge)](https://www.codefactor.io/repository/github/shiroechi/boorudex)

# Download

[![Nuget](https://img.shields.io/nuget/v/BooruDex?style=for-the-badge)](https://www.nuget.org/packages/BooruDex)

# Overview 

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

## Benchmark

This benchmark compare with other library, [BooruSharp](https://github.com/Xwilarg/BooruSharp)

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1256 (1909/November2018Update/19H2)
AMD FX-8800P Radeon R7, 12 Compute Cores 4C+8G, 1 CPU, 4 logical and 4 physical cores
.NET Core SDK=5.0.101
  [Host]    : .NET Core 5.0.1 (CoreCLR 5.0.120.57516, CoreFX 5.0.120.57516), X64 RyuJIT  [AttachedDebugger]
  MediumRun : .NET Core 5.0.1 (CoreCLR 5.0.120.57516, CoreFX 5.0.120.57516), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|     Method |     Mean |    Error |    StdDev |      Min |      Max | Ratio | RatioSD | Rank | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------- |---------:|---------:|----------:|---------:|---------:|------:|--------:|-----:|------:|------:|------:|----------:|
|   BooruDex | 502.0 ms | 85.06 ms | 124.68 ms | 371.9 ms | 861.2 ms |  1.00 |    0.00 |    1 |     - |     - |     - |  78.38 KB |
| BooruSharp | 506.2 ms | 54.29 ms |  76.11 ms | 374.3 ms | 690.9 ms |  1.07 |    0.33 |    1 |     - |     - |     - | 353.91 KB |

**Note**
*Speed or perfomace may not accurate because internet connection or server response*

This benchmark search 10 latest post from [danbooru](https://danbooru.donmai.us/).

As of version 2.2.0, [BooruDex](https://github.com/Shiroechi/BooruDex) changed all object models (Post, Tag, ..) from `struct` to `class`.
So the memory(RAM) usage increased a little, before it was about 68.76 KB. 

Refer to this [repository](https://github.com/Shiroechi/BooruDex.Test) for the benchmark log and source code.

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
