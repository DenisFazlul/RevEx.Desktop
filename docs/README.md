# Документация RevEx Desktop

Сверено с исходным кодом 20.10.2026.

| Раздел | Содержание |
| --- | --- |
| [Архитектура](ARCHITECTURE.md) | Проекты solution, запуск, MVVM, работа с API и роли. |
| [Запуск и настройки](OPERATIONS.md) | Требования, адреса, OIDC, пользовательские настройки и диагностика. |
| [Коннекторы](CONNECTORS.md) | Регистрация, HTTP-маршруты, очередь загрузок и mock. |
| [Обновления и поставка](UPDATES.md) | Стартовая политика backend, Velopack, подтверждение установки и CI. |
| [Разработка](DEVELOPMENT.md) | Сборка, проверки, публикация и ограничения интеграций. |

Серверный проект: [RevEx.Web](https://github.com/DenisFazlul/RevEx.Web),
включая `docs/API.md`, `docs/openapi.json` и `docs/DESKTOP_RELEASES.md`.
Плагин: [RevEx.RevitPlugin](https://github.com/DenisFazlul/RevEx.RevitPlugin).

В репозитории Desktop нет Dockerfile/Compose: Avalonia запускается на хосте.
Папка `docs` находится в корне рядом с solution, а общий Compose — в Web.
Резервные каталоги вроде `RevEx.Desktop.backup-*` не являются актуальной исходной базой.
