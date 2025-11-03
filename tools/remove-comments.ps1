# Removes single-line (//), XML doc (///) and block (/* */) comments from C# files under Assets/_Scripts
# WARNING: naive removal may affect strings containing // or /* */. Review changes after running.

$root = Join-Path (Get-Location) "Assets/_Scripts"
if (-Not (Test-Path $root)) { Write-Output "Path not found: $root"; exit1 }

Get-ChildItem -Path $root -Recurse -Filter *.cs | ForEach-Object {
 $file = $_.FullName
 Write-Output "Processing $file"
 $text = Get-Content -Raw -Encoding UTF8 $file

 # Remove block comments (/* ... */) - singleline and multiline
 $text = [regex]::Replace($text, '/\*.*?\*/', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)

 # Remove XML doc comments (///...) at line start
 $text = [regex]::Replace($text, '^[ \t]*///.*$', '', [System.Text.RegularExpressions.RegexOptions]::Multiline)

 # Remove single-line comments (//...) but avoid http:// and https:// by negative lookbehind
 # This is still heuristic and may remove comment-like content inside strings.
 $text = [regex]::Replace($text, '(?<!--)(?<!http:)(?<!https:)//.*$', '', [System.Text.RegularExpressions.RegexOptions]::Multiline)

 # Trim trailing whitespace on each line
 $lines = $text -split "\r?\n"
 $lines = $lines | ForEach-Object { $_.TrimEnd() }

 # Remove consecutive blank lines (more than2) to keep file tidy
 $outLines = @()
 $blankCount =0
 foreach ($ln in $lines) {
 if ([string]::IsNullOrWhiteSpace($ln)) { $blankCount++ } else { $blankCount =0 }
 if ($blankCount -le2) { $outLines += $ln }
 }

 $outText = ($outLines -join "`n") + "`n"
 Set-Content -Path $file -Value $outText -Encoding UTF8
}

Write-Output "Done. Please review changes in git or editor before committing."