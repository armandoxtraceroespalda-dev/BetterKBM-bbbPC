# VGAudio ATRAC9 decoder subset

Source: https://github.com/Thealexbarney/VGAudio
Pinned revision: 9d8f6ea04c83cccccb3dd7851a631bbd53a8dbbe
Author: Alex Barney. License: MIT (see LICENSE).

Only ATRAC9 decoding and the utility files it needs are included. The unused
Atrac9Parameters class is omitted. Sources retain their original namespaces.
This project targets .NET 8 and has no package dependencies.

Local compatibility change: Utilities/ArrayUnpacker.cs uses ReadExactly instead
of a single DeflateStream.Read. .NET 6+ allows partial reads, which otherwise
leave decoder tables incomplete and fail type initialization. No codec tables,
transform math or game data are changed. This dependency is used in the isolated
sound-extraction worker, and is not initialized in normal cached UI playback.
