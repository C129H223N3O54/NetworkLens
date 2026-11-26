<#
.SYNOPSIS
    Verifies that every translation key used in code/XAML exists in Strings.cs.

.DESCRIPTION
    Missing keys render as "[Key]" in the UI. This check runs in CI so such
    gaps are caught before a release. Also reports duplicate keys.
    Exit code 1 on problems.
#>
param(
    [string]$Root = (Join-Path $PSScriptRoot '..' 'NetworkLens')
)

$ErrorActionPreference = 'Stop'
$stringsFile = Join-Path $Root 'Localization/Strings.cs'

# Keys defined in Strings.cs:  ["Key"] = ("de", "en"),
$definitions = Select-String -Path $stringsFile -Pattern '^\s*\["([A-Za-z0-9_]+)"\]\s*=' -AllMatches |
    ForEach-Object { $_.Matches[0].Groups[1].Value }

$defined   = [System.Collections.Generic.HashSet[string]]::new([string[]]$definitions)
$duplicates = $definitions | Group-Object | Where-Object Count -gt 1 | Select-Object -ExpandProperty Name

$missing = [System.Collections.Generic.List[string]]::new()

Get-ChildItem -Path $Root -Recurse -Include *.cs, *.xaml |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Name -ne 'Strings.cs' -and $_.Name -ne 'LocalizationManager.cs' } |
    ForEach-Object {
        $file = $_
        $text = Get-Content -Raw -Path $file.FullName
        $patterns = @(
            '\.T\("([A-Za-z0-9_]+)"\)',          # L.T("Key")
            'Binding \[([A-Za-z0-9_]+)\]'        # {Binding [Key], Source=...}
        )
        foreach ($p in $patterns) {
            foreach ($m in [regex]::Matches($text, $p)) {
                $key = $m.Groups[1].Value
                if (-not $defined.Contains($key)) {
                    $missing.Add("$($file.Name): $key")
                }
            }
        }
    }

Write-Host "Translation keys defined: $($defined.Count)"

$failed = $false
if ($duplicates) {
    Write-Host "Duplicate keys:" -ForegroundColor Red
    $duplicates | ForEach-Object { Write-Host "  $_" }
    $failed = $true
}
if ($missing.Count -gt 0) {
    Write-Host "Keys used but not defined:" -ForegroundColor Red
    $missing | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
    $failed = $true
}

if ($failed) { exit 1 }
Write-Host "All translation keys OK" -ForegroundColor Green
