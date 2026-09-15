# Стили интерфейса RevEx Desktop

Эта папка содержит общую дизайн-систему Avalonia-приложения. Экраны должны описывать
структуру, данные и привязки, а цвета, шрифты, отступы и повторяющуюся разметку следует
задавать здесь.

Все файлы подключаются через `RevExStyles.axaml`, который, в свою очередь, подключён в
`App.axaml`. Поэтому классы и темы доступны во всех `UserControl` и `Window` приложения
без дополнительных импортов.

## Структура папки

| Файл | Ответственность |
| --- | --- |
| `RevExStyles.axaml` | Общая точка входа. Подключает остальные файлы в правильном порядке. |
| `Tokens.axaml` | Палитра и общие дизайн-токены. |
| `Typography.axaml` | Заголовки, подписи и основной текст. |
| `Controls.axaml` | Кнопки, поля ввода, списки и простые контролы. |
| `Cards.axaml` | Карточки, рамки, бейджи, разделители и иконки. |
| `Navigation.axaml` | Левое меню, верхняя полоса вкладок и элементы оболочки приложения. |
| `Templates.axaml` | Составные компоненты на основе `ControlTheme` и шаблонов контролов. |

Порядок подключения важен: сначала загружаются токены, затем использующие их стили,
а составные шаблоны подключаются последними.

## Дизайн-токены

Токены находятся в `Tokens.axaml`. Их следует использовать вместо цветов, прописанных
непосредственно в экране.

| Ресурс | Назначение |
| --- | --- |
| `CanvasBrush` | Общий фон окна и рабочей области. |
| `SurfaceBrush` | Основная поверхность карточек и панелей. |
| `SurfaceMutedBrush` | Второстепенная нейтральная поверхность. |
| `SurfaceHoverBrush` | Состояние наведения на интерактивный элемент. |
| `SurfaceSelectedBrush` | Выбранный элемент или активная вкладка. |
| `SidebarBrush` | Фон панели навигации и полосы вкладок. |
| `TextPrimaryBrush` | Основной цвет текста. |
| `TextSecondaryBrush` | Подписи и второстепенный текст. |
| `TextTertiaryBrush` | Самые тихие подписи и заголовки групп. |
| `BorderBrush` | Границы карточек и разделители. |
| `BorderStrongBrush` | Активные границы полей и интерактивных поверхностей. |
| `AccentBrush` | Основной акцентный цвет. |
| `AccentHoverBrush` | Акцентная кнопка при наведении. |
| `AccentSoftBrush` | Светлый акцентный фон бейджей и иконок. |
| `DangerBrush` | Опасные действия и ошибки. |
| `DangerSoftBrush` | Фон опасного состояния. |
| `SuccessBrush` | Положительное состояние подключения. |

Пример использования ресурса в новом стиле:

```xml
<Setter Property="Background" Value="{StaticResource SurfaceBrush}" />
```

Если нужно изменить цвет во всём приложении, меняйте соответствующий токен, а не каждый
экран отдельно.

## Использование классов

Класс применяется через свойство `Classes`:

```xml
<TextBlock Classes="h1" Text="Каталог" />
```

Несколько классов можно комбинировать через пробел:

```xml
<Border Classes="card section-card">
    <!-- Содержимое секции -->
</Border>
```

В этом примере `card` задаёт поверхность, границу и скругление, а `section-card` —
внутренние отступы конкретного вида карточки.

### Типографика

| Класс | Контрол | Назначение |
| --- | --- | --- |
| `h1` | `TextBlock` | Главный заголовок страницы. |
| `h2` | `TextBlock` | Заголовок секции или карточки. |
| `caption` | `TextBlock` | Пояснение и второстепенный текст. |
| `label` | `TextBlock` | Подпись поля формы. |
| `section-label` | `TextBlock` | Компактный заголовок группы. |
| `body-copy` | `TextBlock` | Основной текст материала с комфортной высотой строки. |
| `row-arrow` | `TextBlock` | Стрелка перехода в строке списка. |
| `menu-card-title` | `TextBlock` | Заголовок карточки главного меню. |

```xml
<StackPanel Spacing="5">
    <TextBlock Classes="h1" Text="Настройки" />
    <TextBlock Classes="caption" Text="Параметры рабочего пространства" />
</StackPanel>
```

### Кнопки и поля

| Класс | Контрол | Назначение |
| --- | --- | --- |
| `primary` | `Button` | Основное действие экрана. |
| `ghost` | `Button` | Второстепенное действие без постоянного фона. |
| `menu-card` | `Button` | Готовая карточка перехода на главном экране. |
| `search` | `TextBox` | Поле поиска. |

```xml
<Button Classes="primary"
        Content="Сохранить"
        Command="{Binding SaveCommand}" />

<Button Classes="ghost" Content="Отмена" />

<TextBox Classes="search"
         Watermark="Поиск…"
         Text="{Binding SearchQuery}" />
```

Карточка меню получает название через `Content`, а переход — через `Command`:

```xml
<Button Classes="menu-card"
        Content="Каталог"
        Command="{Binding OpenCatalogCommand}" />
```

### Карточки и декоративные элементы

| Класс | Контрол | Назначение |
| --- | --- | --- |
| `card` | `Border` | Базовая поверхность карточки. |
| `section-card` | `Border` | Стандартные отступы карточки секции. |
| `details-card` | `Border` | Отступы карточки подробной информации. |
| `settings-card` | `Border` | Отступы карточки настроек. |
| `content-row` | `Border` | Поверхность строки материала. |
| `value-box` | `Border` | Поле для отображения значения настройки. |
| `icon-tile` | `Border` | Акцентная подложка для иконки. |
| `eyebrow` | `Border` | Небольшой акцентный бейдж над заголовком. |
| `section-separator` | `Border` | Горизонтальный разделитель. |
| `status-dot` | `Ellipse` | Зелёный индикатор состояния. |

```xml
<Border Classes="card section-card">
    <TextBlock Classes="h2" Text="Параметры" />
</Border>
```

### Списки

| Класс | Назначение |
| --- | --- |
| `clean-list` | Убирает стандартный фон и границу `ListBox`. |
| `category-list` | Задаёт расположение элементов списка категорий. |
| `content-list` | Задаёт расположение карточек материалов. |

Классы списков обычно используются вместе:

```xml
<ListBox Classes="clean-list content-list"
         ItemsSource="{Binding Contents}" />
```

### Навигация

| Класс | Контрол | Назначение |
| --- | --- | --- |
| `app-toolbar` | `Border` | Нейтральная верхняя панель вспомогательных окон. |
| `brand-mark` | `Border` | Контейнер логотипа RevEx. |
| `workspace-sidebar` | `Border` | Постоянная левая панель рабочего пространства. |
| `sidebar-menu-button` | `Button` | Постоянный пункт основного меню слева. |
| `workspace-header` | `Border` | Верхняя полоса рабочих вкладок. |
| `top-tab-button` | `Button` | Заголовок рабочей вкладки справа. |
| `tab-close` | `Button` | Компактное закрытие рабочей вкладки. |

Эти классы в основном используются в `MainWindow.axaml`. Для обычного экрана менять их
не требуется.

Левая панель содержит постоянное основное меню и не входит в коллекцию вкладок.
Открываемые из него разделы отображаются горизонтальными вкладками над правой рабочей
областью. `MainViewModel` и `WorkspaceTabFactory` остаются единой точкой открытия и
повторной активации вкладок. Поддерживаются привычные desktop-сочетания: `Ctrl/Cmd+W` закрывает
текущую закрываемую вкладку, `Ctrl+Tab` и `Ctrl+Shift+Tab` переключают вкладки,
а `Ctrl/Cmd+B` скрывает или возвращает левую панель.

При запуске справа открывается каталог. Карточки каталога, редактор контента, теги,
настройки и будущие рабочие окна открываются в той же верхней полосе. Повторное открытие
одинакового `(TabType, Key)` активирует существующую вкладку вместо создания дубликата.

## Использование составных шаблонов

Составные шаблоны применяются через свойство `Theme`:

```xml
Theme="{StaticResource PageHeaderTheme}"
```

Они принимают данные через `Header` и `Content`, а всю внутреннюю разметку создают сами.

### `PageHeaderTheme`

Стандартный заголовок страницы с пояснением:

```xml
<HeaderedContentControl
    Theme="{StaticResource PageHeaderTheme}"
    Header="Каталог"
    Content="Материалы, инструкции и документы команды" />
```

### `EyebrowHeaderTheme`

Заголовок материала с акцентным бейджем:

```xml
<HeaderedContentControl
    Theme="{StaticResource EyebrowHeaderTheme}"
    Header="{Binding Title}"
    Content="Карточка материала из базы знаний" />
```

### `ContentRowTheme`

Строка материала с иконкой, описанием и стрелкой:

```xml
<HeaderedContentControl
    Theme="{StaticResource ContentRowTheme}"
    Header="{Binding Name}"
    Content="{Binding Description}"
    DoubleTapped="OnContentDoubleTapped" />
```

### `DetailsCardTheme`

Карточка подробного описания:

```xml
<HeaderedContentControl
    Theme="{StaticResource DetailsCardTheme}"
    Header="Описание"
    Content="{Binding Description}" />
```

### `ApiSettingsCardTheme`

Карточка адреса API с индикатором состояния:

```xml
<HeaderedContentControl
    Theme="{StaticResource ApiSettingsCardTheme}"
    Header="API-сервер"
    Content="{Binding ApiPath}" />
```

## Пример нового экрана

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Margin="40,36" RowDefinitions="Auto,24,*">
        <HeaderedContentControl
            Theme="{StaticResource PageHeaderTheme}"
            Header="Новый раздел"
            Content="Краткое описание раздела" />

        <Border Grid.Row="2" Classes="card section-card">
            <!-- Содержимое экрана -->
        </Border>
    </Grid>
</UserControl>
```

## Куда добавлять новый стиль

1. Новый цвет или глобальное значение — в `Tokens.axaml`.
2. Оформление текста — в `Typography.axaml`.
3. Кнопка, поле, список или простой контрол — в `Controls.axaml`.
4. Карточка, рамка, бейдж или декоративный элемент — в `Cards.axaml`.
5. Элемент главного окна или вкладок — в `Navigation.axaml`.
6. Компонент со своей внутренней разметкой — в `Templates.axaml`.

Если создаётся новый файл стилей, его необходимо добавить в `RevExStyles.axaml`.

## Правила развития дизайн-системы

- Не прописывайте цвета непосредственно в экранах. Используйте ресурсы из `Tokens.axaml`.
- Не копируйте наборы одинаковых `Setter` между файлами. Создайте семантический класс.
- Если повторяется целый фрагмент разметки, создайте `ControlTheme` в `Templates.axaml`.
- Называйте классы по назначению (`content-row`), а не по внешнему виду (`gray-border`).
- В экранах оставляйте структуру, тексты, команды, события и привязки к данным.
- После изменения стилей проверяйте проект командой `dotnet build RevEx.Desktop.sln`.
