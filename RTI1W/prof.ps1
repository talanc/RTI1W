dotnet build -c Release
if ($LASTEXITCODE -eq 0) {
    for ($i = 1; $i -le 10; $i++) {
        (Measure-Command { .\bin\Release\net10.0\RTI1W.exe --threads 1 --seed 10 }).TotalSeconds
    }
}