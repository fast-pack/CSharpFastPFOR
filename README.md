CSharpFastPFOR: A C# port of the simple integer compression library JavaFastPFOR
==========================================================

Why?
----
While building a in-memory compressed datastructure, I needed a library for efficient integer compression. JavaFastPFOR was what I needed, so I ported it to C#.
See the original project by Daniel Lemire here: https://github.com/fast-pack/JavaFastPFOR

Requirements
------------
* .NET Core 2.0 SDK
* Visual Studio 2019

API
-----------------
The API is excatly the same as JavaFastPFOR. See the documentation here: http://www.javadoc.io/doc/me.lemire.integercompression/JavaFastPFOR/

This port was checked for parity against JavaFastPFOR version 0.3.14, commit [`a6518ff792317dc029530cdf4e05af3c4e4fe19e`](https://github.com/fast-pack/JavaFastPFOR/commit/a6518ff792317dc029530cdf4e05af3c4e4fe19e).

References
-----------------

* Daniel Lemire and Leonid Boytsov, Decoding billions of integers per second through vectorization, Software Practice & Experience 45 (1), 2015.  http://arxiv.org/abs/1209.2137 http://onlinelibrary.wiley.com/doi/10.1002/spe.2203/abstract
* Daniel Lemire, Leonid Boytsov, Nathan Kurz, SIMD Compression and the Intersection of Sorted Integers, Software Practice & Experience (to appear) http://arxiv.org/abs/1401.6399
* Matteo Catena, Craig Macdonald, Iadh Ounis, On Inverted Index Compression for Search Engine Efficiency,  Lecture Notes in Computer Science 8416 (ECIR 2014), 2014.
http://dx.doi.org/10.1007/978-3-319-06028-6_30