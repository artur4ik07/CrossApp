\# CrossApp
Наскрізний проєкт з крос-платформного програмування.
Предметна область: Замовлення. Сутності: Customer, Product, Order, OrderLine.
Призначення: оформлення замовлень і підрахунок сум.



\## Структура solution



```

CrossApp/

├── CrossApp.sln

├── README.md

├── .gitignore

└── src/

&#x20;   ├── Core/   (class library: EnvironmentInfo, EnvironmentReport)

&#x20;   └── Cli/    (консольний застосунок, ProjectReference на Core)

```



Залежність одностороння: Cli → Core. Core не посилається на Cli, щоб уникнути циклічної залежності і щоб згодом підключити Core до Api та Blazor.



Домовленість про каталоги в Core на весь семестр:

\- `Core/Dto/` – record-типи формату даних (тиждень 3)

\- `Core/Domain/` – сутності з поведінкою та інваріантами (тиждень 4)

\- `Core/Storage/` – реалізації сховищ (тиждень 5)



\## Запуск
dotnet build
dotnet run --project src/Cli

dotnet run --project src/Cli -- --json



\## Публікація



```

dotnet publish src/Cli -c Release -r win-x64 --self-contained true  -o publish/sc

dotnet publish src/Cli -c Release -r win-x64 --self-contained false -o publish/fd

.\\publish\\sc\\Cli.exe

.\\publish\\fd\\Cli.exe

```



\### Порівняння режимів (ЛР2)



| RID     | Режим               | Розмір publish | Файлів | Потрібен встановлений runtime |

|---------|---------------------|----------------|--------|-------------------------------|

| win-x64 | self-contained      | 74,50 МБ       | 191    | ні                            |

| win-x64 | framework-dependent | 0,18 МБ        | 7      | так (.NET 9)                  |



\*\*Self-contained\*\* містить у каталозі .NET Runtime, тому він великий і працює на комп'ютері без .NET, але створюється під конкретну RID.

\*\*Framework-dependent\*\* містить лише код застосунку та залежності, тому він малий, але потребує встановленого .NET Runtime відповідної версії.



\## Середовище
.NET SDK 9.0.100, Windows x64. Core збирається під net8.0 і net9.0 (multi-targeting), Cli під net9.0.



\## Додаткове завдання: Порівняння розмірів publish

\* \*\*win-x64\*\*: \~74.48 MB (78 097 005 байт)

\* \*\*linux-x64\*\*: \~76.00 MB (79 640 536 байт)\[cite: 1]

## lab1 test


## Доменна модель (ЛР4): Order + OrderLine

Сутності в src/Core/Domain/. Order — агрегат з інкапсульованою колекцією рядків,
створюється лише через фабричний метод Order.Create(), конструктор приватний.

Інваріанти:

| # | Правило                                                  | Тип винятку                   | Метод           |
|---|-----------------------------------------------------------|--------------------------------|------------------|
| 1 | Id замовлення не порожній                                  | ArgumentException              | Order.Create     |
| 2 | CustomerId не порожній                                     | ArgumentException              | Order.Create     |
| 3 | ProductId рядка не порожній                                | ArgumentException              | Order.AddLine    |
| 4 | ProductName рядка не порожня                               | ArgumentException              | Order.AddLine    |
| 5 | Кількість у рядку > 0                                       | ArgumentOutOfRangeException    | Order.AddLine    |
| 6 | Ціна у рядку >= 0                                           | ArgumentOutOfRangeException    | Order.AddLine    |
| 7 | Не можна додати рядок до підтвердженого замовлення          | InvalidOperationException      | Order.AddLine    |
| 8 | Не можна підтвердити замовлення без жодного рядка            | InvalidOperationException      | Order.Confirm    |
| 9 | Не можна підтвердити вже підтверджене замовлення             | InvalidOperationException      | Order.Confirm    |

Мапінг Order <-> OrderDto реалізовано через ToDto()/FromDto(); FromDto відновлює
сутність через Create+AddLine+Confirm, тому проходить ті самі перевірки, що й
при першому створенні.
