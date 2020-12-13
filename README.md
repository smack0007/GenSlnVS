# SlnGenVS

Creates Visual Studio solution files based on the directory structure.

## Why?

Over the years I have developed a way of structuring my projects and the
associated solution file in the root of the repository. I finally got tired
of managing the solution files by hand and decided to write this tool to
generate the solution files for me.

The solution files are generated based on the following rules:

1. There always exists a solution folder as the root node that
is the same name as the repository.
2. Under that node mirror the directory structure by adding in
solution folders / solution items as needed.
3. Ignore `.git`, `.vs`, `bin`, `obj`, and `ext` directories and
also don't add other solution files found.
4. If a directory contains a `csproj` file don't add it as a solution
folder and add the `csproj` file instead.

There exists an option in Visual Studio called Folder View. Although
it may seem like this tool could be completely replaced by that feature
as of this writing using the Folder View is still not the same experience
as using the Soultion View. 

## Usage

First install the tool globally:

```cmd
dotnet tool install -g slngenvs
```

After that just call the tool and provide it the path to the repository:

```cmd
slngenvs c:\path\to\the\repo
```

You can also just call the tool directly within the repository directory and
just use `.`:

```cmd
slngenvs .
```

## Possible Improvments

The tool was written in a few hours based on how I like to structure my
projects. There are few ways this project could be improved if anyone is
looking to contribute.

- Make the folders / extensions to ignore configurable. I think it would
make the most sense for the settings to be stored in a file in the root
of the repository. `.slngenvs` maybe?

- Building on the previous point, `csproj` is also hardcoded currently.
This should be a list (`vbproj`, `fsproj`) and could also be made
configurable.