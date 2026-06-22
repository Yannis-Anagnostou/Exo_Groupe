# Ajouter des tests xUnit à un projet

## 1. Objectif

Le but est de tester une partie de la logique métier, sans démarrer toute l'API HTTP.

On va créer un projet :

```text
tests/OrderManagement.Tests/
```

Puis on va tester des services existants du projet :

- `CategoryService` ;
- `ServiceOrder`.

---

## 2. Pourquoi xUnit ?

**xUnit** est un framework de tests unitaires pour .NET.

Il permet d'écrire des tests automatisés avec des méthodes décorées par :

```csharp
[Fact]
```

Un test vérifie qu'une petite partie du code se comporte comme prévu.

Exemple simple :

```csharp
[Fact]
public void Addition_Should_Return_Correct_Result()
{
    var result = 2 + 2;

    Assert.Equal(4, result);
}
```

Dans ce projet, les tests sont particulièrement utiles pour vérifier :

- la création d'une catégorie ;
- la suppression impossible d'une catégorie contenant des produits ;
- la création d'une commande ;
- le calcul du total d'une commande ;
- la diminution du stock après commande.

---

## 3. Créer le projet de tests xUnit

Depuis la racine du projet, exécuter :

```bash
dotnet new xunit -n OrderManagement.Tests -o tests/OrderManagement.Tests --framework net10.0
```

Cela crée un nouveau projet de tests dans :

```text
tests/OrderManagement.Tests/
```

---

## 4. Ajouter les références vers les projets existants

Le projet de tests doit pouvoir accéder au code à tester.

Ajouter les références suivantes :

```bash
dotnet add tests/OrderManagement.Tests/OrderManagement.Tests.csproj reference src/OrderManagement.Application/OrderManagement.Application.csproj

dotnet add tests/OrderManagement.Tests/OrderManagement.Tests.csproj reference src/OrderManagement.Infrastructure/OrderManagement.Infrastructure.csproj

dotnet add tests/OrderManagement.Tests/OrderManagement.Tests.csproj reference src/OrderManagement.Domain/OrderManagement.Domain.csproj
```

Le projet de tests pourra alors utiliser :

- les DTOs ;
- les services ;
- les entités ;
- `AppDbContext`.

---

## 5. Ajouter EF Core InMemory

Les services du projet utilisent `AppDbContext`, donc Entity Framework Core.

Pour les tests, on ne veut pas utiliser une vraie base SQL Server.

On peut utiliser une base en mémoire :

```bash
dotnet add tests/OrderManagement.Tests/OrderManagement.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 10.0.8
```

La version `10.0.8` correspond aux packages EF Core déjà utilisés dans les projets du ZIP.

---

## 6. Ajouter le projet de tests à la solution

Le projet utilise une solution au format `.slnx`.

Ajouter le projet de tests à la solution :

```bash
dotnet sln OrderManagement.slnx add tests/OrderManagement.Tests/OrderManagement.Tests.csproj
```

Ensuite, vérifier la solution :

```bash
dotnet sln OrderManagement.slnx list
```

Le projet `OrderManagement.Tests` doit apparaître dans la liste.

---

## 7. Structure attendue après ajout des tests

Après création du projet de tests, on peut avoir cette structure :

```text
OrderManagement.slnx
src/
├── OrderManagement.API/
├── OrderManagement.Application/
├── OrderManagement.Domain/
└── OrderManagement.Infrastructure/

tests/
└── OrderManagement.Tests/
    ├── OrderManagement.Tests.csproj
    ├── Helpers/
    │   └── TestDbContextFactory.cs
    ├── CategoryServiceTests.cs
    └── ServiceOrderTests.cs
```

---

## 8. Créer une factory pour AppDbContext

Créer le fichier :

```text
tests/OrderManagement.Tests/Helpers/TestDbContextFactory.cs
```

Contenu :

```csharp
using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
```

Pourquoi utiliser `Guid.NewGuid()` ?

Chaque test reçoit une base en mémoire différente.

Cela évite qu'un test influence un autre test.

---

## 9. Premier test : créer une catégorie

Créer le fichier :

```text
tests/OrderManagement.Tests/CategoryServiceTests.cs
```

Contenu :

```csharp
using OrderManagement.Application.DTOs.Categories;
using OrderManagement.Infrastructure.Services;
using OrderManagement.Tests.Helpers;

namespace OrderManagement.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateAsync_Should_Create_Category()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryService(context);

        var dto = new CreateCategoryDto
        {
            Name = "Informatique",
            Description = "Produits informatiques"
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal("Informatique", result.Name);
        Assert.Equal("Produits informatiques", result.Description);
        Assert.Single(context.Categories);
    }
}
```

Ce test vérifie que :

- le service crée bien une catégorie ;
- un identifiant est généré ;
- les valeurs sont correctement enregistrées ;
- la base contient bien une catégorie.

---

## 10. Tester une règle métier : ne pas supprimer une catégorie avec produits

Dans `CategoryService`, la méthode `DeleteAsync` interdit la suppression d'une catégorie qui contient encore des produits.

On peut tester cette règle.

Ajouter ce test dans `CategoryServiceTests.cs` :

```csharp
using OrderManagement.Application.Exceptions;
using OrderManagement.Domain.Entities;
```

Puis ajouter la méthode suivante :

```csharp
[Fact]
public async Task DeleteAsync_Should_Throw_BadRequest_When_Category_Has_Products()
{
    // Arrange
    await using var context = TestDbContextFactory.Create();

    var category = new Category
    {
        Name = "Livres",
        Description = "Catégorie avec produits"
    };

    context.Categories.Add(category);
    await context.SaveChangesAsync();

    var product = new Product
    {
        Name = "Clean Code",
        Description = "Livre de programmation",
        Price = 30m,
        Stock = 10,
        CategoryId = category.Id
    };

    context.Products.Add(product);
    await context.SaveChangesAsync();

    var service = new CategoryService(context);

    // Act + Assert
    await Assert.ThrowsAsync<BadRequestException>(() => service.DeleteAsync(category.Id));
}
```

Ce test vérifie une règle importante :

> Une catégorie ne peut pas être supprimée si elle contient encore des produits.

---

## 11. Tester la création d'une commande

Le service `ServiceOrder` crée une commande, calcule le total et diminue le stock.

Créer le fichier :

```text
tests/OrderManagement.Tests/ServiceOrderTests.cs
```

Contenu :

```csharp
using OrderManagement.Application.DTOs.OrderDto;
using OrderManagement.Application.Services.OrderService;
using OrderManagement.Domain.Entities;
using OrderManagement.Tests.Helpers;

namespace OrderManagement.Tests;

public class ServiceOrderTests
{
    [Fact]
    public async Task CreateOrderAsync_Should_Create_Order_And_Decrease_Product_Stock()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var category = new Category
        {
            Name = "Hardware",
            Description = "Matériel informatique"
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Clavier",
            Description = "Clavier mécanique",
            Price = 50m,
            Stock = 4,
            CategoryId = category.Id
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var service = new ServiceOrder(context);

        var dto = new CreateOrderDto
        {
            Items = new List<OrderItemDto>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 2
                }
            }
        };

        // Act
        var result = await service.CreateOrderAsync(dto, userId: 123);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal(100m, result.TotalAmount);

        var updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal(2, updatedProduct!.Stock);

        Assert.Single(context.Orders);
        Assert.Single(context.OrderItems);
    }
}
```

Ce test vérifie que :

- une commande est créée ;
- le total est calculé correctement ;
- le stock du produit est diminué ;
- une ligne de commande est créée.

---

## 12. Tester le cas de stock insuffisant

Toujours dans `ServiceOrderTests.cs`, ajouter :

```csharp
using OrderManagement.Application.Exceptions;
```

Puis :

```csharp
[Fact]
public async Task CreateOrderAsync_Should_Throw_BadRequest_When_Stock_Is_Insufficient()
{
    // Arrange
    await using var context = TestDbContextFactory.Create();

    var category = new Category
    {
        Name = "Hardware"
    };

    context.Categories.Add(category);
    await context.SaveChangesAsync();

    var product = new Product
    {
        Name = "Souris",
        Price = 25m,
        Stock = 1,
        CategoryId = category.Id
    };

    context.Products.Add(product);
    await context.SaveChangesAsync();

    var service = new ServiceOrder(context);

    var dto = new CreateOrderDto
    {
        Items = new List<OrderItemDto>
        {
            new()
            {
                ProductId = product.Id,
                Quantity = 5
            }
        }
    };

    // Act + Assert
    await Assert.ThrowsAsync<BadRequestException>(() => service.CreateOrderAsync(dto, userId: 123));
}
```

Ce test vérifie que l'on ne peut pas commander plus que le stock disponible.

---

## 13. Lancer les tests en local

Depuis la racine du projet :

```bash
dotnet test OrderManagement.slnx
```

Avec plus de détails :

```bash
dotnet test OrderManagement.slnx --configuration Release --verbosity normal
```

Avec génération d'un résultat `.trx` :

```bash
dotnet test OrderManagement.slnx --configuration Release --logger "trx;LogFileName=test-results.trx" --results-directory ./TestResults
```

---

## 14. Relier les tests à GitHub Actions

Une fois le projet `OrderManagement.Tests` ajouté à la solution, le workflow GitHub Actions peut lancer :

```yaml
- name: Lancer les tests
  run: >
    dotnet test ${{ env.SOLUTION_PATH }}
    --configuration Release
    --no-build
    --logger "trx;LogFileName=test-results.trx"
    --results-directory ${{ env.TEST_RESULTS_DIR }}
```

Si un test échoue, le workflow devient rouge.

Cela permet d'empêcher la fusion d'une pull request qui casse une règle métier.

---

## 15. Points importants

Pour expliquer le testing, on peut utiliser cette progression :

1. On crée un projet xUnit séparé.
2. On ajoute une référence vers les projets à tester.
3. On crée une base EF Core en mémoire.
4. On teste une méthode simple.
5. On teste une règle métier.
6. On lance les tests avec `dotnet test`.
7. On branche les tests dans GitHub Actions.

Phrase simple :

> La CI vérifie automatiquement le projet, mais ce sont les tests qui disent réellement ce qu'il faut vérifier.

---

## 16. Exemple complet de `CategoryServiceTests.cs`

Voici une version complète possible :

```csharp
using OrderManagement.Application.DTOs.Categories;
using OrderManagement.Application.Exceptions;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Services;
using OrderManagement.Tests.Helpers;

namespace OrderManagement.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateAsync_Should_Create_Category()
    {
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryService(context);

        var dto = new CreateCategoryDto
        {
            Name = "Informatique",
            Description = "Produits informatiques"
        };

        var result = await service.CreateAsync(dto);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("Informatique", result.Name);
        Assert.Equal("Produits informatiques", result.Description);
        Assert.Single(context.Categories);
    }

    [Fact]
    public async Task DeleteAsync_Should_Throw_BadRequest_When_Category_Has_Products()
    {
        await using var context = TestDbContextFactory.Create();

        var category = new Category
        {
            Name = "Livres",
            Description = "Catégorie avec produits"
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        context.Products.Add(new Product
        {
            Name = "Clean Code",
            Description = "Livre de programmation",
            Price = 30m,
            Stock = 10,
            CategoryId = category.Id
        });

        await context.SaveChangesAsync();

        var service = new CategoryService(context);

        await Assert.ThrowsAsync<BadRequestException>(() => service.DeleteAsync(category.Id));
    }
}
```
---
À retenir :

```text
xUnit = écrire les tests
EF Core InMemory = tester sans vraie base SQL Server
dotnet test = lancer les tests
GitHub Actions = lancer les tests automatiquement à chaque push ou pull request
```

---

## 17. Références utiles

- Testing .NET : https://learn.microsoft.com/dotnet/core/testing/
- xUnit avec .NET : https://learn.microsoft.com/dotnet/core/testing/unit-testing-csharp-with-xunit
- `dotnet test` : https://learn.microsoft.com/dotnet/core/tools/dotnet-test
- `dotnet sln` : https://learn.microsoft.com/dotnet/core/tools/dotnet-sln
- EF Core InMemory : https://learn.microsoft.com/ef/core/providers/in-memory/
