dotnet run --configuration Release -- --output image.ppm
if ($LASTEXITCODE -eq 0) {
	.\image.ppm
}