[![CI](https://github.com/AriYeSiX/GameBackend/actions/workflows/ci.yml/badge.svg)](https://github.com/AriYeSiX/GameBackend/actions/workflows/ci.yml)
# GameBackend

## Производительность лидербордов

В корне репозитория лежит benchmark.ps1, который можно использовать для замера скорости выполнения запросов к бд. Команда для вызова через терминал: ".\benchmark.ps1 -Token "%Токен аутентификации пользователя%""

Сравнение реализаций `ILeaderboardRanking` на запросе `GET /api/leaderboards/{key}/me`
для игрока в середине рейтинга. 100 запросов, медиана из трёх прогонов.

| Записей   | PostgreSQL | Redis  | Разница |
|-----------|------------|--------|---------|
| 100 000   | 5,4 мс     | 3,7 мс | ~1,5×   |
| 1 000 000 | 26,5 мс    | 3,2 мс | ~8×     |

Время PostgreSQL растёт вместе с размером таблицы (подсчёт записей выше игрока),
время Redis остаётся постоянным (Sorted Set, O(log N)).

Условия: локальная машина, PostgreSQL 17 и Redis 7 в Docker, API в режиме Release/Run без отладчика.

Повторить замер: `.\benchmark.ps1 -Token "<access-токен>"`