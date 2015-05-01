# HexEd

HexEd is a multi-platform (C#/Mono) GUI hex editor.

Features include:
 * Rapid navigation and editing of very large files
 * Configurable radix, word size, endian-ness
 * Unlimited, branching undo
 * Bookmarking
 * Structure definitions to automatically identify parts of files
 * Checksum & hash calculation (MD2/4/5, SHA1/256/384/512, numerous CRC variants)
 * Statistics calculation (character counts, entropy, etc.)
 * Python console

Current status: very alpha, don't trust it, backup files before editing!

Hasn't been tested on anything other than Linux/Mono recently.


## Building (with mono)
 1. git clone https://github.com/esar/hexed
 2. cd hexed
 3. git submodule init
 4. git submodule update
 5. xbuild hexed.sln

## Screenshot (May 2015)
![Screenshot](https://github.com/esar/hexed/blob/master/doc/hexed.jpg?raw=true)
