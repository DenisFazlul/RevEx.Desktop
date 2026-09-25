# Дизайн-система RevEx Desktop

Все стили подключены глобально через `RevExStyles.axaml`. На экране не нужно
подключать отдельные словари: выбирайте семантический класс или тему и оставляйте
в представлении только структуру, текст и привязки.

## Быстрый выбор

| Нужно | Используйте |
| --- | --- |
| Основная кнопка | `Classes="primary"` |
| Вторичная кнопка | `Classes="secondary"` |
| Кнопка без постоянного фона | `Classes="ghost"` |
| Опасное действие | `Classes="danger"` |
| Кнопка только с иконкой | `Classes="icon"` |
| Маленькая кнопка с иконкой | `Classes="icon-sm"` |
| Стандартная карточка | `Classes="card section-card"` |
| Компактная карточка | `Classes="card card-compact"` |
| Приглушённая карточка | `Classes="card card-muted section-card"` |
| Тег | `Classes="tag"` |
| Рамка изображения | `Classes="image-frame"` |
| Многострочное поле | `Classes="multiline"` |
| Ошибка валидации | `Classes="validation-message"` |
| Нейтральный статус | `Classes="status-message"` |
| Заголовок страницы | `PageHeaderTheme` |
| Заголовок диалога | `DialogHeaderTheme` |
| Поле формы с подписью | `FormFieldTheme` |
| Подписанное значение | `InfoFieldTheme` |

Классы можно компоновать. Первый класс обычно описывает вид элемента, второй —
размер или поведение:

```xml
<Button Classes="icon" ToolTip.Tip="Редактировать">
    <PathIcon Data="..." />
</Button>
```

Размеры `Button` и `PathIcon` уже задаёт класс `icon`. Не добавляйте в экран
`Width`, `Height`, `MinHeight` и `Padding`, если стандартный размер подходит.

## Готовые примеры

### Кнопки

```xml
<StackPanel Orientation="Horizontal" Spacing="8">
    <Button Classes="primary" Content="Сохранить" Command="{Binding SaveCommand}" />
    <Button Classes="secondary" Content="Отмена" />

    <Button Classes="icon" ToolTip.Tip="Изменить">
        <PathIcon Data="M3,11 L3,14 L6,14 L14,6 L11,3 Z" />
    </Button>

    <Button Classes="icon danger" ToolTip.Tip="Удалить">
        <PathIcon Data="M3,4 L13,4 M5,4 L5,14 L11,14 L11,4" />
    </Button>
</StackPanel>
```

### Форма

`FormFieldTheme` сам создаёт подпись и правильный отступ до контрола:

```xml
<StackPanel Spacing="14">
    <HeaderedContentControl Theme="{StaticResource FormFieldTheme}" Header="Название">
        <TextBox Text="{Binding Name}" />
    </HeaderedContentControl>

    <HeaderedContentControl Theme="{StaticResource FormFieldTheme}" Header="Описание">
        <TextBox Classes="multiline" Text="{Binding Description}" />
    </HeaderedContentControl>

    <TextBlock Classes="validation-message" Text="{Binding ErrorMessage}" />
    <Button Classes="primary" Content="Сохранить" />
</StackPanel>
```

### Отображение информации

```xml
<HeaderedContentControl Theme="{StaticResource InfoFieldTheme}"
                        Header="Описание"
                        Content="{Binding Description}" />
```

### Карточка секции

```xml
<Border Classes="card section-card">
    <StackPanel Spacing="14">
        <TextBlock Classes="h2" Text="Параметры" />
        <HeaderedContentControl Theme="{StaticResource InfoFieldTheme}"
                                Header="Версия" Content="{Binding Version}" />
    </StackPanel>
</Border>
```

### Заголовок окна или страницы

```xml
<HeaderedContentControl Theme="{StaticResource PageHeaderTheme}"
                        Header="Каталог"
                        Content="Материалы и документы команды" />

<HeaderedContentControl Theme="{StaticResource DialogHeaderTheme}"
                        Header="Редактирование"
                        Content="Измените значение и нажмите «Сохранить»" />
```

### Полный минимальный экран

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Margin="32,30" RowDefinitions="Auto,22,*" MaxWidth="880">
        <HeaderedContentControl Theme="{StaticResource PageHeaderTheme}"
                                Header="Новый раздел"
                                Content="Краткое описание раздела" />

        <Border Grid.Row="2" Classes="card section-card">
            <StackPanel Spacing="14">
                <HeaderedContentControl Theme="{StaticResource FormFieldTheme}" Header="Название">
                    <TextBox Text="{Binding Name}" />
                </HeaderedContentControl>
                <Button Classes="primary" HorizontalAlignment="Left"
                        Content="Сохранить" Command="{Binding SaveCommand}" />
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

## Публичный API

### Типографика

- `h1`, `h2`, `label`, `caption`, `section-label`, `body-copy`.

### Кнопки

- Варианты: `primary`, `secondary`, `ghost`, `danger`.
- Размеры: `compact`, `icon`, `icon-sm`.
- Раскладка: `full-width`.

### Поверхности

- `card`, `section-card`, `card-compact`, `card-muted`.
- `value-box`, `tag`, `image-frame`, `content-row`.
- `side-panel`, `content-pane`, `section-separator`, `icon-tile`, `eyebrow`.

### Списки и поля

- `search`, `multiline`.
- `clean-list`, `category-list`, `content-list`.
- `validation-message`, `status-message`.

### Составные темы

- `PageHeaderTheme` — заголовок страницы.
- `DialogHeaderTheme` — заголовок диалога.
- `EyebrowHeaderTheme` — заголовок материала с бейджем.
- `FormFieldTheme` — подпись и произвольный контрол ввода.
- `InfoFieldTheme` — подпись и текстовое значение.
- `ContentRowTheme`, `DetailsCardTheme`, `ApiSettingsCardTheme` — специализированные компоненты.

Навигационные классы предназначены для оболочки приложения и обычно не нужны в
обычных экранах.

## Где менять систему

| Файл | Что в нём находится |
| --- | --- |
| `Tokens.axaml` | Цвета и глобальные ресурсы. |
| `Typography.axaml` | Текстовые классы. |
| `Controls.axaml` | Кнопки, поля, списки и статусы. |
| `Cards.axaml` | Поверхности, теги, изображения и разделители. |
| `Navigation.axaml` | Только оболочка главного окна. |
| `Templates.axaml` | Составные темы с внутренней разметкой. |

Правило: повторяются только свойства — добавьте класс; повторяется внутренняя
структура контролов — добавьте `ControlTheme`. Цвета берите из `Tokens.axaml`.

После изменения дизайн-системы выполните:

```bash
dotnet build RevEx.Desktop.sln --no-restore --disable-build-servers -m:1
```
