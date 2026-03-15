# 🧬 Game of Life 3D

Un'implementazione del **Gioco della Vita di Conway in tre dimensioni**, sviluppata in **C# (.NET 8)** con interfaccia grafica **Avalonia UI 11**.

La simulazione si svolge su una griglia cubica toroidale (i bordi si collegano tra loro). Il rendering 3D avviene tramite proiezione prospettica e SkiaSharp, senza dipendenze da OpenGL o GPU dedicata.

---

## Screenshot

> *Avvia l'applicazione e usa il mouse per ruotare la vista 3D.*

---

## Funzionalità

- 🔲 **Griglia 3D toroidale** con vicinato di Moore a 26 celle
- ▶️ **Simulazione in tempo reale** con velocità configurabile (1–60 step/s)
- 🖱️ **Camera orbitale** — trascina per ruotare, rotella per zoom
- 🔍 **Zoom con bottoni e tastiera** — pulsanti `−` `+` `⌂` nella toolbar, shortcut `+` `-` `0`
- 🎛️ **Regole configurabili** — modifica le condizioni di sopravvivenza e nascita a caldo
- 📐 **Dimensione griglia regolabile** — da 10³ a 50³ celle
- 🎨 **Renderer Skia** — funziona su macOS, Windows e Linux senza OpenGL
- 🖼️ **Icona personalizzata** — icona isometrica 3D nel dock/taskbar e nella titlebar

---

## Requisiti

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Build e avvio

```bash
# Compila la soluzione
dotnet build

# Avvia l'applicazione
dotnet run --project src/GameOfLife3D.App
```

---

## Struttura del progetto

```
GameOfLife3D/
├── src/
│   ├── GameOfLife3D.Core/          # Logica di simulazione (senza dipendenze UI)
│   │   ├── Grid3D.cs               # Griglia 3D con doppio buffer thread-safe
│   │   ├── SimulationEngine.cs     # Step, Randomize, Clear
│   │   └── Rules/
│   │       ├── IRule3D.cs          # Interfaccia regola
│   │       └── StandardRule3D.cs  # Regola "445" (sopravvive 5-7, nasce su 6)
│   └── GameOfLife3D.App/           # Applicazione Avalonia
│       ├── Assets/
│       │   └── icon.png            # Icona isometrica 3D (256×256 RGBA)
│       ├── Controls/
│       │   └── GameOfLifeGlControl.cs   # Renderer 3D (Skia + proiezione prospettica)
│       ├── ViewModels/
│       │   └── MainViewModel.cs         # Stato e comandi (CommunityToolkit.Mvvm)
│       ├── Views/
│       │   └── MainWindow.axaml         # Layout UI (toolbar, slider, viewport)
│       └── MacDockIcon.cs               # Icona dock macOS via ObjC runtime
└── .github/
    └── copilot-instructions.md
```

---

## Regole di default — "445"

La regola predefinita è la cosiddetta **"445"** (o *"Clouds"*):

| Condizione   | Numero di vicini |
|-------------|-----------------|
| Sopravvivenza | 5, 6, 7         |
| Nascita       | 6               |

Puoi modificare le regole direttamente dall'interfaccia nei campi **Survive** e **Birth**, inserendo i valori separati da virgola (es. `4,5` oppure `6,7,8`).

Alcune regole interessanti da provare:

| Nome         | Survive | Birth |
|-------------|---------|-------|
| 445 (default) | 5,6,7  | 6     |
| Coral        | 5,6,7,8 | 6,7,8 |
| Amoeba       | 9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26 | 3,5,6,7,8 |
| Slow Decay   | 5,6,7,8 | 6     |

---

## Controlli

| Azione                      | Come                                        |
|-----------------------------|---------------------------------------------|
| Avviare/fermare la sim.     | Pulsante **Start/Stop**                     |
| Eseguire un singolo step    | Pulsante **Step**                           |
| Generare una griglia random | Pulsante **Randomize**                      |
| Pulire la griglia           | Pulsante **Clear**                          |
| Ruotare la vista            | Trascina con il mouse                       |
| Zoom in                     | Pulsante **+** nella toolbar  oppure tasto `+` |
| Zoom out                    | Pulsante **−** nella toolbar  oppure tasto `-` |
| Reset zoom                  | Pulsante **⌂** nella toolbar  oppure tasto `0` |
| Zoom fine (rotella)         | Rotella del mouse                           |
| Cambiare velocità           | Slider **Speed**                            |
| Cambiare dimensione griglia | Slider **Grid Size**                        |

---

## Architettura

Il progetto è diviso in due layer netti:

**`GameOfLife3D.Core`** — libreria pura, senza dipendenze UI, testabile in isolamento.
La griglia usa un **doppio buffer** (`_front` / `_back`): il motore scrive sulla griglia posteriore durante lo step e chiama `Swap()` (thread-safe con lock) al termine. Il thread del timer e il thread di rendering non entrano mai in conflitto.

**`GameOfLife3D.App`** — applicazione Avalonia con pattern MVVM.
Il rendering 3D avviene in `GameOfLifeRenderOp` (implementa `ICustomDrawOperation`): le celle vive vengono proiettate nello spazio schermo tramite matrici `System.Numerics`, ordinate dalla più lontana alla più vicina (algoritmo del pittore) e disegnate come cerchi con sfumatura in profondità su un `SKCanvas`.

La camera è in coordinate sferiche (`_radius`, `_theta`, `_phi`). Il raggio viene aggiornato solo quando cambia la dimensione della griglia, così le operazioni di zoom (rotella, pulsanti, tastiera) persistono correttamente tra un frame e l'altro.

Su **macOS**, l'icona del dock viene impostata programmaticamente in `MacDockIcon.cs` tramite l'ObjC runtime (`NSApplication.setApplicationIconImage:`), necessario quando l'app gira fuori da un bundle `.app`.

---

## Licenza

MIT
