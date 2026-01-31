# Agents.md — Blazor Server: PSM-Export (CSV)

## Ziel
Eine **Blazor Server App** bauen, die über die **BVL PSM API** alle Pflanzenschutzmittel („Mittel“) inkl.
- **Wirkstoff(e)** (verknüpft über *wirkstoff_gehalt*)
- **Zulassungszeitraum** (Anfang/Ende)
- **Schadorganismen („gegen was“) = `schadorg`**  
  → **muss dekodiert werden** (Kode → Klartext) über den **`/kode`** Endpoint

als **CSV** exportiert. Zusätzlich: **UI zur Auswahl der Export-Spalten**.

---

## Relevante Endpoints
### Basisdaten
- Mittel: `https://psm-api.bvl.bund.de/ords/psm/api-v1/mittel/`
- Wirkstoff: `https://psm-api.bvl.bund.de/ords/psm/api-v1/wirkstoff/`
- Mittel↔Wirkstoff (+ Gehalt): `https://psm-api.bvl.bund.de/ords/psm/api-v1/wirkstoff_gehalt/`

### Anwendungen / Schadorganismen
- Anwendungen (verknüpft Mittel ↔ Anwendung): `https://psm-api.bvl.bund.de/ords/psm/api-v1/awg`
  - Eine Anwendung beinhaltet u.a. Mittel und Schadorganismus. :contentReference[oaicite:0]{index=0}
- Zuordnung Anwendung ↔ Schadorganismus: `https://psm-api.bvl.bund.de/ords/psm/api-v1/awg_schadorg` :contentReference[oaicite:1]{index=1}
- Schadorganismus Baum/Gruppe (optional, für Hierarchie): `https://psm-api.bvl.bund.de/ords/psm/api-v1/schadorg_gruppe` :contentReference[oaicite:2]{index=2}

### Dekodierung (Kode → Klartext)
- Kode-Decode: `https://psm-api.bvl.bund.de/ords/psm/api-v1/kode` :contentReference[oaicite:3]{index=3}
- Kodelisten-Infos: `https://psm-api.bvl.bund.de/ords/psm/api-v1/kodeliste` :contentReference[oaicite:4]{index=4}
- Mapping: welche Tabelle/Feld nutzt welche Kodeliste:
  `https://psm-api.bvl.bund.de/ords/psm/api-v1/kodeliste_feldname` :contentReference[oaicite:5]{index=5}

Doku-Referenzen:
- Swagger: `https://psm-api.bvl.bund.de/#/default/get_wirkstoff_gehalt_`
- Repo: `https://github.com/bundesAPI/pflanzenschutzmittelzulassung-api` :contentReference[oaicite:6]{index=6}

---

## Kerndaten-Pipeline (inkl. Schadorg-Decode)

### Schritt A — Mittel laden
- Alle Einträge aus `/mittel`
- Extrahiere:
  - `kennr` (Kenn-/Zulassungsnummer)
  - Zulassungsbeginn/-ende (Feldnamen 1:1 aus Swagger übernehmen)

### Schritt B — Wirkstoffe joinen
- `/wirkstoff` (wirknr → Wirkstoffname)
- `/wirkstoff_gehalt` (kennr ↔ wirknr + gehalt/einheit)

### Schritt C — Schadorganismen je Mittel ermitteln
**Weg über Anwendungen (AWG):**
1. `/awg?kennr=<KENNR>` → liefert alle Anwendungen zu einem Mittel :contentReference[oaicite:7]{index=7}  
2. Für jede `awg_id` die Schadorg-Kodes aus `/awg_schadorg?awg_id=<AWG_ID>` holen :contentReference[oaicite:8]{index=8}  
3. Ergebnis ist ein oder mehrere `schadorg`-Kodes (z. B. `BRORM` etc.) :contentReference[oaicite:9]{index=9}

### Schritt D — Schadorg-Kodes dekodieren (Klartext)
**Wichtig:** `/kode` benötigt i.d.R. `kode` + `kodeliste` (+ optional `sprache=DE`). :contentReference[oaicite:10]{index=10}

**Wie finden wir die passende `kodeliste` für `schadorg`?**
- Über `/kodeliste_feldname` herausfinden, welche Kodeliste in welcher Tabelle/Spalte steckt. :contentReference[oaicite:11]{index=11}  
  Beispiel-Strategie:
  - Query für Tabelle/Feld, die `schadorg` enthält (z. B. `AWG_SCHADOR...` + Feld `SCHADOR...` / `SCHADORG`)
  - Ergebnis enthält `kodeliste` Nummer.

**Decode-Aufruf (konzeptionell):**
- `/kode?kode=<SCHADORG_KODE>&kodeliste=<LISTE>&sprache=DE` :contentReference[oaicite:12]{index=12}  
→ liefert `kodetext` (der Anzeigename für CSV/UI)

Optional:
- Falls du zusätzlich Gruppierung/Hierarchie willst: `/schadorg_gruppe` als Baumstruktur nutzen (Parent/Child). :contentReference[oaicite:13]{index=13}

---

## Projekte / Struktur
Solution: `PsmExporter.sln`

### 1) PsmExporter.Web (Blazor Server)
- Spaltenauswahl (Checkbox-Liste)
- Export-Button → CSV Download
- Preview (nur ausgewählte Spalten)

### 2) PsmExporter.Data (Class Library)
- DTOs (API)
- Domain Models (aggregiert)
- API Clients + Aggregation Service
- Kode-Dekoder (schadorg → Text)
- CSV Builder

---

## Models (PsmExporter.Data)

### DTOs
- `MittelDto` (aus `/mittel`)
- `WirkstoffDto` (aus `/wirkstoff`)
- `WirkstoffGehaltDto` (aus `/wirkstoff_gehalt`)
- `AwgDto` (aus `/awg`)
- `AwgSchadorgDto` (aus `/awg_schadorg`)
- `KodeDto` (aus `/kode`)
- `KodelisteFeldnameDto` (aus `/kodeliste_feldname`)

### Domain
- `MittelAggregate`
  - `string Kennr`
  - `string Name`
  - `DateOnly? ZulassungVon`
  - `DateOnly? ZulassungBis`
  - `List<WirkstoffInfo> Wirkstoffe`
  - `List<SchadorgInfo> Schadorganismen` (decoded)

- `SchadorgInfo`
  - `string Kode`
  - `string? Text` (dekodiert)
  - optional: `List<string> Gruppen` (wenn /schadorg_gruppe genutzt wird)

---

## Services (PsmExporter.Data)

### Typed Clients
- `IMittelClient`
- `IWirkstoffClient`
- `IWirkstoffGehaltClient`
- `IAwgClient`
- `IAwgSchadorgClient`
- `IKodeClient`
- `IKodelistenClient` (kodeliste + kodeliste_feldname)

### Aggregation
`IPsmExportService.LoadAggregatedAsync(...)`
- lädt Mittel
- joint Wirkstoffe
- lädt AWGs je Mittel (oder batchweise)
- extrahiert Schadorg-Kodes
- **ermittelt `kodeliste` für Schadorg** via `/kodeliste_feldname`
- **dekodiert** Schadorg-Kodes via `/kode`
- baut `MittelAggregate`

**Caching (wichtig für Performance)**
- Cache für:
  - Wirkstoff-Dictionary
  - `kodeliste` Nummer für Schadorg (einmalig)
  - Kode→Text Mapping (Dictionary)

---

## CSV Export
- Spalten-Registry (inkl. `Schadorganismen` als:
  - `BRORM (Bromus ...); ...` oder nur Klartext)
- Separator `;`
- UTF-8 (optional BOM für Excel)

---

## Docker + GitHub Action
- Dockerfile (multi-stage .NET 8)
- Workflow: buildx + push nach GHCR

---

## Umsetzungsschritte (Checkliste)
1. Solution + Projekte (Web + Data)
2. DTOs aus Swagger/Repo ableiten
3. Clients + Pagination
4. Aggregation:
   - Mittel/Wirkstoff join
   - AWG + AWG_SCHADORG join
   - **Schadorg-Kodeliste via KODELISTE_FELDNAME bestimmen**
   - **Decode via KODE**
5. CSV Builder + Download Endpoint
6. UI: Spaltenauswahl + Preview
7. Dockerfile + GH Action
