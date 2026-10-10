param(
    [Parameter(Mandatory)] [string] $Token,
    [int] $Requests = 100
)

$url = "http://localhost:5165/api/leaderboards/classic/me"
$headers = @{ Authorization = "Bearer $Token" }

# Прогрев: первый запрос не считаем (в Redis-режиме он загружает таблицу)
Invoke-RestMethod $url -Headers $headers | Out-Null

$times = 1..$Requests | ForEach-Object {
    (Measure-Command { Invoke-RestMethod $url -Headers $headers | Out-Null }).TotalMilliseconds
}

$sorted = $times | Sort-Object
"Запросов: $Requests"
"Среднее: {0:N2} мс" -f ($times | Measure-Object -Average).Average
"Медиана: {0:N2} мс" -f $sorted[[int]($Requests * 0.5)]
"p95:     {0:N2} мс" -f $sorted[[int]($Requests * 0.95) - 1]