$c = Get-Content reference_app.js -Raw
$index = $c.IndexOf('Register as Vendor')
if ($index -ge 0) {
    $start = [Math]::Max(0, $index - 1000)
    $len = [Math]::Min(2000, $c.Length - $start)
    $c.Substring($start, $len) | Out-File -Encoding utf8 snippet_vendor_link.txt
}
else {
    "Not Found" | Out-File -Encoding utf8 snippet_vendor_link.txt
}
