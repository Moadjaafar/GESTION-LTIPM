
# GESTION-LTIPN - Système de Gestion des Réservations de Transport

## 📋 Vue d'ensemble du projet

Application ASP.NET Core MVC 8.0 pour la gestion des réservations de transport de marchandises (Congolé/Conserve) entre Agadir/Casablanca et Dakhla.

**Base de données:** `LTIPM_db` (SQL Server)
**Chaîne de connexion:** Voir `appsettings.json`

---

## ✅ Fonctionnalités Implémentées (Session 1)

### 1. **Authentification & Autorisation**
- ✅ Système de login sans hash (mot de passe en clair comme demandé)
- ✅ Cookie authentication
- ✅ Page de login avec template LTPM
- ✅ Protection globale de toutes les pages (redirection vers login si non authentifié)
- ✅ Logout fonctionnel
- ✅ Affichage du nom d'utilisateur et société dans le header

**Rôles utilisateur:**
- `Admin` - Accès complet
- `Validator` - Validation des réservations (non implémenté encore)
- `Booking_Agent` - Création de réservations uniquement

### 2. **Module Réservations (Bookings)**

#### Modèles créés:
- `User` - Utilisateurs du système
- `Society` - Sociétés clientes
- `Booking` - Réservations
- `Camion` - Flotte de camions
- `Voyage` - Voyages individuels (modèle créé, fonctionnalités non implémentées)

#### Fonctionnalités Réservations:
- ✅ **Création de réservation** (Admin + Booking_Agent uniquement)
  - Sélection de société
  - Type de voyage: **Congolé** ou **Conserve**
  - **Nbr_LTC**: Nombre de voyages (1-100)
  - Notes optionnelles
  - Génération automatique de référence: `BK{YYYYMMDD}{001-999}`

- ✅ **Liste des réservations**
  - Filtrage par rôle (Booking_Agent voit uniquement ses propres réservations)
  - Affichage: Référence, Société, Type, Nbr LTC, Statut, Date de création, Créateur

- ✅ **Détails d'une réservation**
  - Affichage de toutes les informations
  - Bouton "Supprimer" (seulement si statut = Pending)

- ✅ **Suppression**
  - Uniquement pour les réservations en attente (Pending)
  - Autorisée pour le créateur ou Admin
  - Confirmation obligatoire

#### Statuts des réservations:
- `Pending` - En attente de validation
- `Validated` - Validée (workflow non implémenté)
- `Completed` - Complétée (workflow non implémenté)
- `Cancelled` - Annulée (workflow non implémenté)

### 3. **Interface Utilisateur**
- ✅ Template admin LTPM intégré
- ✅ Navigation avec sidebar verticale/horizontale
- ✅ Menu: Tableau de bord, Réservations, Réception MP
- ✅ Mode clair/sombre
- ✅ Design responsive
- ✅ Messages de succès/erreur avec TempData
- ✅ Validation côté client et serveur

---

## 🗄️ Structure de la Base de Données

### Tables Implémentées:

```sql
-- Utilisateurs
Users (UserId, Username, Password, Role, SocietyId, TypeVoyage, IsActive, CreatedAt, UpdatedAt)

-- Sociétés
Societies (SocietyId, SocietyName, Address, City, Phone, Email, IsActive, CreatedAt, UpdatedAt)

-- Réservations
Bookings (BookingId, BookingReference, SocietyId, TypeVoyage, Nbr_LTC,
          CreatedByUserId, ValidatedByUserId, BookingStatus, CreatedAt, ValidatedAt, Notes)

-- Camions
Camions (CamionId, CamionMatricule, DriverName, DriverPhone, CamionType,
         SocietyId, IsActive, CreatedAt, UpdatedAt)

-- Voyages (modèle créé, fonctionnalités non implémentées)
Voyages (VoyageId, BookingId, VoyageNumber, SocietyPrincipaleId, SocietySecondaireId,
         CamionId, DepartureCity, DepartureDate, DepartureTime, DepartureType,
         ReceptionDate, ReceptionTime, ReturnDepartureDate, ReturnDepartureTime,
         ReturnArrivalCity, ReturnArrivalDate, IsValidated, ValidatedByUserId,
         ValidatedAt, PricePrincipale, PriceSecondaire, Currency, VoyageStatus,
         CreatedAt, UpdatedAt)
```

---

## 🚧 À Faire (Prochaines Sessions)

### 1. **Migration de la Base de Données**
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2. **Données de Test**
Créer des utilisateurs et sociétés de test:
```sql
-- Utilisateur Admin
INSERT INTO Users (Username, Password, Role, IsActive)
VALUES ('admin', 'admin123', 'Admin', 1);

-- Utilisateur Booking Agent
INSERT INTO Users (Username, Password, Role, IsActive)
VALUES ('agent1', 'agent123', 'Booking_Agent', 1);

-- Utilisateur Validator
INSERT INTO Users (Username, Password, Role, IsActive)
VALUES ('validator1', 'val123', 'Validator', 1);

-- Sociétés
INSERT INTO Societies (SocietyName, City, IsActive)
VALUES ('KING PELAGIQUE GROUP', 'Dakhla', 1);
```

### 3. **Module Validation des Réservations** (Rôle Validator)
- [ ] Page de liste des réservations en attente
- [ ] Action de validation (change statut de Pending → Validated)
- [ ] Historique des validations
- [ ] Notifications ou dashboard pour les réservations en attente

### 4. **Module Voyages** (Séparé des Réservations)
Géré par un rôle différent (à définir):

- [ ] **Création de voyages** pour une réservation validée
  - Limité au nombre de LTC de la réservation
  - Numérotation séquentielle (VoyageNumber: 1, 2, 3...)
  - Informations de départ:
    - Ville: Agadir ou Casablanca
    - Date et heure de départ
    - Type: **Emballage** (avec cargo) ou **Empty** (à vide)
    - Société principale (de la réservation)
    - Société secondaire (si type = Emballage)
  - Assignation de camion (optionnel au début)

- [ ] **Suivi du voyage**
  - Réception à Dakhla (date/heure)
  - Départ retour de Dakhla (date/heure)
  - Arrivée retour (ville + date)
  - Changement de statut: Planned → InProgress → Completed

- [ ] **Tarification**
  - Assignation du prix pour société principale
  - Assignation du prix pour société secondaire (si applicable)
  - Devise: MAD

- [ ] **Liste et détails des voyages**
- [ ] **Validation des voyages** (si nécessaire)

### 5. **Module Réception MP** (Menu existant)
- [ ] À définir selon les besoins métier
- [ ] Probablement lié à la réception à Dakhla

### 6. **Dashboard (Tableau de bord)**
- [ ] Statistiques des réservations (en attente, validées, complétées)
- [ ] Nombre de voyages en cours
- [ ] Graphiques par société
- [ ] Calendrier des départs/arrivées

### 7. **Gestion des Camions**
- [ ] CRUD camions (matricule, chauffeur, type, société)
- [ ] Disponibilité des camions
- [ ] Historique des voyages par camion

### 8. **Gestion des Sociétés**
- [ ] CRUD sociétés
- [ ] Liste des réservations par société
- [ ] Statistiques par société

### 9. **Gestion des Utilisateurs** (Admin uniquement)
- [ ] CRUD utilisateurs
- [ ] Assignation de rôles
- [ ] Assignation de sociétés
- [ ] Gestion TypeVoyage par utilisateur

### 10. **Améliorations**
- [ ] Filtres et recherche avancée
- [ ] Export Excel/PDF
- [ ] Audit trail (logs de toutes les actions)
- [ ] Notifications (email, push)
- [ ] Pagination pour les grandes listes
- [ ] Validation métier plus poussée

---

## 📂 Structure du Projet

```
GESTION-LTIPN/
├── Controllers/
│   ├── AccountController.cs      # Authentification
│   ├── BookingController.cs      # Gestion réservations
│   └── HomeController.cs         # Dashboard
├── Data/
│   └── ApplicationDbContext.cs   # EF Core DbContext
├── Models/
│   ├── User.cs                   # Entité utilisateur
│   ├── Society.cs                # Entité société
│   ├── Booking.cs                # Entité réservation
│   ├── Camion.cs                 # Entité camion
│   ├── Voyage.cs                 # Entité voyage
│   ├── LoginViewModel.cs         # ViewModel login
│   └── BookingViewModel.cs       # ViewModel réservation
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml          # Page de login
│   │   └── AccessDenied.cshtml   # Page accès refusé
│   ├── Booking/
│   │   ├── Index.cshtml          # Liste réservations
│   │   ├── Create.cshtml         # Créer réservation
│   │   └── Details.cshtml        # Détails réservation
│   └── Shared/
│       ├── _Layout.cshtml        # Layout principal
│       └── _ValidationScriptsPartial.cshtml
├── wwwroot/
│   └── assets/                   # Template LTPM (CSS, JS, images)
├── Program.cs                    # Configuration app
├── appsettings.json             # Configuration (connection string)
└── GESTION-LTIPN.csproj         # Projet .NET
```

---

## 🔑 Points Importants

### Règles Métier Implémentées:
1. **Booking_Agent** ne peut voir que ses propres réservations
2. Seules les réservations **Pending** peuvent être supprimées
3. Seuls **Admin** et **Booking_Agent** peuvent créer des réservations
4. Seul le créateur ou Admin peut supprimer une réservation
5. Référence de réservation auto-générée et unique

### Règles Métier à Implémenter:
1. Nombre de voyages créés ne doit pas dépasser Nbr_LTC
2. Si DepartureType = "Emballage", SocietySecondaireId est obligatoire
3. Si DepartureType = "Empty", SocietySecondaireId doit être null
4. PriceSecondaire n'est applicable que si SocietySecondaireId existe
5. Les voyages ne peuvent être créés que pour des réservations validées

### Sécurité:
- ⚠️ **Mots de passe en clair** (comme demandé par le client)
- ✅ Protection CSRF avec AntiForgeryToken
- ✅ Autorisation basée sur les rôles
- ✅ Validation des entrées côté serveur et client

---

## 🚀 Commandes Utiles

### Développement
```bash
# Restaurer les packages
dotnet restore

# Compiler le projet
dotnet build

# Lancer l'application
dotnet run

# Accès: https://localhost:5001
```

### Entity Framework
```bash
# Créer une migration
dotnet ef migrations add MigrationName

# Appliquer les migrations
dotnet ef database update

# Supprimer la dernière migration
dotnet ef migrations remove

# Voir l'état des migrations
dotnet ef migrations list
```

---

## 📝 Notes de Session

### Session 1 (Date: 2025-01-09)
**Ce qui a été fait:**
- Configuration initiale du projet
- Authentification complète
- Module Bookings (CRUD basique)
- Layout et navigation
- Modèles de base de données

**Décisions importantes:**
- Les voyages seront gérés séparément par un autre rôle
- La validation des réservations sera un processus à part (rôle Validator)
- Nbr_LTC ajouté pour définir le nombre de voyages planifiés

**Prochaine étape prioritaire:**
- Créer les migrations et la base de données
- Implémenter le module de validation (rôle Validator)
- Implémenter le module de gestion des voyages

---

## 🎯 Objectif Final

Système complet de gestion des réservations et voyages de transport avec:
- Workflow: Création → Validation → Planification voyages → Suivi → Tarification → Complétion
- Tableaux de bord et statistiques
- Gestion multi-rôles avec permissions granulaires
- Traçabilité complète des opérations
- Interface moderne et responsive

---

**Dernière mise à jour:** Session 1 - 2025-01-09
