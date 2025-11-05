# BitCheck Help Output Preview

When you run `BitCheck` or `BitCheck --help`, you'll now see:

```
Description:

  ____  _ _    ____ _               _    
 | __ )(_) |_ / ___| |__   ___  ___| | __
 |  _ \| | __| |   | '_ \ / _ \/ __| |/ /
 | |_) | | |_| |___| | | |  __/ (__|   < 
 |____/|_|\__|\____|_| |_|\___|\___|_|\_\
                                          
 The simple and fast data integrity checker!
 Detect bitrot and file corruption with ease.

 GitHub: https://github.com/alanbarber/bitcheck

Usage:
  BitCheck [options]

Options:
  -r, --recursive  Recursively process all files in sub-folders
  -a, --add        Add new files to the database
  -u, --update     Update any existing hashes that do not match
  -c, --check      Check existing hashes match
  -v, --verbose    Verbose output
  --version        Show version information
  -?, -h, --help   Show help and usage information
```

## ASCII Art Details

The banner spells out "BitCheck" in a stylized font called "Standard" which is:
- Clean and professional
- Easy to read
- Works well in all terminals
- Not too large (5 lines tall)

## Alternative Designs

If you want a different style, here are some alternatives:

### Option 2: Block Letters
```
 _____ _____   _____ _____ _____ _____ _____ _____ 
| __  |     | |     |  |  |   __|     |  |  |
| __ -|-   -| |   --|     |   __|   --|    -|
|_____|_____| |_____|__|__|_____|_____|__|__|
```

### Option 3: Banner Style
```
#####  ### #####  ####  #   # ###### ####  #   #
#   #   #    #   #    # #   # #     #    # #  #
#####   #    #   #      ##### ####  #      ###
#   #   #    #   #      #   # #     #      #  #
#####  ###   #    ####  #   # ###### ####  #   #
```

### Option 4: Slant
```
    ____  _ __  ________              __  
   / __ )(_) /_/ ____/ /_  ___  _____/ /__
  / __  / / __/ /   / __ \/ _ \/ ___/ //_/
 / /_/ / / /_/ /___/ / / /  __/ /__/ ,<   
/_____/_/\__/\____/_/ /_/\___/\___/_/|_|  
```

### Option 5: Mini (Compact)
```
 ___ _ _   ___ _         _   
| _ |_) |_/ __| |_  ___ | |__
| _ \ |  _| (__| ' \/ -_)| / /
|___/_|\__\___|_||_\___||_\_\
```

## Current Choice

The current implementation uses the "Standard" font (Option 1) because it:
- ✅ Looks professional
- ✅ Is highly readable
- ✅ Works in all terminals (no special characters)
- ✅ Is the right size (not too big, not too small)
- ✅ Matches the clean, technical nature of the tool

## How to Change

If you want a different style, just replace the ASCII art in the `rootCommand.Description` in `Program.cs` (lines 40-48).
