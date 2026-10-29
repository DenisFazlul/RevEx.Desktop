# Общая модель RevEx

Сущности, HTTP DTO, enum протокола коннекторов и имена ролей находятся
в отдельном репозитории [RevEx.Domain](https://github.com/DenisFazlul/RevEx.Domain).
Клонируйте его рядом с этим репозиторием под именем RevEx.Domain.
Исходники: src/RevEx.Domain. Поддерживаются .NET Framework 4.7 и современный .NET.

Локальная сборка использует ProjectReference. Для сборки с пакетом используйте
-p:UseRevExDomainPackage=true и предварительно доступный пакет RevEx.Domain 1.0.0.
Правила структуры, упаковка и проверки описаны в README репозитория Domain.

Пространства имён DTO: RevEx.Domain.Contracts.<предметная область>.
Контракты коннекторов: RevEx.Domain.Contracts.Connectors.
Сущности: RevEx.Domain.Entities. Роли: RevEx.Domain.Security.
Не создавайте локальные копии этих типов. UI, транспорт и хранение остаются
в соответствующих приложениях.

CI установщиков получает приватные зависимости через actions/checkout.
В настройках GitHub Actions нужен REVEX_DEPENDENCIES_TOKEN с чтением
репозиториев RevEx.Domain и RevEx.Connecter. Токен в исходниках не хранится.

Проверки интерфейса без открытия окон и входа в аккаунт:

dotnet run --project tests/DomainUiChecks -c Release

Межпроцессная проверка Framework/современного .NET описана в README Domain.
