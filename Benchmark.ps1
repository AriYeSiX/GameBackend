param(
    [Parameter(Mandatory)] [string] $Token,
    [int] $Requests = 100
)

$url = "http://localhost:5165/api/leaderboards/classic/me"
$headers = @{ Authorization = "Bearer $Token" }

# Warm-up: the first request is not measured (in Redis mode it loads the leaderboard)
Invoke-RestMethod $url -Headers $headers | Out-Null

$times = 1..$Requests | ForEach-Object {
    (Measure-Command { Invoke-RestMethod $url -Headers $headers | Out-Null }).TotalMilliseconds
}

$sorted = $times | Sort-Object
"Requests: $Requests"
"Average: {0:N2} ms" -f ($times | Measure-Object -Average).Average
"Median:  {0:N2} ms" -f $sorted[[int]($Requests * 0.5)]
"p95:     {0:N2} ms" -f $sorted[[int]($Requests * 0.95) - 1]