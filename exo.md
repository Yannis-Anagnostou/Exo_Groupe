# Énoncé de projet

## Développement backend en condition réelle — ASP.NET Core

## Contexte

Vous faites partie d’une **équipe backend** chargée de développer une API REST en **ASP.NET Core**.

Votre mission est de concevoir, développer, sécuriser, documenter et tester une API backend complète.

Le projet doit simuler une situation professionnelle réelle :

```text
un seul projet commun
une seule API
une seule base de données
plusieurs modules fonctionnels
plusieurs développeurs qui collaborent
```

Aucune interface graphique n’est demandée.

Le backend sera testé et documenté uniquement à travers :

```text
Swagger UI
OpenAPI
```

L’API doit donc être suffisamment claire, cohérente et documentée pour être utilisée directement depuis Swagger.

---

# Objectif général

L’équipe doit développer une **API de gestion de commandes**.

L’API doit permettre de gérer :

```text
les utilisateurs
l’authentification
les rôles
les produits
les catégories
les commandes
les règles métier
la validation des données
les erreurs
la documentation OpenAPI
```

Vous travaillez tous sur **un seul projet commun**.

---

# Scénario métier

Une entreprise souhaite disposer d’un backend pour gérer ses ventes.

L’API doit permettre :

```text
à un visiteur de consulter les produits
à un utilisateur de créer un compte
à un utilisateur de se connecter
à un utilisateur connecté de créer une commande
à un utilisateur connecté de consulter ses propres commandes
à un administrateur de gérer les produits
à un administrateur de gérer les catégories
à un administrateur de consulter toutes les commandes
```

Principe important :

```text
Le backend ne fait jamais confiance aux données reçues.
```

Exemple :

```text
Lorsqu’une commande est créée, le prix des produits ne doit pas être envoyé dans la requête.

Le backend doit :
- vérifier que les produits existent
- récupérer les prix depuis la base de données
- vérifier les quantités
- calculer le total de la commande
```

---

# Organisation du travail

Vous êtes une seule équipe backend.

Le projet sera développé dans :

```text
un seul repository Git
une seule solution ASP.NET Core
une seule base de données Microsoft SQL Server
une seule API commune
```

Le travail sera réparti par modules.

## Répartition possible

| Sous-équipe             | Responsabilité                                      |
| ----------------------- | --------------------------------------------------- |
| Équipe Authentification | Inscription, connexion, JWT, rôles                  |
| Équipe Produits         | Gestion des produits                                |
| Équipe Catégories       | Gestion des catégories                              |
| Équipe Commandes        | Création et consultation des commandes              |
| Équipe Qualité API      | Validation, erreurs, Swagger, documentation OpenAPI |

---

# Base de données imposée

Le projet doit utiliser **Microsoft SQL Server**.

L’équipe devra fournir une configuration claire permettant de lancer le projet et de créer la base de données.


La configuration exacte de la base de données devra être renseignée dans le fichier de configuration du projet.

Les informations sensibles ne doivent pas être exposées publiquement.

---

# Fonctionnalités attendues

## 1. Authentification

L’API doit permettre à un utilisateur de créer un compte et de se connecter.

Endpoints attendus :

```http
POST /api/auth/register
POST /api/auth/login
GET /api/auth/me
```

Règles :

```text
Un utilisateur peut créer un compte.
Un utilisateur peut se connecter.
Après connexion, l’API retourne un token JWT.
Le token JWT permet d’accéder aux endpoints protégés.
Il existe au minimum deux rôles : Admin et User.
Les mots de passe ne doivent jamais être stockés en clair.
```

Swagger doit permettre de tester :

```text
la création d’un compte
la connexion
la récupération du token JWT
l’accès à l’utilisateur connecté
```

---

## 2. Produits

L’API doit permettre de gérer les produits.

Endpoints attendus :

```http
GET /api/products
GET /api/products/{id}
POST /api/products
PUT /api/products/{id}
DELETE /api/products/{id}
```

Règles :

```text
Tout le monde peut consulter les produits.
Seul un administrateur peut créer un produit.
Seul un administrateur peut modifier un produit.
Seul un administrateur peut supprimer un produit.
Un produit doit avoir un nom.
Un produit doit avoir un prix supérieur à 0.
Un produit doit être lié à une catégorie.
Un produit peut avoir un stock.
```

Champs recommandés :

```text
Id
Name
Description
Price
Stock
CategoryId
```

Swagger doit afficher clairement les modèles de requête et de réponse.

---

## 3. Catégories

L’API doit permettre de gérer les catégories de produits.

Endpoints attendus :

```http
GET /api/categories
GET /api/categories/{id}
POST /api/categories
PUT /api/categories/{id}
DELETE /api/categories/{id}
```

Règles :

```text
Tout le monde peut consulter les catégories.
Seul un administrateur peut créer une catégorie.
Seul un administrateur peut modifier une catégorie.
Seul un administrateur peut supprimer une catégorie.
Une catégorie doit avoir un nom.
Une catégorie ne peut pas être supprimée si elle contient encore des produits.
```

Champs recommandés :

```text
Id
Name
Description
```

---

## 4. Commandes

L’API doit permettre à un utilisateur connecté de créer une commande.

Endpoints attendus :

```http
POST /api/orders
GET /api/orders
GET /api/orders/{id}
```

Exemple de requête :

```json
{
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 3,
      "quantity": 1
    }
  ]
}
```

Règles métier :

```text
Un utilisateur doit être connecté pour créer une commande.
Une commande doit contenir au moins un produit.
Chaque quantité doit être supérieure à 0.
Chaque produit demandé doit exister.
Le prix unitaire est récupéré depuis la base de données SQL Server.
Le total de la commande est calculé par le backend.
Un utilisateur ne peut consulter que ses propres commandes.
Un administrateur peut consulter toutes les commandes.
```

Champs recommandés pour une commande :

```text
Id
UserId
OrderDate
TotalAmount
Items
```

Champs recommandés pour une ligne de commande :

```text
Id
OrderId
ProductId
Quantity
UnitPrice
TotalPrice
```

---

# Sécurité attendue

Les endpoints doivent être protégés selon le rôle de l’utilisateur.

| Action                    | Visiteur | User | Admin |
| ------------------------- | -------: | ---: | ----: |
| Consulter les produits    |      Oui |  Oui |   Oui |
| Consulter les catégories  |      Oui |  Oui |   Oui |
| Créer une commande        |      Non |  Oui |   Oui |
| Voir ses commandes        |      Non |  Oui |   Oui |
| Voir toutes les commandes |      Non |  Non |   Oui |
| Créer un produit          |      Non |  Non |   Oui |
| Modifier un produit       |      Non |  Non |   Oui |
| Supprimer un produit      |      Non |  Non |   Oui |
| Créer une catégorie       |      Non |  Non |   Oui |
| Modifier une catégorie    |      Non |  Non |   Oui |
| Supprimer une catégorie   |      Non |  Non |   Oui |

Dans Swagger, il doit être possible d’ajouter un token JWT dans l’en-tête d’autorisation.

Format attendu :

```text
Bearer <token>
```

---

# Contraintes techniques

Le projet doit utiliser obligatoirement :

```text
ASP.NET Core Web API
Entity Framework Core
Microsoft SQL Server
JWT
Swagger
OpenAPI
DTOs
Services
Validation
Gestion d’erreurs
Git
```

Packages NuGet recommandés :

```text
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
Microsoft.AspNetCore.Authentication.JwtBearer
Swashbuckle.AspNetCore
```

---

# Entity Framework Core et migrations

Le projet doit utiliser **Entity Framework Core** pour communiquer avec **Microsoft SQL Server**.

L’équipe doit créer au minimum :

```text
un AppDbContext
les entités principales
une migration initiale
la base de données SQL Server
```

Commandes attendues :

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Les migrations doivent être versionnées dans Git.

---

# Documentation Swagger / OpenAPI

La documentation Swagger / OpenAPI fait partie du livrable.

Elle doit permettre de comprendre et tester l’API sans autre outil.

Elle doit contenir :

```text
la liste des endpoints
les méthodes HTTP
les paramètres attendus
les modèles de requête
les modèles de réponse
les codes de retour possibles
la configuration JWT
les endpoints publics
les endpoints protégés
```

Les endpoints doivent retourner des codes HTTP cohérents :

```text
200 OK
201 Created
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
500 Internal Server Error
```

---

# Structure recommandée

Structure proposée :

```text
Controllers/
Data/
DTOs/
Models/
Services/
Repositories/
Exceptions/
Middlewares/
```

Exemple :

```text
Controllers/
  AuthController.cs
  ProductsController.cs
  CategoriesController.cs
  OrdersController.cs

DTOs/
  Auth/
  Products/
  Categories/
  Orders/

Models/
  User.cs
  Product.cs
  Category.cs
  Order.cs
  OrderItem.cs

Services/
  AuthService.cs
  ProductService.cs
  CategoryService.cs
  OrderService.cs

Data/
  AppDbContext.cs

Middlewares/
  ExceptionMiddleware.cs
```

---

# Règles de qualité

Le projet doit respecter les règles suivantes :

```text
Les controllers ne doivent pas contenir toute la logique métier.
Les données reçues doivent être validées.
Les mots de passe ne doivent jamais être stockés en clair.
Les erreurs doivent être compréhensibles.
Les DTOs doivent être utilisés pour les entrées et les sorties importantes.
Les endpoints protégés doivent nécessiter un token JWT.
Les règles métier doivent être appliquées côté backend.
Le code doit être lisible et organisé.
La documentation Swagger / OpenAPI doit être exploitable.
Les migrations EF Core doivent permettre de recréer la base SQL Server.
```

---

# Format des erreurs

L’API doit retourner des erreurs claires.

Exemple pour une erreur de validation :

```json
{
  "status": 400,
  "message": "Validation failed",
  "errors": {
    "name": [
      "Le nom du produit est obligatoire."
    ],
    "price": [
      "Le prix doit être supérieur à 0."
    ]
  }
}
```

Exemple pour une ressource introuvable :

```json
{
  "status": 404,
  "message": "Produit introuvable."
}
```

Exemple pour un accès interdit :

```json
{
  "status": 403,
  "message": "Vous n'avez pas l'autorisation d'effectuer cette action."
}
```

---

# Organisation Git

Le projet contient une branche principale :

```text
main
```

Chaque sous-équipe travaille sur une branche dédiée :

```text
feature/auth
feature/products
feature/categories
feature/orders
feature/api-quality
```

Avant d’intégrer une fonctionnalité dans `main`, il faut vérifier que :

```text
le projet compile
les endpoints fonctionnent dans Swagger
la documentation OpenAPI est correcte
la base SQL Server est à jour
les migrations sont cohérentes
le code respecte la structure commune
```


# README attendu

Le README doit contenir :

```text
le contexte du projet
les fonctionnalités disponibles
les prérequis techniques
la procédure d’installation
la procédure de lancement
la configuration SQL Server
la procédure de création de la base de données
les commandes EF Core utilisées
l’URL de Swagger
les endpoints principaux
les comptes de test
les rôles disponibles
les membres de l’équipe et leurs responsabilités
```

---

# Résultat attendu

À la fin des 3 jours, l’équipe doit aêtre aller le plus loin possile sur API backend complète, documentée et testable via Swagger / OpenAPI.

L’objectif est de comprendre comment développer un backend en condition réelle :

```text
en équipe
avec Git
avec Microsoft SQL Server
avec Entity Framework Core
avec des migrations
avec une API REST
avec des DTOs
avec des règles métier
avec une authentification
avec des rôles
avec une documentation OpenAPI
avec une démonstration finale via Swagger
```
