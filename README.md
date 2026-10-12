# Mini Factory

Мобильный Unity-кликер: фабрика из 3 производственных машин, временный Boost, offline-прогресс, Unity IAP и аналитика. Тестовое задание (Unity Developer).

- Unity **6000.3.22f1**, платформа **Android**
- UI — встроенный `UnityEngine.UI` (legacy Text), без TMP
- Графическая часть не прорабатывалась: стандартный Unity UI и primitives

## Запуск

1. Unity Hub → импорт проекта `Mini_factory_game/` (открывать версией 6000.3.22f1).
2. Открыть сцену `Assets/Scenes/Main.unity` → Play.

Сцена в репозитории уже собрана и содержит весь контент (карточки, HUD, титульный экран) — дополнительно генерировать ничего не нужно.

## Android build

1. **File → Build Settings → Android → Switch Platform**.
2. **Project Settings → Player**: Orientation = Portrait, package name `com.minifactory.game`.
3. **File → Build Settings → Build** → выбрать путь, например `Builds/MiniFactory.apk`.
   - Debug-панель (5 тапов в правый верхний угол) компилируется только при включённом **Development Build** (галочка в Build Settings). Для release-сборки галочку снять.
4. Установить: `adb install Builds/MiniFactory.apk`.

**Проверялся build на эмуляторе BlueStacks** (Android 9, API 28). Graphics API — OpenGLES3: с Vulkan BlueStacks падал на старте (`SIGSEGV` в `UnityGfxDeviceW`), поэтому в Player Settings Android явно выставлен OpenGLES3 (`m_BuildTargetGraphicsAPIs`).

## Системы

### Фабрика и экономика (п.1)

- 3 машины: `press` (доступна сразу), `cutter` (50), `assembler` (500).
- Состояния `Locked/Unlocked`, уровень, производительность, стоимость открытия, стоимость следующего улучшения — всё в UI.
- Открытые машины автоматически производят валюту (тик `GameLoop`).
- Формулы:
  - производство: `base × level × growth^(level−1)`, `growth = 1.1`
  - стоимость улучшения: `upgradeCostBase × 1.15^level`

### Boost и offline progress (п.2)

- Boost умножает производство всей фабрики; duration/multiplier/enable — из конфига.
- Offline-доход считается при старте по фактическому времени отсутствия, огранич `maxOfflineSeconds`; активный Boost учитывает реально прошедшее время (остаток времени Boost не сгорает, а продолжает действовать, если не вышел за пределы offline-окна).
- Итог offline показывается в диалоге на титульном экране.
- Сохранение — JSON в `persistentDataPath/progress.json` (`JsonSaveService`), пишется при действиях игрока и сворачивании приложения.

### Конфигурация (п.3)

- `GameConfigAsset` (ScriptableObject): параметры машин, unlock/upgrade, Boost (`boostEnabled`, `boostDurationSeconds`, `boostMultiplier`), `maxOfflineSeconds`, UI-настройки, секция A/B.
- Игра читает конфиг только через `IGameConfigSource` → immutable `GameConfig`. Источники — декораторы:
  - `LocalConfigSource` (базовый, из ассета)
  - `ExperimentConfigSource` (A/B-оверрайды)
  - `RemoteConfigConfigSource` (значения из Remote Config поверх локальных)
- Замена/дополнение источника — без изменения игровой логики (п.3 ТЗ выполнено буквально: второй источник подключён тем же швом).

### A/B-тесты

- Вариант (A/B) назначается при первом запуске случайно и запоминается в сейве.
- **Переключается только при следующем запуске игры** (через debug-панель → рестарт): намеренно, через Firebase Remote Config A/B **не** управляется.
- Оверрайды варианта B охватывают **все** множители сразу: production, upgrade cost, unlock cost (секция `Experiment` в `GameConfigAsset`, каждый — свой bool + значение).
- Вариант A = контроль, оверрайды не применяются (`ExperimentConfigSource` становится прозрачным прокси).

### Unity IAP (п.4)

- `UnityIapService` (Unity IAP v5.4.4) реализует `IGamePurchaseService`; gameplay-код зависит только от интерфейса.
- Продукты: `coins_pack_small` (consumable, +1000 монет), `boost_pack` (consumable, +10 мин Boost).
- Обработаны: инициализация, получение продукта, успешная/неуспешная покупка, IAP недоступен.
- **Fake Store в редакторе** (`#if UNITY_EDITOR`); на устройстве при недоступном Store включается mock-режим, покупка эмулируется, показывается toast.

### Analytics (п.5)

- `AnalyticsService` принимает массив `IAnalyticsProvider` — несколько провайдеров одновременно.
- Обязательный `ConsoleAnalyticsProvider` (лог в консоль/логcat) включён всегда.
- События: `game_started`, `machine_unlocked`, `machine_upgraded`, `boost_started`, `boost_finished`, `offline_income_applied`, `purchase_succeeded`, `purchase_failed`, плюс `progress_restarted` — с параметрами (machine_id, level, cost, длительности и т.д.).
- Заготовка AppMetrica-провайдера есть, но выключена: SDK не устанавливался (см. «Ограничения»).

### Mobile lifecycle

- Сворачивание/возврат: тик останавливается (`GameLoop.Paused`), при возврате считается offline-доход и применяется.
- Автосейвы до инициализации на титульном экране отключены (иначе сворачивание затёрло бы старый сейв пустым).
- Титульный экран: «Продолжить» / «Начать сначала» (с подтверждением, стирает прогресс).

### Debug-панель

- Открывается 5 тапами в правый верхний угол, компилируется только под `#if UNITY_EDITOR || DEVELOPMENT_BUILD` (в release-сборке отсутствует).
- Кнопки: +монеты, +1 уровень / открыть первую машину (чит напрямую в модель, без списания), переключение A/B, вывод конфига в лог, рестарт прогресса.

### Мини-туториал

- Data-driven: шаги задаются в конфиге (`tutorialSteps` в `GameConfigAsset`), gameplay-код о шагах ничего не знает.
- Текущие шаги: 1) улучшить первую машину, 2) открыть первую закрытую машину.
- `TutorialDirector` (Core) подписан на события контроллера (`MachineUpgraded`/`MachineUnlocked`) и продвигает шаг при совпадении триггера (`UpgradeAny`/`UnlockAny`/`UpgradeMachine`/`UnlockMachine` + optional `machineId`).
- Цель подсветки резолвится селектором: `first` (первая машина), `firstLocked` (первая закрытая) или конкретный id — не хардкод id в логике.
- Прогресс — `tutorialStep` в сейве, переживает перезапуски; завершение = `CurrentStep == null` (шагов больше нет).
- Отображение — `TutorialOverlay`: затемнение с «дыркой» под целевую кнопку + текст подсказки.

## Архитектура

MVC + события, без синглтонов и DI-фреймворков. Composition root — `GameBootstrapper` (ручная сборка конструкторами, единственное место, где известны все реализации).

| Слой | asmdef | Содержимое |
|---|---|---|
| **Core** | `MiniFactory.Core` (`noEngineReferences`) | `FactoryModel`, `MachineState`, `GameController`, `EconomyService`, `OfflineCalculator`, `GameConfig`/`ExperimentConfig`, `JsonSaveService`, `AnalyticsService` — чистый C#, unit-тестируется без Unity |
| **App** | `MiniFactory.App` | `GameBootstrapper`, `GameLoop`, `GameConfigAsset`, `LocalConfigSource`/`FirebaseRemoteConfigService`, `UnityIapService`, `DebugPanelView` — Unity-адаптеры |
| **View** | (App/View) | `BalanceView`, `ProductionView`, `BoostView`, `MachineCardView`, `PurchaseView`, `TitleScreenView`, `TutorialOverlay` — подписка на события модели, вызовы контроллера |

- Связи: View → Controller (прямые вызовы), Model → View (события `event Action`), Controller ← сервисы только через интерфейсы `Core/Contracts` (`ISaveService`, `IAnalytics`, `IGamePurchaseService`, `IClock`, `IGameConfigSource`, `IRemoteConfigService`).
- Шов под Remote Config/A/B — `IGameConfigSource`; шов под IAP — `IGamePurchaseService`; шов под analytics — `IAnalyticsProvider`.

## Packages / SDK

- `com.unity.purchasing` 5.4.4 — Unity IAP
- `com.unity.test-framework` 1.5.1 — Edit Mode тесты
- `com.unity.ugui` 2.0.0
- Firebase Unity SDK 13.17.0 — **только Remote Config** (доп. задание): реальный `FirebaseRemoteConfigService` с try/catch и падением на локальный `LocalRemoteConfigService`, если Firebase не инициализировался
- Android Gradle-шаблоны (`mainTemplate` и др.) — для Firebase/EDM4U

## Тесты

39 Edit Mode тестов (12 файлов): экономика, offline-калькулятор, модель, контроллер, сейв, A/B-селектор и конфиг-декораторы, Remote Config, туториал, аналитика, UI-конфиг.

Запуск: **Window → General → Test Runner → EditMode → Run All**, либо батчем:

```
Unity.exe -batchmode -projectPath <path> -runTests -testPlatform EditMode -testResults results.xml
```

## Профилирование (базовое)

Development build на BlueStacks (Android 9), замеры через `dumpsys`:

| Метрика | Результат |
|---|---|
| FPS (SurfaceFlinger, покой) | **30.0 FPS**, интервалы кадров 31.8–34.6 мс |
| FPS под нагрузкой (40 апгрейдов подряд) | **30.0 FPS**, ни одного кадра >50 мс |
| Память (PSS) | 231 MB в покое → 245 MB после нагрузки |
| Java / Native heap | 9.2 MB / 24.5 MB |
| CPU | ~4% одного ядра |

- 30 FPS — дефолтный `targetFrameRate` Unity на мобильных платформах; просадок и всплесков нет (p99 = 34.6 мс).
- `dumpsys gfxinfo` для Unity бесполезен (рендер мимо HWUI) — кадры считались через `dumpsys SurfaceFlinger --latency`.
- Экономика/таймер — не CPU-bound: даже после серии апгрейдов нагрузка не изменилась.

## Известные ограничения / незавершённое

- **AppMetrica / Firebase Analytics не подключены** — заготовка провайдера есть (`#if MINIFACTORY_APPMETRICA`), SDK намеренно не ставился (AppMetrica: на момент работы сервис был выключен на их стороне).
- **Remote Config**: подключён к Firebase (параметры заданы в консольном проекте), есть fallback-цепочка — если Firebase падает, значения берутся из локального JSON.
- **Profiling** — только базовый (см. раздел выше): память и FPS через `dumpsys`. Глубокий профилильщик (Unity Profiler, CPU-маркеры, render debug) не запускался.
- Release-сборка не собиралась: последний APK собран как Development Build (нужен debug-панели).
- Debug-панель и A/B-переключатель — инструменты разработки, в release отсутствуют/не влияют на баланс игрока.
- Звук/анимации/нормальная арт-составляющая не делались (по ТЗ не оценивается).
- Локализации нет, UI-тексты на русском захардкожены.

## Фактическое время работы

Около 5 часов (лимит ТЗ) — распределение по этапам: окружение и ядро, gameplay/UI, IAP + analytics + тесты, Android/lifecycle, Remote Config + A/B (доп. задания), документация.

## Что осталось и следующий шаг

1. Подключить реальный Analytics Provider (Firebase Analytics — SDK уже в проекте) через готовый `IAnalyticsProvider`.
2. Release-сборка (IL2CPP + без Development Build) и проверка офлайн-прогресса на реальном устройстве.
3. Remote Config: убрать fallback-переключатель в проде или добавить кэширование последних успешных значений.

## Git

GitFlow: `master` (стабильный) ← `dev` ← `feat/*` / `hotfix/*`, merge `--no-ff`. Conventional Commits. История отражает реальный процесс разработки.
