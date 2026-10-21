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

## Сервісний шар (ЛР5): IOrderStore + OrderService

Інтерфейс src/Core/Abstractions/IOrderStore.cs: List(), GetById(id), Add(item),
Update(item), Remove(id).

Дві реалізації (src/Core/Storage/):
- InMemoryOrderStore — Dictionary<string, Order> у пам'яті, дані зникають після
  завершення процесу; заповнюється початковими даними з SampleData.Orders().
- FileOrderStore — кеш у пам'яті + серіалізація у JSON (через OrderDto) у файл
  data/catalog.json; дозавантаження при першому зверненні, запис на диск після
  кожної зміни (Add/Update/Remove).

Сервіс src/Core/Services/OrderService.cs залежить лише від IOrderStore (не від
конкретної реалізації): CreateOrder(customerId), AddLine(orderId, ...),
Confirm(orderId), All(), Find(id).

Composition root — src/Cli/Program.cs, блок --store: тут і лише тут
створюються конкретні класи (InMemoryOrderStore / FileOrderStore) залежно від
прапорця --file.

Запуск:
    dotnet run --project src/Cli -- --store          # InMemoryOrderStore
    dotnet run --project src/Cli -- --store --file    # FileOrderStore

Схема залежностей: Cli -> OrderService -> IOrderStore -> InMemoryOrderStore | FileOrderStore

## Додаткові завдання (ЛР5)

1. **CachingOrderStore** (src/Core/Storage/CachingOrderStore.cs) — декоратор над
   IOrderStore: кешує результат List(), скидає кеш при Add/Update/Remove.
2. **OrderService.Search(Func<Order, bool>)** — пошук за довільним предикатом
   без розширення самого інтерфейсу IOrderStore.
3. **OrderStoreFactory.Create(args)** (src/Core/Storage/OrderStoreFactory.cs) —
   винесена з Program.cs логіка вибору реалізації (--file, --cache). На
   відміну від варіанта "усе в Program.cs" (блок --store), фабрика дозволяє
   повторно використати ту саму логіку вибору сховища з кількох місць
   (наприклад, з тестів) без копіювання тернарного виразу.

Запуск: dotnet run --project src/Cli -- --store-advanced [--file] [--cache]

## Доменні події (ЛР6): OrderService.Changed

Подія: event EventHandler<OrderChangedEventArgs>? Changed у src/Core/Services/OrderService.cs.
Види змін (OrderChangeKind): Created, LineAdded, Confirmed — підіймаються лише
після успішної зміни стану (OnChanged викликається останнім рядком методу,
після _store.Update/_store.Add).

Дані події (OrderChangedEventArgs): OrderId, Kind, LinesCount, Total, At.

Підписники в Cli (--events):
- іменований локальний метод OnOrderChanged — виводить рядок у консоль і дописує
  у logs/app.log;
- лямбда-лічильник (_, _) => changes++ — демонструє замикання над зовнішньою
  змінною.

Відписка service.Changed -= OnOrderChanged демонструється явно: після неї консоль
і лог мовчать, але лямбда-лічильник і далі рахує події.

Майбутні підписники: на тижні 10 — логер проєкту Api, на тижні 12 — компонент
Blazor, що оновлюватиме таблицю замовлень. Жодних змін у Core для цього не
знадобиться, бо сервіс лише піднімає подію, не знаючи, хто і як її обробляє.

Запуск: dotnet run --project src/Cli -- --events

## Додаткові завдання (ЛР6)

1. **INotificationSink / ConsoleSink / FileSink** (src/Core/Notifications/) —
   альтернативний (не через event) спосіб сповіщення: OrderService отримує
   необов'язковий INotificationSink через конструктор (ручний DI тижня 5) і
   викликає sink.Notify(...) синхронно з OnChanged.

   Порівняння з подією (Changed):
   - event дозволяє підписати будь-яку кількість обробників (0, 1, багато),
     легко відписатися через -=; sink — завжди рівно один конкретний об'єкт,
     підміна можлива лише через новий new OrderService(...) у composition root.
   - event тримає Core повністю чистим від залежностей на спосіб виводу;
     ConsoleSink/FileSink — явні, тестовані класи, тому ConsoleSink.Notify
     навмисно викликає Console.WriteLine (єдине усвідомлене виключення з
     правила "Core не пише в консоль", бо це його єдине призначення).
   - sink зручніший, коли потрібен рівно один канал виводу, визначений наперед;
     event зручніший, коли кількість і тип підписників невідомі заздалегідь
     (саме тому в основній частині лабораторної обрано event).

   Запуск: dotnet run --project src/Cli -- --sink [--file-sink]

2. **Action<string> у ProductCsvImporter.Load(path, onSkipped)** — друге
   перевантаження методу з тижня 3: викликає переданий делегат одразу для
   кожного пропущеного рядка під час парсингу, а не лише в підсумковому
   ImportResult.Errors після завершення циклу.

   Запуск: dotnet run --project src/Cli -- --import-report
