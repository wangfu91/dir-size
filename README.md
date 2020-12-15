# dir-size
A simple dotnet global tool to calculate the size of each sub-dir.

## Install:

```bash
dotnet tool install --global dir-size
```

## Usage:

```bash
Usage: dir-size [options]

Options:
  -v|--version  Show version information
  -?|-h|--help  Show help information
  -d|--dir      The directory to work with, default is the current dir
  -s|--sort     Sort by size
```

### Example:

Current directory:
```bash
dir-size
```
Specify the target directory
```bash
dir-size -d path/to/dir
```
Sort by size:
```bash
dir-size -s -d path/to/dir
```
