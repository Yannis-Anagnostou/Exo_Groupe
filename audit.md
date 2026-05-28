# Cours — Optimisation & sécurité avancée sur le projet `Exo_Groupe-dev`

> Le backend ne doit jamais faire confiance aux données envoyées par le client.

Dans ce projet, le meilleur exemple se trouve dans le module **commandes**.

Fichiers concernés :

- `src/OrderManagement.API/Controllers/OrderControllers/orderController.cs`
- `src/OrderManagement.Application/DTOs/OrderDto/CreateOrderDto.cs`
- `src/OrderManagement.Infrastructure/Services/ServiceOrder.cs`
- `src/OrderManagement.Domain/Entities/Order.cs`
- `src/OrderManagement.Domain/Entities/OrderItem.cs`
- `src/OrderManagement.Domain/Entities/Product.cs`

---

# 1. Erreur principale à étudier : une commande accepte des quantités non validées

## Où est le problème ?

Dans `CreateOrderDto.cs`, le client peut envoyer une liste d’articles :

```csharp
public class CreateOrderDto
{
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
```

À première vue, c’est correct.

Mais il manque des validations importantes :

- `Items` doit être obligatoire ;
- `Items` doit contenir au moins un article ;
- `ProductId` doit être supérieur à `0` ;
- `Quantity` doit être supérieur à `0`.

Actuellement, rien n’empêche un client d’envoyer :

```json
{
  "items": [
    {
      "productId": 1,
      "quantity": -10
    }
  ]
}
```

Dans `ServiceOrder.cs`, le total est calculé comme ceci :

```csharp
totalAmount += product.Price * item.Quantity;
```

Si la quantité vaut `-10`, le total devient négatif.

C’est une erreur très importante, parce qu’elle touche directement :

- la sécurité métier ;
- l’intégrité des données ;
- la cohérence comptable ;
- la fiabilité de l’API ;
- la confiance dans les données stockées en base.

---

# 2. Pourquoi c’est grave ?

Une API backend ne doit jamais supposer que le client est honnête.

Même si Swagger montre un exemple propre, un utilisateur peut appeler l’API avec Postman, curl, un script JavaScript ou un client modifié.

Exemples de données dangereuses :

```json
{ "items": [] }
```

```json
{
  "items": [
    { "productId": 0, "quantity": 3 }
  ]
}
```

```json
{
  "items": [
    { "productId": 1, "quantity": -5 }
  ]
}
```

```json
{
  "items": [
    { "productId": 1, "quantity": 999999999 }
  ]
}
```

Le backend doit donc refuser ces données avant de créer la commande.

---

# 3. Première correction : valider les DTOs

Dans ASP.NET Core, une première barrière simple consiste à utiliser les annotations de validation.

Correction proposée dans `CreateOrderDto.cs` :

```csharp
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.DTOs.OrderDto;

public class CreateOrderDto
{
    [Required(ErrorMessage = "La commande doit contenir des articles.")]
    [MinLength(1, ErrorMessage = "Une commande doit contenir au moins un article.")]
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "L'identifiant du produit doit être valide.")]
    public int ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "La quantité doit être comprise entre 1 et 1000.")]
    public int Quantity { get; set; }
}
```

## Pourquoi corriger dans le DTO ?

Le DTO représente ce que le client a le droit d’envoyer.

Il sert de première frontière entre :

- le monde extérieur ;
- le backend ;
- la base de données.

Si une donnée est invalide dès son arrivée, il faut la rejeter avant d’exécuter la logique métier.

Dans ce projet, le `Program.cs` contient déjà une configuration intéressante :

```csharp
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            // format d'erreur personnalisé
        };
    });
```

Donc si les DTOs sont bien annotés, ASP.NET Core peut automatiquement répondre `400 Bad Request` avec un message clair.

---

# 4. Deuxième correction : vérifier les règles métier dans le service

Les annotations DTO ne suffisent pas.

Certaines règles nécessitent la base de données.

Exemples :

- le produit existe-t-il vraiment ?
- le produit a-t-il assez de stock ?
- le prix doit-il être récupéré depuis la base ?
- faut-il créer les lignes de commande ?

Dans `ServiceOrder.cs`, le code vérifie déjà si le produit existe :

```csharp
Product? product = await _context.Products.FindAsync(item.ProductId);
if (product == null)
{
    throw new BadRequestException($"Le produit avec l'ID {item.ProductId} n'existe pas.");
}
```

C’est une bonne idée.

Mais il manque encore :

- la vérification du stock ;
- la création des `OrderItem` ;
- le stockage du prix unitaire au moment de la commande ;
- la diminution du stock ;
- l’utilisation d’une transaction.

Correction possible :

```csharp
public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, int userId)
{
    if (dto.Items == null || dto.Items.Count == 0)
    {
        throw new BadRequestException("Une commande doit contenir au moins un article.");
    }

    var order = new Order
    {
        UserId = userId,
        OrderDate = DateTime.UtcNow,
        Status = OrderStatus.Pending,
        TotalAmount = 0
    };

    foreach (var item in dto.Items)
    {
        var product = await _context.Products.FindAsync(item.ProductId);

        if (product == null)
        {
            throw new BadRequestException($"Le produit avec l'ID {item.ProductId} n'existe pas.");
        }

        if (product.Stock < item.Quantity)
        {
            throw new BadRequestException($"Stock insuffisant pour le produit {product.Name}.");
        }

        var unitPrice = product.Price;
        var lineTotal = unitPrice * item.Quantity;

        order.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            Quantity = item.Quantity,
            UnitPrice = unitPrice,
            TotalPrice = lineTotal
        });

        product.Stock -= item.Quantity;
        order.TotalAmount += lineTotal;
    }

    _context.Orders.Add(order);
    await _context.SaveChangesAsync();

    return new OrderResponseDto
    {
        Id = order.Id,
        UserId = order.UserId,
        OrderDate = order.OrderDate,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        Items = order.Items.Select(i => new OrderItemResponseDto
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}
```

## Pourquoi cette correction est meilleure ?

Elle respecte plusieurs règles importantes :

1. Le client envoie uniquement `ProductId` et `Quantity`.
2. Le backend récupère le prix depuis la base.
3. Le backend calcule le total.
4. Le backend enregistre les lignes de commande.
5. Le backend empêche une commande supérieure au stock disponible.
6. Le backend garde une trace du prix au moment de l’achat.

C’est exactement le type de raisonnement attendu dans une API de gestion de commandes.

---

# 5. Exercice

## Partie A — Reproduire le problème

Dans Swagger ou Scalar :

1. Créer ou identifier un produit existant.
2. Appeler `POST /api/order` avec une quantité négative.
3. Observer la réponse.
4. Vérifier si une commande peut être créée avec un total incorrect.

Payload à tester :

```json
{
  "items": [
    {
      "productId": 1,
      "quantity": -3
    }
  ]
}
```

## Partie B — Corriger le DTO

Modifier `CreateOrderDto.cs` pour ajouter :

- `[Required]` ;
- `[MinLength(1)]` ;
- `[Range]` sur `ProductId` ;
- `[Range]` sur `Quantity`.

## Partie C — Corriger le service

Modifier `ServiceOrder.cs` pour :

- vérifier le stock ;
- créer les `OrderItem` ;
- copier le prix depuis `Product.Price` vers `OrderItem.UnitPrice` ;
- calculer `OrderItem.TotalPrice` ;
- calculer `Order.TotalAmount` ;
- diminuer le stock.

## Partie D — Retester

Retester avec :

```json
{
  "items": [
    {
      "productId": 1,
      "quantity": -3
    }
  ]
}
```

Résultat attendu : `400 Bad Request`.

Retester avec :

```json
{
  "items": [
    {
      "productId": 1,
      "quantity": 2
    }
  ]
}
```

Résultat attendu : commande créée avec un total calculé par le backend.

---

# 6. Autres endroits où le même type d’erreur apparaît

## 6.1. `GetOrderByIdAsync` utilise `order.UserId` avant de vérifier si `order` existe

Dans `ServiceOrder.cs` :

```csharp
Order? order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == id);
if  (isAdmin== true || order.UserId == userId)
{
    if (order == null)
    {
        throw new BadRequestException($"La commande avec l'ID {id} n'existe pas.");
    }
```

Problème : si `order == null`, alors `order.UserId` provoque une erreur serveur.

Correction :

```csharp
var order = await _context.Orders
    .Include(o => o.User)
    .Include(o => o.Items)
    .FirstOrDefaultAsync(o => o.Id == id);

if (order == null)
{
    throw new NotFoundException($"La commande avec l'ID {id} n'existe pas.");
}

if (!isAdmin && order.UserId != userId)
{
    throw new BadRequestException("Vous n'avez pas l'autorisation de voir cette commande.");
}
```

Exercice : corriger cette méthode et vérifier qu’une commande inexistante renvoie bien `404 Not Found`, pas `500 Internal Server Error`.

---

## 6.2. `OrderController` lit les claims manuellement mais n’a pas `[Authorize]`

Dans `orderController.cs`, les méthodes lisent :

```csharp
Claim userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
```

Mais le contrôleur n’a pas d’attribut `[Authorize]`.

Cela veut dire que les routes sont accessibles sans authentification, puis le code essaye de gérer le cas à la main.

Correction recommandée :

```csharp
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    // ...
}
```

Ensuite, on peut simplifier les méthodes, car ASP.NET Core refusera automatiquement les requêtes sans token.

Exercice : ajouter `[Authorize]` au contrôleur de commandes et tester :

- sans token : réponse `401 Unauthorized` ;
- avec token User : accès à ses commandes ;
- avec token Admin : accès à toutes les commandes.

---

## 6.3. Les `try/catch` du contrôleur contournent le middleware d’erreurs

Le projet possède déjà un `ExceptionMiddleware.cs`.

C’est une bonne idée.

Mais `OrderController` fait aussi ceci :

```csharp
try
{
    // logique
}
catch (BadRequestException ex)
{
    return BadRequest(new { error = ex.Message });
}
catch (Exception ex)
{
    return StatusCode(500, new { error = "Une erreur est survenue..." });
}
```

Problème : il y a deux systèmes de gestion d’erreurs en même temps.

Conséquences :

- les erreurs ne sont pas toujours au même format ;
- certaines erreurs sont transformées en `500` alors qu’elles devraient être des `404` ou `400` ;
- le code du contrôleur devient plus long ;
- le middleware devient moins utile.

Correction pédagogique : laisser les exceptions remonter au middleware.

Exemple :

```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var isAdmin = User.IsInRole("Admin");

    var order = await _orderService.GetOrderByIdAsync(id, userId, isAdmin);
    return Ok(order);
}
```

Exercice : retirer progressivement les `try/catch` du `OrderController` et vérifier que `ExceptionMiddleware` renvoie toujours un JSON propre.

---

## 6.4. `DateTime.Now` au lieu de `DateTime.UtcNow`

Dans `ServiceOrder.cs` :

```csharp
o.OrderDate = DateTime.Now;
```

Dans le domaine, `Order.cs` utilise déjà :

```csharp
public DateTime OrderDate { get; set; } = DateTime.UtcNow;
```

Pour une API backend, il vaut mieux utiliser `UtcNow`, surtout si l’application peut être utilisée depuis plusieurs pays ou hébergée sur un serveur distant.

Correction :

```csharp
o.OrderDate = DateTime.UtcNow;
```

Exercice : chercher tous les usages de `DateTime.Now` et les remplacer par `DateTime.UtcNow` lorsque la date est stockée en base.

---

# 7. Ajout recommandé : logs utiles avec `ILogger`

## Pourquoi ajouter des logs ?

Les logs servent à comprendre ce qu’il s’est passé dans l’application sans devoir reproduire manuellement le bug.

Ils sont utiles pour :

- savoir qu’une commande a été créée ;
- savoir qu’un utilisateur a tenté une action interdite ;
- diagnostiquer une erreur serveur ;
- suivre les comportements anormaux ;
- faciliter la maintenance.

Attention : un log ne doit pas contenir de données sensibles.

Ne jamais logger :

- un mot de passe ;
- un token JWT complet ;
- une clé secrète ;
- des données personnelles inutiles.

---

## Exemple dans `ServiceOrder`

Ajouter un logger :

```csharp
private readonly ILogger<ServiceOrder> _logger;

public ServiceOrder(AppDbContext context, ILogger<ServiceOrder> logger)
{
    _context = context;
    _logger = logger;
}
```

Puis logger les événements importants :

```csharp
_logger.LogInformation(
    "Création d'une commande pour l'utilisateur {UserId} avec {ItemCount} article(s).",
    userId,
    dto.Items.Count);
```

En cas de stock insuffisant :

```csharp
_logger.LogWarning(
    "Stock insuffisant pour le produit {ProductId}. Stock actuel: {Stock}, quantité demandée: {Quantity}.",
    product.Id,
    product.Stock,
    item.Quantity);
```

En cas d’erreur inattendue, le `ExceptionMiddleware` log déjà :

```csharp
_logger.LogError(ex, "Une erreur interne s'est produite.");
```

C’est bien : les erreurs techniques doivent être loggées côté serveur, mais l’utilisateur reçoit un message générique.

---

## Quand utiliser chaque niveau de log ?

### `LogInformation`

À utiliser pour les actions normales importantes.

Exemples :

- commande créée ;
- utilisateur connecté ;
- catégorie créée ;
- produit modifié.

### `LogWarning`

À utiliser quand quelque chose est suspect mais pas forcément une erreur technique.

Exemples :

- tentative d’accès à une commande d’un autre utilisateur ;
- stock insuffisant ;
- tentative de connexion échouée ;
- suppression refusée.

### `LogError`

À utiliser pour une erreur technique ou inattendue.

Exemples :

- exception non prévue ;
- base de données indisponible ;
- bug serveur.

---

# 8. Ajout recommandé : pagination

Dans `ServiceOrder.cs`, la méthode admin récupère toutes les commandes :

```csharp
List<Order> orders = await _context.Orders.Include(o => o.User).ToListAsync();
```

Sur une petite base, ça marche.

Sur une vraie base, ça peut devenir lent.

Problèmes possibles :

- trop de données chargées en mémoire ;
- réponse API trop lourde ;
- lenteur côté Swagger ou frontend ;
- surcharge de la base de données.

Correction : ajouter `page` et `pageSize`.

Exemple côté contrôleur :

```csharp
[HttpGet]
public async Task<ActionResult<List<OrderResponseDto>>> GetAllOrders(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var isAdmin = User.IsInRole("Admin");

    var orders = await _orderService.GetAllOrdersAsync(userId, isAdmin, page, pageSize);
    return Ok(orders);
}
```

Exemple côté service :

```csharp
var query = _context.Orders.AsNoTracking();

if (!isAdmin)
{
    query = query.Where(o => o.UserId == userId);
}

var orders = await query
    .OrderByDescending(o => o.OrderDate)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

Pourquoi `AsNoTracking()` ?

Parce que pour une lecture simple, Entity Framework n’a pas besoin de suivre les entités pour détecter des modifications. Cela réduit la mémoire utilisée et améliore les performances.

Exercice : ajouter la pagination sur :

- `GET /api/order` ;
- `GET /api/categories` si nécessaire ;
- plus tard `GET /api/products` quand le module produit sera présent.

---

# 9. Ajout recommandé : transaction lors de la création d’une commande

Créer une commande touche plusieurs éléments :

- la table `Orders` ;
- la table `OrderItems` ;
- le stock des `Products`.

Si une erreur arrive au milieu, il ne faut pas avoir une commande créée sans ses lignes, ou un stock modifié sans commande.

Correction avancée : utiliser une transaction.

```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();

try
{
    // créer la commande
    // ajouter les lignes
    // diminuer le stock
    await _context.SaveChangesAsync();

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

Exercice avancé : entourer `CreateOrderAsync` avec une transaction.

---

# 10. Ajout recommandé : sécuriser les secrets

Dans `appsettings.json`, on trouve :

```json
"Jwt": {
  "Key": "CHANGE_ME_TO_A_LONG_SECRET_KEY_32CHARS_MIN",
  "Issuer": "OrderManagement.API",
  "Audience": "OrderManagement.Client",
  "ExpiresInMinutes": 60
}
```

Le fait que la valeur indique `CHANGE_ME...` est plutôt bon : cela montre qu’il faut la remplacer.

Mais en conditions réelles, une clé JWT ne doit pas être commitée dans Git.

Solutions possibles :

- variables d’environnement ;
- User Secrets en développement ;
- secret manager du serveur ou du cloud ;
- pipeline CI/CD sécurisé.

Exemple avec variable d’environnement :

```bash
setx Jwt__Key "une-vraie-cle-secrete-longue-et-aleatoire"
```

En ASP.NET Core, `Jwt__Key` correspond à :

```json
"Jwt": {
  "Key": "..."
}
```

Exercice : remplacer la clé JWT locale par une variable d’environnement et vérifier que l’API démarre toujours.

---

# 11. Ajout recommandé : rendre Swagger / Scalar plus honnête

Dans `Program.cs`, le code ajoute le schéma Bearer à toutes les opérations OpenAPI.

Cela affiche un cadenas partout, même sur des routes publiques comme :

- `POST /api/auth/register` ;
- `POST /api/auth/login` ;
- `GET /api/categories`.

Ce n’est pas bloquant pour le fonctionnement, mais cela peut induire en erreur.

Exercice bonus : améliorer la génération OpenAPI pour ne mettre le cadenas que sur les endpoints qui ont `[Authorize]`.

---

# 12. Liste d’exercices

## Niveau 1 — Validation

1. Ajouter les annotations de validation dans `CreateOrderDto`.
2. Tester les cas invalides dans Swagger ou Scalar.
3. Vérifier que l’API renvoie `400 Bad Request`.

## Niveau 2 — Sécurité

1. Ajouter `[Authorize]` sur `OrderController`.
2. Vérifier qu’une requête sans token est refusée.
3. Vérifier qu’un utilisateur ne peut pas consulter la commande d’un autre utilisateur.
4. Vérifier qu’un admin peut consulter toutes les commandes.

## Niveau 3 — Règles métier

1. Vérifier que le produit existe.
2. Vérifier que la quantité demandée est disponible en stock.
3. Copier le prix du produit dans `OrderItem.UnitPrice`.
4. Calculer `OrderItem.TotalPrice`.
5. Calculer `Order.TotalAmount`.
6. Diminuer le stock.

## Niveau 4 — Qualité API

1. Supprimer les `try/catch` inutiles dans `OrderController`.
2. Laisser `ExceptionMiddleware` gérer les exceptions.
3. Corriger le `null check` dans `GetOrderByIdAsync`.
4. Utiliser `NotFoundException` quand une commande n’existe pas.

## Niveau 5 — Optimisation

1. Ajouter `AsNoTracking()` sur les lectures simples.
2. Ajouter une pagination sur la liste des commandes.
3. Éviter les `Include` inutiles si les données ne sont pas utilisées.
4. Utiliser `OrderByDescending(o => o.OrderDate)` pour afficher les plus récentes d’abord.

## Niveau 6 — Logs

1. Ajouter `ILogger<ServiceOrder>`.
2. Logger la création d’une commande en `Information`.
3. Logger les refus métier en `Warning`.
4. Ne jamais logger de mot de passe, token JWT ou secret.

## Niveau 7 — Robustesse avancée

1. Ajouter une transaction autour de la création de commande.
2. Vérifier que commande, lignes et stock restent cohérents en cas d’erreur.
3. Écrire au moins trois tests manuels documentés dans le README.

---

# 13. Correction minimale attendue en fin de séance

À la fin de la séance, le groupe devrait au minimum avoir :

- DTO de création de commande validé ;
- endpoints de commandes protégés par `[Authorize]` ;
- création réelle des `OrderItem` ;
- calcul backend du total ;
- vérification du stock ;
- correction du bug `order.UserId` avant `order == null` ;
- suppression des `try/catch` inutiles dans le contrôleur ;
- au moins deux logs utiles ;
- pagination simple sur `GET /api/order`.

---

Une API sécurisée ne consiste pas seulement à ajouter un JWT.

Une API sécurisée, c’est aussi :

- valider les données reçues ;
- vérifier les règles métier côté serveur ;
- empêcher les accès non autorisés ;
- éviter les états incohérents en base ;
- ne pas exposer les détails techniques ;
- écrire des logs utiles ;
- prévoir la montée en charge avec pagination et requêtes optimisées.

Le JWT répond à la question :

> Qui est l’utilisateur ?

Mais les validations et les règles métier répondent aux questions :

> A-t-il le droit de faire ça ?  
> Les données envoyées sont-elles acceptables ?  
> La base restera-t-elle cohérente après l’action ?
