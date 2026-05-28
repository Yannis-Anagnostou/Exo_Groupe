# Exo_Groupe

## Product Management

### Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/product` | No | Get all products |
| GET | `/api/product/{id}` | No | Get product by ID |
| POST | `/api/product` | No | Create a product |
| PUT | `/api/product/{id}` | No | Update a product |
| DELETE | `/api/product/{id}` | No | Delete a product |

---

### GET `/api/product`

Returns all products.

**Response `200`**
```json
[
  {
    "id": 1,
    "name": "Laptop",
    "description": "Gaming laptop",
    "price": 999.99,
    "stock": 10,
    "categoryId": null
  }
]
```

---

### GET `/api/product/{id}`

Returns a single product by ID.

**Response `200`**
```json
{
  "id": 1,
  "name": "Laptop",
  "description": "Gaming laptop",
  "price": 999.99,
  "stock": 10,
  "categoryId": null
}
```

**Response `404`**
```json
{ "message": "Product 1 not found" }
```

---

### POST `/api/product`

Creates a new product.

**Request body**
```json
{
  "name": "Laptop",
  "description": "Gaming laptop",
  "price": 999.99,
  "stock": 10
}
```

| Field | Type | Required |
|-------|------|----------|
| name | string | Yes |
| description | string | No |
| price | decimal | Yes |
| stock | int | No (default: 0) |

**Response `201`** — returns the created product.

---

### PUT `/api/product/{id}`

Updates an existing product.

**Request body**
```json
{
  "name": "Laptop Pro",
  "description": "Updated description",
  "price": 1199.99,
  "stock": 5
}
```

**Response `200`** — returns the updated product.

**Response `404`**
```json
{ "message": "Product 1 not found" }
```

---

### DELETE `/api/product/{id}`

Deletes a product.

**Response `204`** — no content.

**Response `404`**
```json
{ "message": "Product 1 not found" }
```