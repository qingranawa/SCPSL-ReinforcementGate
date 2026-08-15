# ReinforcementGate

[![LabAPI](https://img.shields.io/badge/LabAPI-1.1.7-5865F2)](https://github.com/northwood-studios/LabAPI) [![SCP:SL](https://img.shields.io/badge/SCP%3ASL-14.2.7-2f3136)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![.NET%20Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)](https://dotnet.microsoft.com/download/dotnet-framework) [![Release](https://img.shields.io/github/v/release/qingranawa/SCPSL-ReinforcementGate?display_name=tag&sort=semver)](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases/latest) [![CI](https://github.com/qingranawa/SCPSL-ReinforcementGate/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/qingranawa/SCPSL-ReinforcementGate/actions/workflows/ci.yml) [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Feingranulare Kontrolle der SCP:SL-Verstärkungswellen für LabAPI.

ReinforcementGate steuert unabhängig alle vier großen und kleinen NTF- und Chaos-Insurgency-Wellen, einmalige `skip`-Aktionen, Remote-Admin-Berechtigungen und öffentliche Plugin-APIs.

Die Hauptdokumentation ist auf Chinesisch. Weitere Sprachen: [中文](README.md) · [English](README.en.md) · [Polski](README.pl.md)

## Kernfunktionen

- Große und kleine NTF- sowie Chaos-Insurgency-Wellen unabhängig steuern.
- Zielbezogene oder globale einmalige `skip`-Aktionen setzen, ohne dauerhafte Sperren umzuschreiben.
- Wellen über Remote Admin, `RespawnEvents`-Berechtigungen und konfigurierbare Broadcast/CASSIE-Benachrichtigungen verwalten.
- Andere LabAPI-Plugins über synchrone, stark typisierte Snapshots, Steuerungsmethoden und öffentliche Ereignisse integrieren.

## Schnellstart

1. `ReinforcementGate.dll` aus dem [neuesten Release](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases/latest) herunterladen.
2. Die DLL nach `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\ReinforcementGate.dll` kopieren.
3. Den Server neu starten.
4. In Remote Admin `rf status` ausführen; der Status-Snapshot bestätigt das Laden des Plugins.

## Demo

<video controls preload="metadata" width="720">
  <source src="assets/reinforcement-gate-demo.mp4" type="video/mp4">
  Dein Browser unterstützt kein HTML5-Video; <a href="assets/reinforcement-gate-demo.mp4">Demo-Video herunterladen</a>.
</video>

[Demo-Video herunterladen](assets/reinforcement-gate-demo.mp4)

Vorgeschlagener Demo-Ablauf: `rf status` → `rf disable ntf` → `rf skip ci` → die passende Verstärkungswelle auslösen und die Abfangbenachrichtigung prüfen.

## Serveradministration

### Funktionen

- Erkennt die vier LabAPI-Wellentypen: `MtfWave` (`ntf`), `MiniMtfWave` (`ntf-mini`), `ChaosWave` (`ci`) und `MiniChaosWave` (`ci-mini`).
- Globale und zielbezogene Schalter sind unabhängig voneinander.
- Unterstützt ein einmaliges `skip` pro Ziel sowie ein globales `skip` für die nächste erkannte Welle.
- Ermöglicht getrennte Broadcast- und CASSIE-Vorlagen für die Ausführung eines Befehls und für die tatsächliche Abfangentscheidung.
- Stellt synchrone, stark typisierte Read-only-Zustandssnapshots, Steuerungs-APIs und öffentliche Ereignisse bereit.
- Ein Konfigurations-Reload ersetzt nur den Benachrichtigungsbaum und erhält den Laufzeitstatus der aktuellen Runde.

### Kompatibilität

- SCP: Secret Laboratory Dedicated Server.
- **LabAPI 1.1.7**; keine Abhängigkeit von EXILED oder LabExtended.
- .NET Framework 4.8 (`net48`), x64, C# 12.
- Die Klassifizierung basiert auf LabAPI-Wellentypen, nicht auf Spielerzahlen oder geratenem Spielstatus.

Nach Änderungen an Server-, LabAPI- oder Spielassembly-Versionen muss das Plugin neu gebaut und auf einem Testserver mit allen vier Wrapper-Typen geprüft werden.

### Installation

1. Installiere einen kompatiblen LabAPI-1.1.7-Server.
2. Lade `ReinforcementGate.dll` aus den [Releases](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases) herunter.
3. Kopiere die DLL nach `%AppData%\\SCP Secret Laboratory\\LabAPI\\plugins\\global\\ReinforcementGate.dll` (Hosting-Anbieter können ein entsprechendes lokales AppData-Verzeichnis verwenden).
4. Starte den Server neu. LabAPI erstellt die Port-Konfiguration unter `%AppData%\\SCP Secret Laboratory\\LabAPI\\configs\\<port>\\ReinforcementGate\\reinforcement-gate.yml`.

### Remote-Admin-Befehle

Der Hauptbefehl lautet `reinforcement`; `rf` ist ein Alias. Beide Schreibweisen funktionieren.

| Befehl | Beschreibung |
| --- | --- |
| `reinforcement status` | Zeigt globalen Status, alle vier Ziele, lokale Zustände und ausstehende `skip` an. |
| `reinforcement enable <target>` | Gibt ein Ziel frei; `all` hebt nur die globale Sperre auf. |
| `reinforcement disable <target>` | Sperrt ein Ziel dauerhaft; `all` aktiviert die globale Sperre. |
| `reinforcement skip <target>` | Sperrt die nächste passende Welle; `all` bedeutet die nächste erkannte Welle. |
| `reinforcement reset` | Stellt sofort die Standardwerte wieder her und löscht jedes `skip`. |

Ziele: `all` (alle erkannten Wellen), `ntf` (große NTF-Welle), `ntf-mini` (kleine NTF-Welle), `ci` (große Chaos-Insurgency-Welle), `ci-mini` (kleine Chaos-Insurgency-Welle).

Beispiele: `rf disable ntf-mini`, `rf skip ci`, `rf enable all`.

### Status und `skip`-Priorität

Für jede erkannte Welle gilt folgende Reihenfolge:

1. Globale Sperre: blockieren mit `global-disabled`.
2. Ziel-Sperre: blockieren mit `target-disabled`.
3. Ziel-`skip`: blockieren und nur dieses Ziel-`skip` verbrauchen, Grund `skip`.
4. Globales `skip`: blockieren und nur das globale `skip` verbrauchen, Grund `skip`.
5. Andernfalls darf die Welle erzeugt werden.

Dauerhafte Sperren haben Vorrang vor `skip`; ein `skip` wird dabei nicht verbraucht. Wenn Ziel- und globales `skip` gleichzeitig vorhanden sind, wird zuerst das Ziel-`skip` verbraucht. Das globale Aktivieren/Deaktivieren ändert die vier lokalen Zielschalter nicht. Zu Rundenbeginn werden alle Ziele freigegeben und alle `skip` gelöscht; Laufzeitstatus wird nicht über Runden gespeichert.

### Berechtigungen

- `status` steht jedem Remote-Admin-Aufrufer offen und prüft weder einen RA-Berechtigungsknoten noch `RespawnEvents`.
- `enable`, `disable`, `skip` und `reset` ändern den Status und benötigen `PlayerPermissions.RespawnEvents`.
- Ungültige oder nicht autorisierte Anfragen ändern den Status nicht und senden keine Benachrichtigung.
- Read-only-APIs zwischen Plugins verwenden keine RA-Berechtigungen. Die Steuerungs-API protokolliert die übergebene `source`; die aufrufende Plugin entscheidet selbst über die Autorisierung.

### Konfiguration und Benachrichtigungen

Alle Benachrichtigungsknoten stehen standardmäßig auf `mode: None`; standardmäßig wird kein Broadcast und kein CASSIE gesendet. Serverbetreiber können eigene Vorlagen eintragen und einen Knoten auf `Broadcast`, `Cassie` oder `Both` setzen.

Die Knoten heißen `enable_applied`, `disable_applied`, `disabled_wave_blocked`, `skip_armed` und `skip_triggered`. Sie werden nach einer tatsächlichen enable-/disable-Änderung, einer dauerhaften Abfangentscheidung, dem erfolgreichen Setzen eines `skip` bzw. dem Verbrauch eines einmaligen `skip` ausgelöst.

Unterstützt werden `mode`, `broadcast.message`, `broadcast.duration`, `broadcast.clear_previous`, `cassie.message`, `cassie.subtitles`, `cassie.play_background`, `cassie.priority` und `cassie.glitch_scale`. Ungültige Knoten fallen auf den Standard zurück und protokollieren ihren vollständigen Pfad. Unbekannte Platzhalter bleiben erhalten und werden protokolliert. Fehler beim Parsen oder Senden ändern die bereits getroffene Erlaubnis-/Blockierungsentscheidung nicht. Ein Konfigurations-Reload ersetzt atomar nur den Benachrichtigungsbaum.

### Platzhalter

Broadcast, CASSIE-Sprache und Untertitel unterstützen `{target}`, `{target_name}`, `{admin}`, `{action}` und `{reason}`. Bei `enable` ist `{reason}` leer; bei disable/Abfangereignissen lautet er `global-disabled`, `target-disabled` oder `skip`.

## Entwickler-API

Andere LabAPI-Plugins können die folgenden synchronen, stark typisierten Schnittstellen zum Lesen des Status, zur Wellensteuerung und für öffentliche Ereignisse verwenden.

### Read-only-Status-API

Ein anderes LabAPI-Plugin kann `ReinforcementGate.dll` referenzieren und `ReinforcementStatesApi` verwenden. Snapshots und Ziel-Dictionaries sind schreibgeschützt und legen den internen Controller nicht offen.

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

Verfügbar sind `IsAvailable`, `GetSnapshot()`, `GetState(target)`, `TryGetState(target, out state)`, `StateChanged` und `RoundStateReset`. `GetState` und `TryGetState` akzeptieren nur konkrete Ziele, nicht `All`. Diese Abfragen benötigen keine RA-Berechtigung; bei Nichtverfügbarkeit gibt es eine klare Ausnahme oder `false`.

### Steuerungs-API

Die Steuerungs-API verwendet denselben benachrichtigungsfähigen Controller wie die RA-Befehle. Eine nicht-leere `source` ist für Auditdaten und den Platzhalter `{admin}` erforderlich.

```csharp
using ReinforcementGate.Api;
using ReinforcementGate.Domain;

ReinforcementControlApi.ArmSkip(
    ReinforcementTarget.CiMini,
    "ExamplePlugin");
```

Die Einstiegspunkte sind `SetEnabled(target, enabled, source)`, `ArmSkip(target, source)`, `ClearSkip(target, source)` und `Reset(source)`. Sie akzeptieren `All` oder ein konkretes Ziel und liefern ein `StateTransitionResult`. Wiederholte Vorgänge haben `Changed == false` und senden keine doppelte Befehlsbenachrichtigung. Alle öffentlichen APIs sind synchron und müssen im Server-Hauptthread aufgerufen werden; die aufrufende Plugin übernimmt Autorisierung und Threadprüfung.

### Öffentliche Ereignisse

- `ReinforcementStatesApi.StateChanged` wird nach einer Statusänderung mit einem unveränderlichen `StateTransitionResult` ausgelöst.
- `ReinforcementStatesApi.RoundStateReset` wird bei jedem Runden-Reset ausgelöst.
- `ReinforcementEvents.WaveBlocked` wird nach einem tatsächlichen Abfangen mit Ziel, `ReinforcementBlockReason`, Quelle und unveränderlichem Snapshot nach dem `skip`-Verbrauch ausgelöst.

Fehler von Abonnenten werden isoliert. Beim Entladen entfernt das Plugin interne Weiterleitungen und öffentliche Abonnements.

## Build und Tests

Setze `SL_REFERENCES` auf ein echtes SCP:SL-Verzeichnis `SCPSL_Data/Managed`, das `Assembly-CSharp.dll` und `CommandSystem.Core.dll` enthält:

```powershell
$env:SL_REFERENCES = "D:\\SCPServer\\SCPSL_Data\\Managed"
dotnet restore ReinforcementGate.sln
dotnet build ReinforcementGate.sln --configuration Release --no-restore
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --configuration Release --no-build
```

Die Ausgabe ist `src/ReinforcementGate/bin/Release/net48/ReinforcementGate.dll`. Ein Build mit Stub-Assemblies beweist keine Serverkompatibilität; prüfe Laden, Reload, alle vier Wellen, Broadcast/CASSIE und Runden-Reset auf einem Testserver.

Die öffentliche CI lädt keine SCP:SL-Spielassemblies herunter und verteilt sie nicht weiter. Sie führt Restore, Leerraumprüfung sowie Prüfungen auf Repository-Binärdateien und Diff-Fehler aus; der Build mit `SL_REFERENCES`, Unit-Tests und die Serverkompatibilitätsprüfung müssen lokal oder in einer kontrollierten Testumgebung erfolgen.

## Bekannte Einschränkungen

- Das Plugin entscheidet nur, ob zukünftige LabAPI-Verstärkungsereignisse zugelassen werden.
- Es erzeugt oder wiederholt keine Wellen und ändert keine bereits erzeugten Spieler, Rollen oder Fraktionen.
- Zu Rundenbeginn werden die Standardwerte wiederhergestellt.
- Unbekannte oder zukünftige Wrapper-Typen werden mit einer rate-limitierten Warnung durchgelassen.
- Konfiguration und öffentliche APIs sind synchron und nur im Server-Hauptthread sicher.
- Dieses Projekt installiert, aktualisiert oder konfiguriert weder den Spielserver noch LabAPI.

## Community-Dateien

- [Beitragen](CONTRIBUTING.md)
- [Verhaltenskodex](CODE_OF_CONDUCT.md)
- [Hilfe](SUPPORT.md)
- [Issue-Vorlagen](.github/ISSUE_TEMPLATE/)
- [Sicherheitsrichtlinie](SECURITY.md)
- [Pull-Request-Vorlage](.github/PULL_REQUEST_TEMPLATE.md)

## Lizenz

Der Originalcode dieses Repositorys steht unter der [MIT-Lizenz](LICENSE). LabAPI, SCP:SL-Assemblies und andere Drittkomponenten werden hier nicht weiterverteilt und bleiben ihren eigenen Bedingungen unterworfen.
