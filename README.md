# Simpel Bogføring - Brugerguide 📊

En dansk bogførings-applikation til studerende som skal lære regnskab.

## 📋 Hvad gør programmet?

Dette program hjælper dig med at:
- Bogføre transaktioner (køb, salg, betalinger)
- Automatisk beregne og bogføre moms
- Håndtere åbningsbalancer (primo)
- Generere regnskabsrapporter
- Eksportere data til Excel

## 🚀 Sådan starter du programmet

Åbn **Command Prompt** eller **PowerShell** og skriv:
```
dotnet run --input MAPPE_NAVN
```

Eksempel:
```
dotnet run --input data1
```

Dette vil læse regnskabsdata fra mappen `data1` og lave rapporter i `data1\out`.

## 📁 Filstruktur - Hvad skal du oprette?

For at bruge programmet skal du oprette en mappe (f.eks. `mit_regnskab`) med disse filer:

### 1. `regnskab.csv` - Grundoplysninger
```csv
navn;startdato;slutdato;momsprocent
Mit Firma;01-01-2025;31-12-2025;25
```

**Forklaring:**
- **navn**: Dit firmanavn
- **startdato**: Regnskabsårets start (01-01-2025)
- **slutdato**: Regnskabsårets slut (31-12-2025)  
- **momsprocent**: Momssats i Danmark (25%)

### 2. `kontoplan.csv` - Dine konti
```csv
nr;navn;type;moms
1000;Bank;status;INGEN
1100;Kasse;status;INGEN
2000;Tilgodehavende moms;status;INGEN
3000;Skyldig moms;status;INGEN
4000;Egenkapital;status;INGEN
5000;Salg;drift;UDG
6000;Indkøb;drift;INDG
7000;Omkostninger;drift;INDG
```

**Kontotyper:**
- **status**: Balancekonti (bank, kasse, gæld, egenkapital)
- **drift**: Resultatkonti (salg, indkøb, omkostninger)

**Momstyper:**
- **INGEN**: Ingen moms (f.eks. bank, egenkapital)
- **UDG**: Udgående moms (når du sælger og skal betale moms)
- **INDG**: Indgående moms (når du køber og kan få moms retur)

### 3. Posteringsfiler - Dine transaktioner

Opret filer der starter med `posteringer` (f.eks. `posteringer-jan.csv`, `posteringer-feb.csv`):

```csv
Dato;Bilagsnummer;Konto;Tekst;Beløb
15-01-2025;1001;5000;Salg til kunde A;1000
15-01-2025;1001;1000;Salg til kunde A;-1000
```

**Forklaring af kolonner:**
- **Dato**: Transaktionsdato (dd-mm-yyyy)
- **Bilagsnummer**: Unikt nummer for hver transaktion (f.eks. fakturanummer)
- **Konto**: Kontonummer fra din kontoplan
- **Tekst**: Beskrivelse af transaktionen
- **Beløb**: Beløb UDEN moms (positive = indtægt/aktiv, negative = udgift/passiv)

## 💡 Hvordan fungerer dobbelt bogføring?

Hver transaktion skal **balancere** - summen af alle beløb skal være 0.

**Eksempel - Salg for 1.000 kr:**
```csv
15-01-2025;1001;5000;Salg til kunde A;1000    (indtægt)
15-01-2025;1001;1000;Salg til kunde A;-1000   (penge ind på bank)
```

**Eksempel - Køb for 800 kr:**
```csv
16-01-2025;1002;6000;Indkøb af varer;800      (udgift)
16-01-2025;1002;1000;Indkøb af varer;-800     (penge ud af bank)
```

## 🧾 Automatisk moms

Programmet beregner **automatisk moms** på konti med momstype:

**Ved salg (UDG moms):**
```csv
15-01-2025;1001;5000;Salg;1000        (du bogfører)
15-01-2025;1001;1000;Salg;-1000       (du bogfører)
```

**Programmet tilføjer automatisk:**
```csv
15-01-2025;1001;5000;Moms af Salg;-250     (moms af salg)
15-01-2025;1001;3000;Moms af Salg;250      (skyldig moms)
```

**Resultat:** Kunden betaler 1.250 kr (1.000 + 250 moms), du skylder 250 kr i moms.

**Ved køb (INDG moms):**
```csv
16-01-2025;1002;6000;Indkøb;800       (du bogfører)  
16-01-2025;1002;1000;Indkøb;-800      (du bogfører)
```

**Programmet tilføjer automatisk:**
```csv
16-01-2025;1002;6000;Moms af Indkøb;-200     (moms af indkøb)
16-01-2025;1002;2000;Moms af Indkøb;200      (tilgodehavende moms)
```

**Resultat:** Du betaler 1.000 kr (800 + 200 moms), du får 200 kr retur i moms.

## 🎯 Primo posteringer (åbningsbalancer)

Primo = åbningsbalancer fra sidste år. Brug **negative bilagsnumre**:

**Eksempel - `posteringer-primo.csv`:**
```csv
Dato;Bilagsnummer;Konto;Tekst;Beløb
;-1;1000;Overført fra sidste år;50000
;-1;4000;Overført fra sidste år;-50000
```

**Vigtigt:**
- Lad dato-feltet være **tomt** - programmet sætter automatisk regnskabsårets startdato
- Brug **negative bilagsnumre** (-1, -2, -3...)
- Primo må **kun** bogføres på **statuskonti** (ikke driftskonti)
- Programmet tilføjer automatisk "PRIMO:" foran teksten

## ⚠️ Valideringsregler

Programmet tjekker at:

### Regnskab:
- Startdato er før slutdato
- Momsprocent er mellem 0 og 50%

### Kontoplan:
- Kontonumre er mellem 1 og 1.000.000
- Kontotype er enten "drift" eller "status"  
- Momstype er "INGEN", "UDG" eller "INDG"

### Posteringer:
- Dato er inden for regnskabsåret
- Bilagsnummer er mellem -1.000.000 og 1.000.000 (ikke 0)
- Kontoen findes i kontoplanen
- Tekst er mellem 3 og 200 tegn
- Beløb er ikke 0
- Hver posteringsfil er balanceret (summer til 0)
- Primo posteringer kun på statuskonti

## 📊 Rapporter - Hvad får du?

Programmet genererer automatisk disse rapporter i `MAPPE_NAVN\out`:

### 1. `kontoplan.txt` - Oversigt over alle konti
```
Kontoplan: Mit Firma
===================

1000: Bank (status, INGEN)
5000: Salg (drift, UDG)
6000: Indkøb (drift, INDG)
```

### 2. `balance.txt` - Resultat og status
```
Balance: Mit Firma
==================

DRIFT / RESULTAT
--------------------------------------------------
5000: Salg                                    1.000,00
6000: Indkøb                                   -800,00

STATUS / BALANCE  
--------------------------------------------------
1000: Bank                                      200,00
```

### 3. `kontokort.txt` - Detaljer for hver konto
```
=== KONTOKORT: 1000 Bank ===

Dato       Bilag  Tekst                    Beløb    Saldo
--------------------------------------------------------
15-01-2025  1001  Salg til kunde A        -1.000,00  -1.000,00
16-01-2025  1002  Indkøb af varer            800,00    -200,00
```

### 4. `posteringsliste.txt` - Alle posteringer
```
Posteringsliste: Mit Firma
=========================

Dato       Bilag  Konto  Tekst              Beløb
--------------------------------------------------
15-01-2025  1001   5000  Salg til kunde A    1.000,00
15-01-2025  1001   1000  Salg til kunde A   -1.000,00
```

### 5. Excel-filer (CSV)
- `balance.csv` - Til import i Excel
- `posteringsliste.csv` - Til import i Excel

## 🔧 Fejlfinding

### "Posteringsfil er ikke balanceret"
**Problem:** Summen af beløb er ikke 0
**Løsning:** Tjek at positive og negative beløb summer til 0

### "Kontoen findes ikke i kontoplanen"  
**Problem:** Du bruger et kontonummer som ikke er defineret
**Løsning:** Tilføj kontoen til `kontoplan.csv` eller ret kontonummeret

### "Primo posteringer må kun bogføres på statuskonti"
**Problem:** Du forsøger primo på en driftskonto
**Løsning:** Brug kun statuskonti (bank, kasse, gæld, egenkapital) til primo

### "Posteringsdato skal være inden for regnskabsperioden"
**Problem:** Datoen er uden for regnskabsåret
**Løsning:** Ret datoen til at være mellem start- og slutdato

## 📚 Eksempel - Komplet regnskab

**Mappe: `eksempel_firma`**

**`regnskab.csv`:**
```csv
navn;startdato;slutdato;momsprocent
Eksempel Firma;01-01-2025;31-12-2025;25
```

**`kontoplan.csv`:**
```csv
nr;navn;type;moms
1000;Bank;status;INGEN
4000;Egenkapital;status;INGEN
5000;Salg;drift;UDG
6000;Indkøb;drift;INDG
2000;Tilgodehavende moms;status;INGEN
3000;Skyldig moms;status;INGEN
```

**`posteringer-primo.csv`:**
```csv
Dato;Bilagsnummer;Konto;Tekst;Beløb
;-1;1000;Åbningsbalance;10000
;-1;4000;Åbningsbalance;-10000
```

**`posteringer-jan.csv`:**
```csv
Dato;Bilagsnummer;Konto;Tekst;Beløb
10-01-2025;1001;5000;Salg faktura 1001;2000
10-01-2025;1001;1000;Salg faktura 1001;-2000
15-01-2025;1002;6000;Indkøb af varer;1000
15-01-2025;1002;1000;Indkøb af varer;-1000
```

**Kør programmet:**
```
dotnet run --input eksempel_firma
```

**Resultat:** Rapporter i `eksempel_firma\out` med automatisk moms!

## 🎓 Tips til studerende

1. **Start simpelt** - Begynd med få konti og transaktioner
2. **Tjek altid balancen** - Hver posteringsfil skal summere til 0
3. **Forstå moms** - Programmet hjælper, men du skal vide hvornår der er moms
4. **Brug primo korrekt** - Kun på statuskonti med negative bilagsnumre
5. **Læs fejlmeddelelserne** - De fortæller præcist hvad der er galt
6. **Eksperimenter** - Lav testdata og se hvordan rapporterne ser ud

## 📞 Hjælp

Hvis du får fejl:
1. Læs fejlmeddelelsen grundigt
2. Tjek at alle filer har korrekt format
3. Kontroller at posteringsfiler balancerer
4. Sørg for at alle konti er defineret i kontoplanen

**Held og lykke med dit regnskab! 🎉**