# Get all .csproj files
$projectFiles = Get-ChildItem -Path . -Filter *.csproj -Recurse

foreach ($file in $projectFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # Update TargetFramework
    $content = $content -replace '<TargetFramework>net9\.0</TargetFramework>', '<TargetFramework>net8.0</TargetFramework>'
    
    # Update package versions
    $content = $content -replace 'Version="9\.0\.\d+"', 'Version="8.0.0"'
    
    # Save the changes
    Set-Content -Path $file.FullName -Value $content
    
    Write-Host "Updated $($file.FullName)"
} 