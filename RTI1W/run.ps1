dotnet run --configuration Release | Out-File -FilePath image.ppm -Encoding Ascii
if ($LASTEXITCODE -eq 0) {
	.\image.ppm
}