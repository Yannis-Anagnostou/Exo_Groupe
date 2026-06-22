# CI avec GitHub Actions pour le projet OrderManagement

## 1. Objectif

Ce document explique comment mettre en place une **CI** avec **GitHub Actions**dans un projet donné.

L'objectif de la CI est de vérifier automatiquement que le projet :

- restaure correctement ses dépendances NuGet ;
- compile correctement ;
- lance les tests quand un projet de tests existe ;
- publie l'API dans un dossier prêt pour un déploiement ;
- sauvegarde le résultat sous forme d'artifact GitHub Actions.

---

## 2. Rappel : qu'est-ce que la CI ?

La **CI**, ou **Continuous Integration**, signifie **intégration continue**.

L'idée est simple : à chaque modification importante du code, on lance automatiquement des vérifications.

Dans ce projet ASP.NET Core, la CI peut répondre à cette question :

> Est-ce que l'API OrderManagement fonctionne toujours après cette modification ?

Dans un projet d'équipe, c'est très utile, car une personne peut modifier les catégories, une autre les commandes, une autre l'authentification, etc. La CI permet de détecter rapidement si une modification casse le projet.

---

## 3. GitHub Actions dans ce projet

GitHub Actions utilise des fichiers YAML placés dans le dossier :

```text
.github/workflows/
```

Pour ce projet, on peut créer ce fichier :

```text
.github/workflows/ci.yml
```

Ce fichier décrit ce que GitHub doit faire automatiquement.

---

## 4. Commandes .NET utilisées par la CI

Avant de comprendre le YAML, il faut connaître les commandes principales.

### Restaurer les dépendances

```bash
dotnet restore OrderManagement.slnx
```

Cette commande télécharge les packages NuGet nécessaires aux projets.

Dans ce projet, cela concerne par exemple :

- Entity Framework Core ;
- SQL Server provider ;
- JWT Bearer authentication ;
- Scalar / OpenAPI.

---

### Compiler le projet

```bash
dotnet build OrderManagement.slnx --configuration Release --no-restore
```

Cette commande compile tous les projets de la solution.

On utilise `--configuration Release` pour compiler dans une configuration proche de la production.

On utilise `--no-restore` parce que `dotnet restore` a déjà été exécuté juste avant.

---

### Lancer les tests

```bash
dotnet test OrderManagement.slnx --configuration Release --no-build
```

Cette commande lance les tests automatisés.

Il n'y a pas encore de projet de tests. Le deuxième document explique comment ajouter un projet xUnit dans :

```text
tests/OrderManagement.Tests/
```

Une fois ce projet ajouté à la solution, `dotnet test OrderManagement.slnx` lancera les tests automatiquement.

---

### Publier l'API

```bash
dotnet publish src/OrderManagement.API/OrderManagement.API.csproj --configuration Release --no-restore --output ./publish
```

Cette commande prépare l'API pour un déploiement.

Le dossier `publish` contient les fichiers générés nécessaires pour exécuter l'application.

---

## 5. Workflow GitHub Actions recommandé

Créer le fichier suivant :

```text
.github/workflows/ci.yml
```

Puis ajouter ce contenu :

```yaml
name: CI OrderManagement ASP.NET Core 10

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

env:
  DOTNET_VERSION: '10.0.x'
  SOLUTION_PATH: 'OrderManagement.slnx'
  API_PROJECT_PATH: 'src/OrderManagement.API/OrderManagement.API.csproj'
  PUBLISH_DIR: './publish'
  TEST_RESULTS_DIR: './TestResults'

permissions:
  contents: read

jobs:
  ci:
    name: Restore, build, test and publish
    runs-on: ubuntu-latest

    steps:
      - name: Récupérer le code
        uses: actions/checkout@v4

      - name: Installer .NET 10
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Afficher les informations .NET
        run: dotnet --info

      - name: Restaurer les dépendances
        run: dotnet restore ${{ env.SOLUTION_PATH }}

      - name: Compiler la solution
        run: dotnet build ${{ env.SOLUTION_PATH }} --configuration Release --no-restore

      - name: Lancer les tests
        run: >
          dotnet test ${{ env.SOLUTION_PATH }}
          --configuration Release
          --no-build
          --logger "trx;LogFileName=test-results.trx"
          --results-directory ${{ env.TEST_RESULTS_DIR }}

      - name: Sauvegarder les résultats de tests
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: ${{ env.TEST_RESULTS_DIR }}

      - name: Publier l'API
        run: >
          dotnet publish ${{ env.API_PROJECT_PATH }}
          --configuration Release
          --no-restore
          --output ${{ env.PUBLISH_DIR }}

      - name: Sauvegarder l'application publiée
        uses: actions/upload-artifact@v4
        with:
          name: ordermanagement-api-publish
          path: ${{ env.PUBLISH_DIR }}
```

---

## 6. Explication du workflow

### Nom du workflow

```yaml
name: CI OrderManagement ASP.NET Core 10
```

C'est le nom visible dans l'onglet **Actions** de GitHub.

---

### Déclencheurs

```yaml
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:
```

Ce workflow se lance :

- quand quelqu'un fait un `push` sur `main` ;
- quand une pull request vise `main` ;
- quand on le lance manuellement depuis GitHub.

`workflow_dispatch` est pratique pour relancer la CI à la main.

---

### Variables globales

```yaml
env:
  DOTNET_VERSION: '10.0.x'
  SOLUTION_PATH: 'OrderManagement.slnx'
  API_PROJECT_PATH: 'src/OrderManagement.API/OrderManagement.API.csproj'
```

Ces variables évitent de répéter les chemins partout.

Si le nom du projet change, il suffit de modifier ces variables.

---

### Permissions minimales

```yaml
permissions:
  contents: read
```

Le workflow a seulement le droit de lire le contenu du dépôt.

C'est une bonne pratique de sécurité : on ne donne pas plus de permissions que nécessaire.

---

### Runner

```yaml
runs-on: ubuntu-latest
```

Le job s'exécute sur une machine Ubuntu fournie par GitHub.

Pour une API ASP.NET Core classique, Ubuntu est suffisant, car .NET est multiplateforme.

---

### Checkout

```yaml
- name: Récupérer le code
  uses: actions/checkout@v4
```

Cette étape télécharge le code du dépôt dans le runner.

Sans cette étape, GitHub Actions lance une machine vide qui ne contient pas le projet.

---

### Setup .NET 10

```yaml
- name: Installer .NET 10
  uses: actions/setup-dotnet@v5
  with:
    dotnet-version: ${{ env.DOTNET_VERSION }}
```

Cette étape installe le SDK .NET 10.

Ensuite, le runner peut exécuter :

```bash
dotnet restore
dotnet build
dotnet test
dotnet publish
```

---

### Restore

```yaml
- name: Restaurer les dépendances
  run: dotnet restore ${{ env.SOLUTION_PATH }}
```

Cette étape récupère les packages NuGet.

Si elle échoue, les causes probables sont :

- un package introuvable ;
- une version incompatible ;
- une source NuGet privée manquante ;
- un problème réseau temporaire.

---

### Build

```yaml
- name: Compiler la solution
  run: dotnet build ${{ env.SOLUTION_PATH }} --configuration Release --no-restore
```

Cette étape compile les quatre projets :

- `OrderManagement.API` ;
- `OrderManagement.Application` ;
- `OrderManagement.Domain` ;
- `OrderManagement.Infrastructure`.

Si cette étape échoue, le projet ne compile pas.

---

### Test

```yaml
- name: Lancer les tests
  run: dotnet test ...
```

Cette étape lance les tests xUnit une fois le projet de tests ajouté.

Le workflow génère aussi un fichier `.trx`, qui est un format de résultat de tests lisible par plusieurs outils.

---

### Artifact des tests

```yaml
- name: Sauvegarder les résultats de tests
  if: always()
  uses: actions/upload-artifact@v4
```

`if: always()` signifie que les résultats sont sauvegardés même si les tests échouent.

C'est utile pour pouvoir analyser les erreurs après coup.

---

### Publish de l'API

```yaml
- name: Publier l'API
  run: dotnet publish ...
```

Cette étape prépare le projet `OrderManagement.API` pour un déploiement.

Le résultat est placé dans :

```text
./publish
```

---

### Artifact de l'application publiée

```yaml
- name: Sauvegarder l'application publiée
  uses: actions/upload-artifact@v4
```

Cette étape permet de télécharger le résultat du `dotnet publish` depuis GitHub Actions.

C'est pratique pour vérifier ce qui serait envoyé en déploiement.

---

## 7. Version avec déploiement simulé

Pour montrer la différence entre CI et CD, on peut ajouter un job `deploy` simulé.

```yaml
name: CI/CD OrderManagement ASP.NET Core 10

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

env:
  DOTNET_VERSION: '10.0.x'
  SOLUTION_PATH: 'OrderManagement.slnx'
  API_PROJECT_PATH: 'src/OrderManagement.API/OrderManagement.API.csproj'
  PUBLISH_DIR: './publish'

permissions:
  contents: read

jobs:
  ci:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - run: dotnet restore ${{ env.SOLUTION_PATH }}
      - run: dotnet build ${{ env.SOLUTION_PATH }} --configuration Release --no-restore
      - run: dotnet test ${{ env.SOLUTION_PATH }} --configuration Release --no-build
      - run: dotnet publish ${{ env.API_PROJECT_PATH }} --configuration Release --no-restore --output ${{ env.PUBLISH_DIR }}

      - uses: actions/upload-artifact@v4
        with:
          name: ordermanagement-api-publish
          path: ${{ env.PUBLISH_DIR }}

  deploy:
    runs-on: ubuntu-latest
    needs: ci
    if: github.ref == 'refs/heads/main'

    steps:
      - name: Déploiement simulé
        run: echo "Déploiement de OrderManagement.API en production"
```

La partie importante est :

```yaml
needs: ci
```

Le déploiement attend que la CI soit terminée.

Et :

```yaml
if: github.ref == 'refs/heads/main'
```

Le déploiement ne se lance que sur `main`.

---

## 8. Secrets et configuration

Le projet utilise une configuration JWT et une connexion SQL Server dans `appsettings.json`.

En production, il ne faut pas mettre les vraies valeurs sensibles dans le dépôt.

Il vaut mieux utiliser les secrets GitHub Actions :

```yaml
env:
  ConnectionStrings__DefaultConnection: ${{ secrets.DEFAULT_CONNECTION }}
  Jwt__Key: ${{ secrets.JWT_KEY }}
  Jwt__Issuer: ${{ secrets.JWT_ISSUER }}
  Jwt__Audience: ${{ secrets.JWT_AUDIENCE }}
```

En ASP.NET Core, les doubles underscores `__` permettent de cibler une configuration imbriquée.

Par exemple :

```text
ConnectionStrings__DefaultConnection
```

correspond à :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  }
}
```

---

## 9. Ce qu'il faut retenir

Pour ce projet, la CI sert à automatiser cette chaîne :

```text
Push ou pull request
        ↓
GitHub Actions démarre
        ↓
Checkout du code
        ↓
Installation du SDK .NET 10
        ↓
Restore NuGet
        ↓
Build de OrderManagement.slnx
        ↓
Tests xUnit
        ↓
Publish de OrderManagement.API
        ↓
Artifacts téléchargeables
```

Résumé :

```text
CI = vérifier automatiquement le code
GitHub Actions = outil qui exécute l'automatisation
OrderManagement.slnx = solution à restaurer, compiler et tester
OrderManagement.API = projet à publier
```

---

## 10. Références utiles

- GitHub Actions : https://docs.github.com/actions
- Action checkout : https://github.com/actions/checkout
- Action setup-dotnet : https://github.com/actions/setup-dotnet
- Action upload-artifact : https://github.com/actions/upload-artifact
- CLI .NET : https://learn.microsoft.com/dotnet/core/tools/
- `dotnet test` : https://learn.microsoft.com/dotnet/core/tools/dotnet-test
- `dotnet publish` : https://learn.microsoft.com/dotnet/core/tools/dotnet-publish
