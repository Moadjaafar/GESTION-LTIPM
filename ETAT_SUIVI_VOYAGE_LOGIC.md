# État de Suivi des Voyages - Documentation Technique

## Vue d'ensemble

Le module **État de Suivi des Voyages** (`EtatSuiviVoyageController`) génère un rapport détaillé de tous les voyages avec leurs informations complètes, durées calculées, et permet l'export Excel.

---

## 1. Comportement par Défaut

### Filtre par Défaut
**Condition:** `ReturnArrivalDate == null`

**Signification:** Le système affiche automatiquement tous les voyages qui n'ont **PAS encore terminé leur trajet retour** (pas de date d'arrivée retour enregistrée).

**Titre affiché:** "État de suivi des voyages - Voyages en cours (sans arrivée retour)"

```csharp
if (isDefaultFilter)
{
    query = query.Where(v => v.ReturnArrivalDate == null);
}
```

### Pourquoi ce filtre ?
- Montre les voyages **actifs** ou **en cours**
- Permet de suivre les camions qui sont encore sur la route
- Aide à identifier les voyages qui nécessitent une attention

---

## 2. Filtres Disponibles

Lorsque l'utilisateur applique au moins un filtre, le filtre par défaut est désactivé et seuls les filtres choisis sont appliqués.

| Filtre | Type | Description | Logique SQL |
|--------|------|-------------|-------------|
| **Date Début** | Date | Filtrer par date de départ | `DepartureDate >= dateDebut` |
| **Date Fin** | Date | Filtrer par date de départ | `DepartureDate <= dateFin` |
| **Numéro BK** | Texte | Recherche dans le numéro de booking | `Booking.Numero_BK.Contains(numeroBK)` |
| **Numéro TC** | Texte | Recherche dans le numéro de conteneur | `Numero_TC.Contains(numeroTC)` |
| **Camion** | Liste déroulante | Filtre par camion (départ ou retour) | `CamionFirstDepart == camionId OR CamionSecondDepart == camionId` |
| **Société** | Liste déroulante | Filtre par société (principale ou secondaire) | `SocietyPrincipaleId == societyId OR SocietySecondaireId == societyId` |
| **Statut Voyage** | Liste déroulante | Planned, InProgress, Completed | `VoyageStatus == voyageStatus` |
| **Type Voyage** | Liste déroulante | Congelé, DRY, EMBALLAGE | `Booking.TypeVoyage == typeVoyage` |
| **Ville Départ** | Liste déroulante | Agadir, Casablanca | `DepartureCity == departureCity` |

---

## 3. Structure de la Requête

### Étape 1: Récupération des Données avec Relations

```csharp
var query = _context.Voyages
    .Include(v => v.Booking)              // Réservation associée
    .Include(v => v.SocietyPrincipale)    // Société principale
    .Include(v => v.SocietySecondaire)    // Société secondaire (EMBALLAGE)
    .Include(v => v.CamionFirst)          // Camion aller
    .Include(v => v.CamionSecond)         // Camion retour
    .AsQueryable();
```

### Étape 2: Application des Filtres

Le système applique soit:
- **Le filtre par défaut** (`ReturnArrivalDate == null`)
- **OU les filtres utilisateur** (si au moins un filtre est actif)

### Étape 3: Tri des Résultats

```csharp
.OrderByDescending(v => v.DepartureDate)  // Date de départ la plus récente en premier
.ThenBy(v => v.VoyageNumber)              // Puis par numéro de voyage
```

---

## 4. Logique de Dédoublement des Voyages EMBALLAGE

### Concept Clé

**Un voyage de type EMBALLAGE avec une société secondaire génère DEUX lignes dans le rapport.**

### Pourquoi ?

Un voyage EMBALLAGE implique deux sociétés qui partagent le même camion:
- **Société Principale** : Paye `PricePrincipale`
- **Société Secondaire** : Paye `PriceSecondaire`

Chaque société doit voir sa propre ligne avec son propre prix.

### Exemple Concret

**Voyage physique unique:**
- N° Voyage: 1
- Booking: BK20250120001
- Société Principale: KING PELAGIQUE
- Société Secondaire: MARELUX
- Prix Principal: 8000 MAD
- Prix Secondaire: 4000 MAD

**Résultat dans le rapport (2 lignes):**

| N° OP | Client | Prix | Notes |
|-------|--------|------|-------|
| 1 | KING PELAGIQUE | 8000 MAD | Ligne pour société principale |
| 2 | MARELUX | 4000 MAD | Ligne pour société secondaire |

### Code de Dédoublement

```csharp
// Condition: Voyage a une société secondaire
if (voyage.SocietySecondaireId.HasValue && voyage.SocietySecondaire != null)
{
    // LIGNE 1: Opération pour Société Principale
    voyageItems.Add(new VoyageItemViewModel
    {
        SocietyPrincipale = voyage.SocietyPrincipale?.SocietyName,
        SocietySecondaire = null,  // Masqué
        PricePrincipale = voyage.PricePrincipale,  // Prix principal
        PriceSecondaire = null,  // Masqué
        TypeVoyage = "EMBALLAGE",
        // ... autres champs identiques
    });

    // LIGNE 2: Opération pour Société Secondaire
    voyageItems.Add(new VoyageItemViewModel
    {
        SocietyPrincipale = voyage.SocietySecondaire?.SocietyName,  // Secondaire devient principale
        SocietySecondaire = null,  // Masqué
        PricePrincipale = voyage.PriceSecondaire,  // Prix secondaire devient principal
        PriceSecondaire = null,  // Masqué
        TypeVoyage = "EMBALLAGE",
        // ... autres champs identiques
    });
}
```

### Voyages Normaux (Sans Société Secondaire)

Pour tous les autres voyages (DRY, Congelé, ou EMBALLAGE sans secondaire):
- **Une seule ligne** est générée
- Affiche uniquement la société principale
- Prix principal uniquement

---

## 5. Calcul Automatique des Durées

Le système calcule automatiquement 4 types de durées pour chaque voyage:

### 5.1 Durée Aller Dakhla
**Temps entre le départ et la réception à Dakhla**

```csharp
if (voyage.DepartureDate.HasValue && voyage.ReceptionDate.HasValue)
{
    var departureDateTime = voyage.DepartureDate.Value.Add(voyage.DepartureTime ?? TimeSpan.Zero);
    var receptionDateTime = voyage.ReceptionDate.Value.Add(voyage.ReceptionTime ?? TimeSpan.Zero);
    var duree = receptionDateTime - departureDateTime;
    dureeAllerDakhla = $"{(int)duree.TotalHours}h {duree.Minutes}m";
}
```

**Exemple:** `"14h 30m"` (14 heures et 30 minutes)

### 5.2 Durée Séjour Dakhla
**Temps passé à Dakhla entre la réception et le départ retour**

```csharp
if (voyage.ReceptionDate.HasValue && voyage.ReturnDepartureDate.HasValue)
{
    var receptionDateTime = voyage.ReceptionDate.Value.Add(voyage.ReceptionTime ?? TimeSpan.Zero);
    var returnDepartureDateTime = voyage.ReturnDepartureDate.Value.Add(voyage.ReturnDepartureTime ?? TimeSpan.Zero);
    var duree = returnDepartureDateTime - receptionDateTime;
    dureeSejourDakhla = $"{(int)duree.TotalHours}h {duree.Minutes}m";
}
```

**Exemple:** `"48h 0m"` (2 jours à Dakhla)

### 5.3 Durée Retour
**Temps entre le départ retour de Dakhla et l'arrivée finale**

```csharp
if (voyage.ReturnDepartureDate.HasValue && voyage.ReturnArrivalDate.HasValue)
{
    var returnDepartureDateTime = voyage.ReturnDepartureDate.Value.Add(voyage.ReturnDepartureTime ?? TimeSpan.Zero);
    var returnArrivalDateTime = voyage.ReturnArrivalDate.Value.Add(voyage.ReturnArrivalTime ?? TimeSpan.Zero);
    var duree = returnArrivalDateTime - returnDepartureDateTime;
    dureeRetour = $"{(int)duree.TotalHours}h {duree.Minutes}m";
}
```

**Exemple:** `"15h 45m"`

### 5.4 Durée Totale
**Temps total du voyage complet (aller-retour)**

```csharp
if (voyage.DepartureDate.HasValue && voyage.ReturnArrivalDate.HasValue)
{
    var departureDateTime = voyage.DepartureDate.Value.Add(voyage.DepartureTime ?? TimeSpan.Zero);
    var returnArrivalDateTime = voyage.ReturnArrivalDate.Value.Add(voyage.ReturnArrivalTime ?? TimeSpan.Zero);
    var duree = returnArrivalDateTime - departureDateTime;
    dureeTotale = $"{(int)duree.TotalHours}h {duree.Minutes}m";
}
```

**Exemple:** `"78h 15m"` (3 jours et 6 heures)

### Schéma Temporel

```
DÉPART              RÉCEPTION           DÉPART RETOUR       ARRIVÉE
Agadir              Dakhla              Dakhla              Agadir
  |---Aller Dakhla---|---Séjour Dakhla---|---Retour---|
  |------------------Durée Totale--------------------------|
```

---

## 6. Informations Camions

Chaque voyage suit **deux camions** potentiels:

### Camion Aller (CamionFirst)
- **Champ:** `CamionFirstDepart` (ID)
- **Relation:** `CamionFirst` (objet Camion)
- **Données affichées:**
  - Matricule: `CamionFirst.CamionMatricule`
  - Chauffeur: `CamionFirst.DriverName`

### Camion Retour (CamionSecond)
- **Champ:** `CamionSecondDepart` (ID)
- **Relation:** `CamionSecond` (objet Camion)
- **Données affichées:**
  - Matricule: `CamionSecond.CamionMatricule`
  - Chauffeur: `CamionSecond.DriverName`

**Note:** Les deux camions peuvent être identiques (même camion aller-retour) ou différents.

---

## 7. Export Excel

### Déclenchement
Action: `ExportExcel` avec les mêmes paramètres de filtre

### Colonnes Exportées (32 colonnes)

| # | Colonne | Source | Description |
|---|---------|--------|-------------|
| 1 | ID Voyage | `VoyageId` | Identifiant unique |
| 2 | N° Voyage | `VoyageNumber` | Numéro séquentiel dans le booking |
| 3 | N° TC | `Numero_TC` | Numéro de conteneur/camion |
| 4 | Statut Voyage | `VoyageStatus` | Planned/InProgress/Completed |
| 5 | Référence Booking | `BookingReference` | Ex: BK20250120001 |
| 6 | Type Voyage | `TypeVoyage` | Congelé/DRY/EMBALLAGE |
| 7 | Société Principale | `SocietyPrincipale` | Nom société principale |
| 8 | Société Secondaire | `SocietySecondaire` | Nom société secondaire (si existe) |
| 9 | Type Départ | `DepartureType` | Emballage/Empty |
| 10 | Type Emballage | `Type_Emballage` | Type marchandise |
| 11 | Ville Départ | `DepartureCity` | Agadir/Casablanca |
| 12 | Date Départ | `DepartureDate` | Format: dd/MM/yyyy |
| 13 | Heure Départ | `DepartureTime` | Format: hh:mm |
| 14 | Camion Départ | `CamionFirstMatricule` | Matricule camion aller |
| 15 | Chauffeur Départ | `CamionFirstDriver` | Nom chauffeur aller |
| 16 | Date Réception | `ReceptionDate` | Date arrivée Dakhla |
| 17 | Heure Réception | `ReceptionTime` | Heure arrivée Dakhla |
| 18 | Date Départ Retour | `ReturnDepartureDate` | Date départ de Dakhla |
| 19 | Heure Départ Retour | `ReturnDepartureTime` | Heure départ de Dakhla |
| 20 | Camion Retour | `CamionSecondMatricule` | Matricule camion retour |
| 21 | Chauffeur Retour | `CamionSecondDriver` | Nom chauffeur retour |
| 22 | Ville Arrivée | `ReturnArrivalCity` | Ville arrivée finale |
| 23 | Date Arrivée | `ReturnArrivalDate` | Date arrivée finale |
| 24 | Heure Arrivée | `ReturnArrivalTime` | Heure arrivée finale |
| 25 | Prix Principal | `PricePrincipale` | Prix MAD société principale |
| 26 | Prix Secondaire | `PriceSecondaire` | Prix MAD société secondaire |
| 27 | Devise | `Currency` | Toujours "MAD" |
| 28 | Durée Aller Dakhla | Calculé | Ex: "14h 30m" |
| 29 | Durée Séjour Dakhla | Calculé | Ex: "48h 0m" |
| 30 | Durée Retour | Calculé | Ex: "15h 45m" |
| 31 | Durée Totale | Calculé | Ex: "78h 15m" |
| 32 | Date Création | `CreatedAt` | Date création voyage |

### Formatage Excel
- **En-têtes:** Gras, fond bleu clair, centré
- **Colonnes:** Ajustées automatiquement à la largeur du contenu
- **Nom fichier:** `{Titre filtres}_{YYYYMMDD}.xlsx`
  - Exemple: `État de suivi des voyages - Voyages en cours (sans arrivée retour)_20250120.xlsx`

---

## 8. Structure du ViewModel

### EtatSuiviVoyageViewModel

```csharp
public class EtatSuiviVoyageViewModel
{
    // Valeurs des filtres (pour maintenir l'état du formulaire)
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? NumeroBK { get; set; }
    public string? NumeroTC { get; set; }
    public int? CamionId { get; set; }
    public int? SocietyId { get; set; }
    public string? VoyageStatus { get; set; }
    public string? TypeVoyage { get; set; }
    public string? DepartureCity { get; set; }

    // Listes pour les dropdowns
    public List<Society> Societies { get; set; }
    public List<Camion> Camions { get; set; }
    public List<string> VoyageStatuses { get; set; }  // Planned, InProgress, Completed
    public List<string> TypeVoyages { get; set; }     // Congelé, DRY, EMBALLAGE
    public List<string> DepartureCities { get; set; } // Agadir, Casablanca

    // Résultats filtrés
    public List<VoyageItemViewModel> Voyages { get; set; }
    public string FilteredTitle { get; set; }  // Titre dynamique des filtres
}
```

### VoyageItemViewModel

```csharp
public class VoyageItemViewModel
{
    // Identifiants
    public int VoyageId { get; set; }
    public int VoyageNumber { get; set; }
    public int BookingId { get; set; }

    // Références
    public string? BookingReference { get; set; }
    public string? Numero_BK { get; set; }
    public string? Numero_TC { get; set; }

    // Informations voyage
    public string? VoyageStatus { get; set; }
    public string? TypeVoyage { get; set; }
    public string? DepartureType { get; set; }
    public string? Type_Emballage { get; set; }

    // Sociétés
    public string? SocietyPrincipale { get; set; }
    public string? SocietySecondaire { get; set; }  // Null dans le rapport

    // Départ
    public string? DepartureCity { get; set; }
    public string? DepartureDate { get; set; }      // Format: dd/MM/yyyy
    public string? DepartureTime { get; set; }      // Format: hh:mm

    // Camion Aller
    public string? CamionFirstMatricule { get; set; }
    public string? CamionFirstDriver { get; set; }

    // Réception Dakhla
    public string? ReceptionDate { get; set; }
    public string? ReceptionTime { get; set; }

    // Départ Retour
    public string? ReturnDepartureDate { get; set; }
    public string? ReturnDepartureTime { get; set; }

    // Camion Retour
    public string? CamionSecondMatricule { get; set; }
    public string? CamionSecondDriver { get; set; }

    // Arrivée
    public string? ReturnArrivalCity { get; set; }
    public string? ReturnArrivalDate { get; set; }
    public string? ReturnArrivalTime { get; set; }

    // Prix
    public decimal? PricePrincipale { get; set; }
    public decimal? PriceSecondaire { get; set; }   // Null dans le rapport
    public string? Currency { get; set; }

    // Durées calculées
    public string? DureeAllerDakhla { get; set; }
    public string? DureeSejourDakhla { get; set; }
    public string? DureeRetour { get; set; }
    public string? DureeTotale { get; set; }

    // Métadonnées
    public string? CreatedAt { get; set; }
}
```

---

## 9. Cas d'Usage

### Cas 1: Suivi des Voyages en Cours
**Utilisateur:** Gestionnaire de flotte
**Action:** Accéder à `/EtatSuiviVoyage/Index` sans filtres
**Résultat:** Liste de tous les voyages sans date d'arrivée retour
**Usage:** Identifier les camions encore sur la route

### Cas 2: Rapport Mensuel
**Utilisateur:** Comptable
**Action:** Filtrer par dates (01/01/2025 - 31/01/2025)
**Résultat:** Tous les voyages du mois avec calculs de durées
**Usage:** Facturation clients et analyse performance

### Cas 3: Suivi Société Spécifique
**Utilisateur:** Commercial
**Action:** Filtrer par société (ex: KING PELAGIQUE)
**Résultat:** Tous les voyages pour cette société (principale OU secondaire)
**Usage:** Rapport client personnalisé

### Cas 4: Performance Camion
**Utilisateur:** Chef de parc
**Action:** Filtrer par camion spécifique
**Résultat:** Historique complet du camion
**Usage:** Analyse kilométrage, maintenance préventive

### Cas 5: Export Comptabilité
**Utilisateur:** Directeur financier
**Action:** Filtrer par dates + Type voyage, puis Export Excel
**Résultat:** Fichier Excel avec toutes les données + calculs
**Usage:** Import dans logiciel comptable

---

## 10. Règles Métier Importantes

### ✅ À Retenir

1. **Voyage EMBALLAGE avec secondaire = 2 lignes dans le rapport**
2. **Filtre par défaut = Voyages sans arrivée retour (`ReturnArrivalDate == null`)**
3. **Durées = Calculs automatiques incluant heures ET dates**
4. **Filtres = Désactivent le comportement par défaut**
5. **Export Excel = Même données que l'écran**
6. **Camions = Deux camions possibles (aller/retour)**
7. **Prix = Chaque société voit uniquement SON prix**

### ⚠️ Limitations Actuelles

- Pas de filtre combiné "ET/OU" pour les sociétés
- Pas de regroupement par société dans l'interface
- Pas de graphiques de durées moyennes
- Pas de filtre par statut booking (seulement statut voyage)

---

## 11. Améliorations Futures Possibles

### Fonctionnalités Suggérées

1. **Tableau de bord visuel**
   - Graphique durées moyennes par trajet
   - Carte avec positions GPS des camions
   - Statistiques temps réel

2. **Alertes automatiques**
   - Voyage dépassant durée normale
   - Camion en retard
   - Prix manquant

3. **Export avancé**
   - Format PDF avec graphiques
   - Regroupement par société
   - Totaux et moyennes

4. **Filtres avancés**
   - Recherche multi-critères
   - Filtres sauvegardés
   - Filtres favoris utilisateur

---

## Résumé Technique

| Aspect | Détail |
|--------|--------|
| **Contrôleur** | `EtatSuiviVoyageController` |
| **Action principale** | `Index(...)` avec 9 paramètres optionnels |
| **Méthode de calcul** | `GetFilteredVoyages(...)` |
| **Export** | `ExportExcel(...)` avec ClosedXML |
| **Dédoublement** | Voyages EMBALLAGE → 2 lignes |
| **Filtre défaut** | `ReturnArrivalDate == null` |
| **Relations chargées** | Booking, Societies (x2), Camions (x2) |
| **Colonnes Excel** | 32 colonnes |

---

**Dernière mise à jour:** 20 Janvier 2025
**Version:** 1.0
