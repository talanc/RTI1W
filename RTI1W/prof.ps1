dotnet build -c Release
if ($LASTEXITCODE -eq 0) {
    Write-Output "BVH=Tree"
    for ($i = 1; $i -le 10; $i++) {
        (Measure-Command { .\bin\Release\net10.0\RTI1W.exe --threads 1 --seed 10 --bvh Tree }).TotalSeconds
    }

    Write-Output "BVH=Linear"
    for ($i = 1; $i -le 10; $i++) {
        (Measure-Command { .\bin\Release\net10.0\RTI1W.exe --threads 1 --seed 10 --bvh Linear }).TotalSeconds
    }
}