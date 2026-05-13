$tag = git describe --tags --abbrev=0 2>$null

if (-not $tag) {
    # タグがない場合はデフォルト
    $tag = "0.0.0-dev"
}

Write-Output $tag
