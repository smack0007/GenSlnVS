$gitStatus = git status -s;
if ($gitStatus) {
    echo "There are currently uncommitted changes.";
    return;
}

if (Test-Path -Path .\bin) {
    rm -r bin;
}

dotnet pack -c Release -nologo;

$nupkg = Get-ChildItem .\bin\Release\*.nupkg;
$nupkgPath = ".\bin\Release\" + $nupkg.Name;
$nupkgParts = $nupkg.Basename.Split(".");
$nupkgVersion = $nupkgParts[$nupkgParts.Length - 3] + "." + $nupkgParts[$nupkgParts.Length - 2] + "." + $nupkgParts[$nupkgParts.Length - 1];

$gitTag = git tag -l v$nupkgVersion;
if ($gitTag) {
    echo "There already exists a commit tagged with the version '$nupkgVersion'.";
    return;
}

dotnet nuget push $nupkgPath -s https://api.nuget.org/v3/index.json;

git tag -a v$nupkgVersion -m "Version $nupkgVersion";

git push "origin" --tags
