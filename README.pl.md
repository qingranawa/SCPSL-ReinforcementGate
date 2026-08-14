# ReinforcementGate

[![LabAPI](https://img.shields.io/badge/LabAPI-1.1.7-5865F2)](https://github.com/northwood-studios/LabAPI) [![SCP:SL](https://img.shields.io/badge/SCP%3ASL-14.2.7-2f3136)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

ReinforcementGate to wtyczka LabAPI dla serwerów SCP: Secret Laboratory. Kontroluje główne i mini fale posiłków NTF oraz Chaos Insurgency, oferując globalne zatrzymanie i pominięcie jednej następnej fali.

Główny README jest po chińsku. Inne języki: [中文](README.md) · [English](README.en.md) · [Deutsch](README.de.md)

## Funkcje

- Rozpoznaje cztery typy fal LabAPI: `MtfWave` (`ntf`), `MiniMtfWave` (`ntf-mini`), `ChaosWave` (`ci`) i `MiniChaosWave` (`ci-mini`).
- Przełączniki globalne i dla poszczególnych celów są niezależne.
- Obsługuje jednorazowe `skip` dla celu oraz globalne `skip` dla następnej rozpoznanej fali.
- Pozwala osobno skonfigurować komunikaty Broadcast i CASSIE przy wykonaniu komendy oraz przy faktycznym zablokowaniu fali.
- Udostępnia synchroniczne, silnie typowane, tylko do odczytu migawki stanu, API sterujące i zdarzenia publiczne.
- Przeładowanie konfiguracji podmienia tylko drzewo powiadomień i zachowuje stan bieżącej rundy.

## Kompatybilność

- SCP: Secret Laboratory Dedicated Server.
- **LabAPI 1.1.7**; brak zależności od EXILED i LabExtended.
- .NET Framework 4.8 (`net48`), x64, C# 12.
- Klasyfikacja opiera się na typach fal LabAPI, a nie na liczbie graczy ani zgadywaniu stanu gry.

Po zmianie wersji serwera, LabAPI lub bibliotek gry przebuduj wtyczkę i sprawdź sygnatury zdarzeń oraz cztery typy opakowań fal na serwerze testowym.

## Instalacja

1. Zainstaluj kompatybilny serwer z LabAPI 1.1.7.
2. Pobierz `ReinforcementGate.dll` z sekcji [Releases](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases).
3. Skopiuj plik do `%AppData%\\SCP Secret Laboratory\\LabAPI\\plugins\\global\\ReinforcementGate.dll` (hosting może używać równoważnego lokalnego katalogu AppData).
4. Uruchom lub zrestartuj serwer. LabAPI utworzy konfigurację portu w `%AppData%\\SCP Secret Laboratory\\LabAPI\\configs\\<port>\\ReinforcementGate\\reinforcement-gate.yml`.

## Komendy Remote Admin

Komenda główna to `reinforcement`, a jej alias to `rf`; można używać obu nazw.

| Komenda | Opis |
| --- | --- |
| `reinforcement status` | Pokazuje stan globalny, cztery cele, stany lokalne i oczekujące `skip`. |
| `reinforcement enable <target>` | Zezwala na cel; `all` usuwa tylko globalne zatrzymanie. |
| `reinforcement disable <target>` | Trwale zatrzymuje cel; `all` włącza globalne zatrzymanie. |
| `reinforcement skip <target>` | Zatrzymuje następną pasującą falę; `all` oznacza następną rozpoznaną falę. |
| `reinforcement reset` | Natychmiast przywraca wartości domyślne i usuwa wszystkie `skip`. |

Cele: `all` (wszystkie rozpoznane fale), `ntf` (główna fala NTF), `ntf-mini` (mini fala NTF), `ci` (główna fala Chaos Insurgency), `ci-mini` (mini fala Chaos Insurgency).

Przykłady: `rf disable ntf-mini`, `rf skip ci`, `rf enable all`.

## Stan i kolejność `skip`

Dla każdej rozpoznanej fali decyzja przebiega tak:

1. Globalne zatrzymanie: blokada z powodem `global-disabled`.
2. Zatrzymanie celu: blokada z powodem `target-disabled`.
3. `skip` celu: blokada i zużycie tylko tego `skip`, z powodem `skip`.
4. Globalne `skip`: blokada i zużycie tylko globalnego `skip`, z powodem `skip`.
5. W przeciwnym razie fala zostaje przepuszczona.

Trwałe zatrzymanie ma priorytet nad `skip`, więc blokada nie zużywa `skip`. Gdy istnieją jednocześnie `skip` celu i globalne, najpierw zużywany jest `skip` celu. Początek rundy włącza wszystkie cele i usuwa wszystkie `skip`; stan działania nie jest zapisywany między rundami.

## Uprawnienia

- `status` jest dostępne dla każdego wywołującego Remote Admin i nie sprawdza ani węzła uprawnień RA, ani `RespawnEvents`.
- `enable`, `disable`, `skip` i `reset` zmieniają stan i wymagają `PlayerPermissions.RespawnEvents`.
- Nieprawidłowe lub nieautoryzowane żądania nie zmieniają stanu i nie wysyłają powiadomień.
- Tylko do odczytu API między wtyczkami nie używa uprawnień RA. API sterujące zapisuje przekazane `source`, a autoryzację pozostawia wywołującej wtyczce.

## Konfiguracja i powiadomienia

Wszystkie węzły powiadomień domyślnie mają `mode: None`, więc domyślnie nie jest wysyłany Broadcast ani CASSIE. Właściciel serwera może wpisać własne szablony i ustawić `Broadcast`, `Cassie` lub `Both`.

Węzły to `enable_applied`, `disable_applied`, `disabled_wave_blocked`, `skip_armed` i `skip_triggered`. Odpowiadają one odpowiednio zmianie enable, zmianie disable, faktycznej blokadzie trwałej, uzbrojeniu `skip` i zużyciu jednorazowego `skip`.

Obsługiwane pola to `mode`, `broadcast.message`, `broadcast.duration`, `broadcast.clear_previous`, `cassie.message`, `cassie.subtitles`, `cassie.play_background`, `cassie.priority` i `cassie.glitch_scale`. Nieprawidłowy węzeł wraca do wartości domyślnych, a jego pełna ścieżka jest logowana. Nieznane placeholdery są zachowywane i logowane. Błąd parsowania lub wysyłania nie zmienia wcześniejszej decyzji o przepuszczeniu albo blokadzie. Przeładowanie konfiguracji atomowo podmienia tylko drzewo powiadomień.

## Placeholdery szablonów

Broadcast, mowa CASSIE i napisy obsługują `{target}`, `{target_name}`, `{admin}`, `{action}` oraz `{reason}`. Dla `enable` wartość `{reason}` jest pusta, a dla disable/intercepcji to `global-disabled`, `target-disabled` lub `skip`.

## Tylko do odczytu API stanu

Inna wtyczka LabAPI może odwołać się do `ReinforcementGate.dll` i użyć `ReinforcementStatesApi`. Migawki oraz słowniki celów są tylko do odczytu i nie ujawniają wewnętrznego kontrolera.

```csharp
using LabApi.Features.Console;
using ReinforcementGate.Api;
using ReinforcementGate.Domain;

if (ReinforcementStatesApi.IsAvailable)
{
    ReinforcementStateSnapshot snapshot = ReinforcementStatesApi.GetSnapshot();
    ReinforcementTargetState ntf = snapshot.Targets[ReinforcementTarget.Ntf];
    Logger.Info($"NTF effective enabled: {ntf.IsEffectivelyEnabled}");
}
```

Dostępne są `IsAvailable`, `GetSnapshot()`, `GetState(target)`, `TryGetState(target, out state)`, `StateChanged` i `RoundStateReset`. `GetState` i `TryGetState` przyjmują tylko konkretne cele, nie `All`. Zapytania nie wymagają uprawnień RA; gdy usługa jest niedostępna, zwracany jest czytelny wyjątek albo `false`.

## API sterujące

API sterujące używa tego samego kontrolera z obsługą powiadomień co komendy RA. Niepuste `source` jest wymagane do audytu i wartości `{admin}`.

```csharp
using ReinforcementGate.Api;
using ReinforcementGate.Domain;

ReinforcementControlApi.ArmSkip(
    ReinforcementTarget.CiMini,
    "ExamplePlugin");
```

Punkty wejścia to `SetEnabled(target, enabled, source)`, `ArmSkip(target, source)`, `ClearSkip(target, source)` i `Reset(source)`. Przyjmują `All` lub konkretny cel i zwracają `StateTransitionResult`. Powtórzona operacja ma `Changed == false` i nie wysyła ponownie powiadomienia komendy. Wszystkie publiczne API są synchroniczne i muszą być wywoływane na głównym wątku serwera; wywołująca wtyczka odpowiada za autoryzację i wątek.

## Zdarzenia publiczne

- `ReinforcementStatesApi.StateChanged` uruchamia się po zmianie stanu i przekazuje niezmienny `StateTransitionResult`.
- `ReinforcementStatesApi.RoundStateReset` uruchamia się przy każdym resecie rundy.
- `ReinforcementEvents.WaveBlocked` uruchamia się po faktycznej blokadzie i przekazuje cel, `ReinforcementBlockReason`, źródło oraz niezmienną migawkę po zużyciu `skip`.

Błędy subskrybentów są izolowane. Przy wyłączaniu wtyczka usuwa wewnętrzne przekierowania i publiczne subskrypcje.

## Budowanie i testy

Ustaw `SL_REFERENCES` na prawdziwy katalog `SCPSL_Data/Managed` zawierający `Assembly-CSharp.dll` i `CommandSystem.Core.dll`:

```powershell
$env:SL_REFERENCES = "D:\\SCPServer\\SCPSL_Data\\Managed"
dotnet restore ReinforcementGate.sln
dotnet build ReinforcementGate.sln --configuration Release --no-restore
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --configuration Release --no-build
```

Wynik to `src/ReinforcementGate/bin/Release/net48/ReinforcementGate.dll`. Samo przejście testów z użyciem stubów nie potwierdza zgodności z serwerem; sprawdź ładowanie, reload, cztery fale, Broadcast/CASSIE i reset rundy na serwerze testowym.

Publiczne CI nie pobiera ani nie redystrybuuje bibliotek gry SCP:SL. Wykonuje przywracanie pakietów, kontrolę białych znaków, binariów repozytorium i różnic; kompilacja z `SL_REFERENCES`, testy jednostkowe oraz sprawdzenie zgodności z serwerem muszą być wykonane lokalnie lub w kontrolowanym środowisku testowym.

## Znane ograniczenia

- Wtyczka decyduje tylko o przyszłych zdarzeniach fal LabAPI.
- Nie tworzy ani nie odtwarza fal i nie zmienia już wygenerowanych graczy, ról ani frakcji.
- Początek rundy przywraca wartości domyślne.
- Nieznane lub przyszłe typy wrapperów są przepuszczane z ostrzeżeniem ograniczonym częstotliwością.
- Konfiguracja i publiczne API są synchroniczne i bezpieczne wyłącznie na głównym wątku serwera.
- Projekt nie instaluje, nie aktualizuje ani nie konfiguruje serwera gry lub LabAPI.

## Pliki społeczności

- [Współtworzenie](CONTRIBUTING.md)
- [Kodeks postępowania](CODE_OF_CONDUCT.md)
- [Pomoc](SUPPORT.md)
- [Szablony zgłoszeń](.github/ISSUE_TEMPLATE/)
- [Polityka bezpieczeństwa](SECURITY.md)
- [Szablon Pull Request](.github/PULL_REQUEST_TEMPLATE.md)

## Licencja

Oryginalny kod tego repozytorium jest objęty [licencją MIT](LICENSE). LabAPI, biblioteki SCP:SL i inne komponenty zewnętrzne nie są tutaj redystrybuowane i podlegają własnym warunkom.
