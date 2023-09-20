# PmPrefs - Unity Editor Extension

## Vorwort
Das Speichern und Laden von Daten in Unity kann kompliziert sein, aber nicht mehr mit PmPrefs. Diese Unity Editor Extension macht das Speichern und Laden von Daten so einfach wie nie zuvor.

## Features

### Einfache Speicherung
Mit nur einer Zeile Code können Sie alle .NET-Objekte speichern.
```csharp
PmPrefs.Save("name", variable);
```

### Einfaches Laden
Das Laden ist genauso einfach, mit einem kleinen Unterschied: Sie müssen den Datentyp angeben.
```csharp
PmPrefs.Load<type>("name");
```

### Sichere Speicherung
Variablen werden verschlüsselt gespeichert, sodass Sie sicher sein können, dass sie nicht einfach manipuliert werden können.

### Export / Import
Machen Sie mehrere Tests oder arbeiten Sie an verschiedenen Teilen Ihres Projekts? Mit PmPrefs können Sie verschiedene Zustände speichern und wieder laden.

### Und mehr ...
- Voll kompatibel mit Unity PlayerPrefs.
- Keine besonderen Rechte auf dem Endgerät erforderlich.
- Funktioniert mit allen Unity-unterstützten Systemen.

## Warum ist das so besonders?
PmPrefs verwendet weiterhin Unitys PlayerPrefs, unterstützt jedoch alle Unity-Systeme ohne spezielle Rechte auf dem jeweiligen Endgerät.

## Verschlüsselung
Es gibt verschiedene Gründe, warum Sie gespeicherte Variablen verschlüsseln möchten. Einer davon ist, dass Passwörter oder andere sensible Daten nicht im Klartext auf dem System gespeichert werden.

## Anwendungsbeispiele
Stellen Sie sich vor, jemand schreibt eine Online-Banking-App und speichert das Passwort im Klartext im Windows-Register. Nicht sehr gut, oder? Mit PmPrefs ist das kein Problem.

## Installation
Kopieren Sie einfach den Code der `PmPrefs` Klasse in Ihr Unity-Projekt.

## Benutzung
Nach der Installation können Sie die `PmPrefs` Klasse in Ihren Scripts verwenden, genau wie in den oben genannten Beispielen.

## Unterstützung
Haben Sie Probleme oder Anregungen? Öffnen Sie ein [Issue](HierIhrenIssueTrackerLinkEinfügen).
