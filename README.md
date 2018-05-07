# BuildTask

Helper executable to ease the build task under VS Code.

## Disclaimer

Currently, many assumptions I made work only on my PC.

- Visual Studio 2017 (or Preview) Communuty (or Professional) Edition should be installed in `C:\Program Files (x64)\Microsoft Visual Studio\...` to be able to use **MSVC**. The preview version has priority over the stable version if available. The professional edition has priority over the community edition if available.
- LLVM Clang 6.0 should be installed and set in the path to be able to use **Clang**.

## Arguments

| Commandline argument   | Description         | Clang                               | MSVC
|------------------------|---------------------|-------------------------------------|-------------------------------------------
| -clang                 | use clang           | -Xclang -flto-visibility-public-std | /EHsc<br>/permissive-<br>nologo<br>/Foobj
| -msvc                  | use msvc            |
| -debug                 |                     | -DDEBUG=1<br>-O0                    | /DDEBUG=1<br>/Zi<br>/Od
| -ndebug                |                     | -DDEBUG=0<br>-DNDEBUG<br>-O3        | /DDEBUG=0<br>/DNDEBUG<br>/Ox
| -output \<filepath>    |                     | -o \<filepath>                      | /Fe\<filepath>
| -force                 | force recompilation |
| -warnings_are_errors   |                     | -Werror                             | /WX
|_-warning_level <level>_|_not yet implemented_| see table below                     | see table below
|_-std c++xy_            |_not yet implemented_|

> The ` ` between parameter and its value should soon be replacable by `:` or `=`.

For now warning level is set to "high"

| Warning Levels          | clang                   | MSVC
|-------------------------|-------------------------|-------
| none                    |                         | /W0
| severe                  | -Wall                   | /W1
| significant             | -Wall -pedantic         | /W2
| production              | -Wall -pedantic         | /W3
| informational (default) | -Wall -pedantic -Wextra | /W4
| nighmare                | -Wall -pedantic -Wextra | /Wall