# Branche feature/categories — Documentation

Cette branche contient l'implémentation du module de **Gestion des Catégories** conformément aux spécifications de `exo.md`.

---

## 📁 Endpoints Implémentés (`/api/categories`)

* **`GET /api/categories`** : Liste toutes les catégories. (Public)
* **`GET /api/categories/{id}`** : Récupère une catégorie par son identifiant. (Public)
* **`POST /api/categories`** : Crée une nouvelle catégorie. (Sécurisé - Rôle **`Admin`** requis)
* **`PUT /api/categories/{id}`** : Modifie une catégorie existante. (Sécurisé - Rôle **`Admin`** requis)
* **`DELETE /api/categories/{id}`** : Supprime une catégorie. (Sécurisé - Rôle **`Admin`** requis)

---

## ⚙️ Règles métiers & Validations

1. **Intégrité de la suppression** : Une catégorie ne peut pas être supprimée si elle contient encore des produits. Si c'est le cas, l'API renvoie proprement une erreur `400 Bad Request` avec un message adapté (géré par le `ExceptionMiddleware`).
2. **Validation des entrées (DTO)** : Le nom de la catégorie est obligatoire et limité à 150 caractères maximum. Les descriptions sont limitées à 500 caractères.
3. **Format des erreurs unifié** : 
   * En cas de validation invalide (ex: nom manquant), l'API renvoie un statut `400` avec une liste d'erreurs formatée.
   * En cas de ressource inexistante, l'API renvoie un statut `404` avec un message clair.

---

## 🚀 Comment tester sur cette branche ?

1. **Mettre à jour la base de données** (applique les tables `Categories` et de gestion des migrations) :
   ```bash
   dotnet ef database update --project src/OrderManagement.Infrastructure/OrderManagement.Infrastructure.csproj --startup-project src/OrderManagement.API/OrderManagement.API.csproj
   ```
2. **Lancer l'application** :
   ```bash
   dotnet run --project src/OrderManagement.API/OrderManagement.API.csproj
   ```
3. **Accéder à Swagger UI** à la racine de l'hôte (ex: `https://localhost:7198/`) pour tester les routes publiques et protégées.