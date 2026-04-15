$file = "backend/AIService/Migrations/20260404181444_AddAIAnalysisEnhancements.cs"
$content = [System.IO.File]::ReadAllText($file)
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$bytes = $utf8NoBom.GetBytes($content)
[System.IO.File]::WriteAllBytes($file, $bytes)
Write-Host "✅ UTF-8 BOM removed"

# Verify
$bytesCheck = [System.IO.File]::ReadAllBytes($file)
$first6 = $bytesCheck[0..5]
$hex = [BitConverter]::ToString($first6)
Write-Host "First 6 bytes (hex): $hex"
if ($hex.StartsWith("EF-BB-BF")) {
    Write-Host "❌ BOM still present!"
} else {
    Write-Host "✅ No BOM detected (starts with 'using' bytes)"
}
